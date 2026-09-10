using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Import;

public interface IImportService
{
    Task<ImportSummary> ImportAsync(
        SourceSystem source, string fileName, string content, CancellationToken ct = default);
}

/// <summary>
/// CSV yuklemesini uctan uca yurutur: ayristirma, mukerrer eleme, hatali satir loglama,
/// iade eslestirme. Hatali satirlar dosyayi reddettirmez; ayri tabloda birikir.
/// </summary>
public sealed class ImportService : IImportService
{
    private readonly CommissionDbContext _db;
    private readonly IReadOnlyDictionary<SourceSystem, ISourceImporter> _importers;
    private readonly ICurrentUser _currentUser;

    public ImportService(
        CommissionDbContext db,
        IEnumerable<ISourceImporter> importers,
        ICurrentUser currentUser)
    {
        _db = db;
        _importers = importers.ToDictionary(i => i.SourceSystem);
        _currentUser = currentUser;
    }

    public async Task<ImportSummary> ImportAsync(
        SourceSystem source, string fileName, string content, CancellationToken ct = default)
    {
        if (!_importers.TryGetValue(source, out var importer))
            throw new DomainException("SOURCE_UNSUPPORTED", $"'{source}' kaynak sistemi icin ayristirici yok.");

        var batch = new ImportBatch
        {
            SourceSystem = source,
            FileName = fileName,
            FileHash = CsvReaderHelper.FileHash(content),
            ImportedBy = _currentUser.UserId
        };

        _db.ImportBatches.Add(batch);
        await _db.SaveChangesAsync(ct);

        var employees = await _db.Employees
            .ToDictionaryAsync(e => e.EmployeeNo, e => e.Id, ct);

        var knownEmployees = employees.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = importer.Parse(content, knownEmployees);

        var failures = rows.Where(r => !r.IsValid).ToList();
        var candidates = rows.Where(r => r.IsValid).ToList();

        var (accepted, duplicatesInFile) = DeduplicateWithinFile(candidates);

        var incomingHashes = accepted.Select(r => r.Sale!.SourceHash).ToList();
        var existingHashes = await _db.SaleRecords
            .Where(s => incomingHashes.Contains(s.SourceHash))
            .Select(s => s.SourceHash)
            .ToListAsync(ct);

        var existing = existingHashes.ToHashSet();
        var duplicatesInDb = accepted.Count(r => existing.Contains(r.Sale!.SourceHash));

        var toInsert = new List<SaleRecord>();

        foreach (var row in accepted)
        {
            var sale = row.Sale!;
            if (existing.Contains(sale.SourceHash)) continue;

            sale.ImportBatchId = batch.Id;
            sale.EmployeeId = employees.TryGetValue(sale.EmployeeNo, out var id) ? id : null;
            toInsert.Add(sale);
        }

        _db.SaleRecords.AddRange(toInsert);

        _db.ImportErrors.AddRange(failures.Select(f => new ImportError
        {
            ImportBatchId = batch.Id,
            RowNumber = f.RowNumber,
            RawLine = Truncate(f.RawLine, 2000),
            ErrorCode = f.ErrorCode!,
            ErrorMessage = Truncate(f.ErrorMessage!, 500)
        }));

        await _db.SaveChangesAsync(ct);

        var matchedReversals = await MatchReversalsAsync(toInsert, ct);

        batch.TotalRows = rows.Count;
        batch.ImportedRows = toInsert.Count;
        batch.DuplicateRows = duplicatesInFile + duplicatesInDb;
        batch.FailedRows = failures.Count;
        batch.CompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new ImportSummary
        {
            BatchId = batch.Id,
            SourceSystem = source,
            FileName = fileName,
            TotalRows = batch.TotalRows,
            ImportedRows = batch.ImportedRows,
            DuplicateRows = batch.DuplicateRows,
            FailedRows = batch.FailedRows,
            MatchedReversals = matchedReversals,
            Errors = failures.Select(f => new ImportErrorSummary
            {
                RowNumber = f.RowNumber,
                ErrorCode = f.ErrorCode!,
                ErrorMessage = f.ErrorMessage!,
                RawLine = f.RawLine
            }).ToList()
        };
    }

    /// <summary>
    /// Ayni dosyada ayni belge numarasi birden fazla kez gelebiliyor.
    /// Muhasebelesmis (POSTED) satir muhasebelesmemise tercih edilir; kalanlar mukerrer sayilir.
    /// </summary>
    private static (List<RowParseResult> Accepted, int Duplicates) DeduplicateWithinFile(
        List<RowParseResult> candidates)
    {
        var accepted = new List<RowParseResult>();
        var duplicates = 0;

        foreach (var group in candidates.GroupBy(r => r.Sale!.SourceHash))
        {
            var preferred = group
                .OrderBy(r => r.Sale!.Status == SaleStatus.Unposted ? 1 : 0)
                .ThenBy(r => r.RowNumber)
                .First();

            accepted.Add(preferred);
            duplicates += group.Count() - 1;
        }

        return (accepted, duplicates);
    }

    /// <summary>
    /// Iade kayitlarini iptal ettikleri satisla eslestirir ve orijinali Reversed olarak isaretler.
    /// ERP'de referans alani vardir; PMS/POS'ta yoktur, bu yuzden
    /// (personel + urun + mutlak tutar + tarih onceligi) ile en yakin aday secilir.
    /// Eslesme bulunamazsa iade yine de prim tabanindan dusulur — sadece baglanti kurulamamis olur.
    /// </summary>
    private async Task<int> MatchReversalsAsync(List<SaleRecord> inserted, CancellationToken ct)
    {
        var refunds = inserted.Where(s => s.Status == SaleStatus.Refund).ToList();
        if (refunds.Count == 0) return 0;

        var matched = 0;

        foreach (var refund in refunds)
        {
            SaleRecord? original = null;

            if (!string.IsNullOrWhiteSpace(refund.SourceReference))
            {
                original = await _db.SaleRecords.FirstOrDefaultAsync(
                    s => s.SourceSystem == refund.SourceSystem
                         && s.SourceDocumentNo == refund.SourceReference
                         && s.Status == SaleStatus.Normal, ct);
            }

            if (original is null)
            {
                var target = Math.Abs(refund.AmountTry);

                original = await _db.SaleRecords
                    .Where(s => s.SourceSystem == refund.SourceSystem
                                && s.EmployeeNo == refund.EmployeeNo
                                && s.ProductCode == refund.ProductCode
                                && s.Status == SaleStatus.Normal
                                && s.TransactionDate <= refund.TransactionDate
                                && s.AmountTry == target)
                    .OrderByDescending(s => s.TransactionDate)
                    .FirstOrDefaultAsync(ct);
            }

            if (original is null) continue;

            refund.ReversedSaleId = original.Id;
            original.Status = SaleStatus.Reversed;
            matched++;
        }

        if (matched > 0) await _db.SaveChangesAsync(ct);
        return matched;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
