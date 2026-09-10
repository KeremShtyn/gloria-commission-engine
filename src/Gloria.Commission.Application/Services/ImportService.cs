using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Application.Mappers;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

/// <summary>
/// CSV yuklemesini uctan uca yurutur: ayristirma, mukerrer eleme, hatali satir loglama,
/// iade eslestirme. Ayristirma isi kaynak sisteme ozgu oldugu icin
/// <see cref="ISourceImporter"/> implementasyonlarina devredilir.
/// </summary>
public sealed class ImportService : IImportService
{
    private readonly IReadOnlyDictionary<SourceSystem, ISourceImporter> _importers;
    private readonly IEmployeeRepository _employees;
    private readonly ISaleRecordRepository _sales;
    private readonly IImportRepository _imports;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ImportService(
        IEnumerable<ISourceImporter> importers,
        IEmployeeRepository employees,
        ISaleRecordRepository sales,
        IImportRepository imports,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _importers = importers.ToDictionary(i => i.SourceSystem);
        _employees = employees;
        _sales = sales;
        _imports = imports;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ImportSummary> ImportAsync(
        SourceSystem source, string fileName, string content, CancellationToken ct = default)
    {
        if (!_currentUser.CanSeeAllEmployees)
            throw new DomainException("FORBIDDEN", "Veri aktarimi icin Admin veya Muhasebe rolu gerekir.");

        if (!_importers.TryGetValue(source, out var importer))
            throw new DomainException("SOURCE_UNSUPPORTED", $"'{source}' kaynak sistemi icin ayristirici yok.");

        var batch = new ImportBatch
        {
            SourceSystem = source,
            FileName = fileName,
            FileHash = ContentHash.Of(content),
            ImportedBy = _currentUser.UserId
        };

        _imports.AddBatch(batch);
        await _unitOfWork.SaveChangesAsync(ct);

        var employeeIds = await _employees.GetIdsByEmployeeNoAsync(ct);
        var knownEmployees = employeeIds.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = importer.Parse(content, knownEmployees);
        var failures = rows.Where(r => !r.IsValid).ToList();

        var (accepted, duplicatesInFile) = DeduplicateWithinFile(rows.Where(r => r.IsValid).ToList());

        var existing = await _sales.FindExistingHashesAsync(
            accepted.Select(r => r.Sale!.SourceHash).ToList(), ct);

        var toInsert = new List<SaleRecord>();

        foreach (var row in accepted)
        {
            var sale = row.Sale!;
            if (existing.Contains(sale.SourceHash)) continue;

            sale.ImportBatchId = batch.Id;
            sale.EmployeeId = employeeIds.TryGetValue(sale.EmployeeNo, out var id) ? id : null;
            toInsert.Add(sale);
        }

        _sales.AddRange(toInsert);

        _imports.AddErrors(failures.Select(f => new ImportError
        {
            ImportBatchId = batch.Id,
            RowNumber = f.RowNumber,
            RawLine = Truncate(f.RawLine, 2000),
            ErrorCode = f.ErrorCode!,
            ErrorMessage = Truncate(f.ErrorMessage!, 500)
        }));

        await _unitOfWork.SaveChangesAsync(ct);

        var matchedReversals = await MatchReversalsAsync(toInsert, ct);

        batch.TotalRows = rows.Count;
        batch.ImportedRows = toInsert.Count;
        batch.DuplicateRows = duplicatesInFile + accepted.Count(r => existing.Contains(r.Sale!.SourceHash));
        batch.FailedRows = failures.Count;
        batch.CompletedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

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

    public async Task<IReadOnlyList<ImportBatchResponse>> GetBatchesAsync(CancellationToken ct = default)
    {
        var batches = await _imports.FindBatchesAsync(ct);
        return batches.Select(ImportMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ImportErrorResponse>> GetErrorsAsync(
        int batchId, CancellationToken ct = default)
    {
        var errors = await _imports.FindErrorsByBatchAsync(batchId, ct);
        return errors.Select(ImportMapper.ToResponse).ToList();
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
            accepted.Add(group
                .OrderBy(r => r.Sale!.Status == SaleStatus.Unposted ? 1 : 0)
                .ThenBy(r => r.RowNumber)
                .First());

            duplicates += group.Count() - 1;
        }

        return (accepted, duplicates);
    }

    /// <summary>
    /// Iade kayitlarini iptal ettikleri satisla eslestirir ve orijinali Reversed olarak isaretler.
    /// Eslesme bulunamazsa iade yine de prim tabanindan dusulur — sadece baglanti kurulamamis olur.
    /// </summary>
    private async Task<int> MatchReversalsAsync(List<SaleRecord> inserted, CancellationToken ct)
    {
        var refunds = inserted.Where(s => s.Status == SaleStatus.Refund).ToList();
        if (refunds.Count == 0) return 0;

        var matched = 0;

        foreach (var refund in refunds)
        {
            var original = await _sales.FindOriginalForRefundAsync(refund, ct);
            if (original is null) continue;

            refund.ReversedSaleId = original.Id;
            original.Status = SaleStatus.Reversed;
            matched++;
        }

        if (matched > 0) await _unitOfWork.SaveChangesAsync(ct);
        return matched;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
