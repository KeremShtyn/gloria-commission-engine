using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Services;

public interface IReconciliationService
{
    Task<ReconciliationReportDto> GetAsync(int year, int month, CancellationToken ct = default);
}

/// <summary>
/// ERP'yi prim tabani olarak degil dogrulama katmani olarak kullanan mutabakat raporu.
/// ERP'nin Reference alani PMS belge numaralarina isaret etse de icerik tutmadigi icin
/// satir bazli eslestirme yapilmaz; karsilastirma urun grubu toplaminda yapilir.
/// </summary>
public sealed class ReconciliationService : IReconciliationService
{
    private readonly CommissionDbContext _db;

    public ReconciliationService(CommissionDbContext db) => _db = db;

    public async Task<ReconciliationReportDto> GetAsync(
        int year, int month, CancellationToken ct = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var sales = await _db.SaleRecords
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to)
            .Select(s => new { s.SourceSystem, s.ProductGroup, s.AmountTry, s.Status, s.ReversedSaleId })
            .ToListAsync(ct);

        var groups = sales
            .Select(s => s.ProductGroup)
            .Distinct()
            .OrderBy(g => g)
            .Select(group =>
            {
                var operational = sales
                    .Where(s => s.ProductGroup == group
                                && s.SourceSystem != SourceSystem.Erp
                                && s.Status is SaleStatus.Normal or SaleStatus.Refund)
                    .Sum(s => s.AmountTry);

                var accounted = sales
                    .Where(s => s.ProductGroup == group
                                && s.SourceSystem == SourceSystem.Erp
                                && s.Status is SaleStatus.Normal or SaleStatus.Refund)
                    .Sum(s => s.AmountTry);

                return new ReconciliationGroupDto
                {
                    ProductGroup = group,
                    OperationalRevenue = operational,
                    AccountedRevenue = accounted,
                    Difference = Math.Round(operational - accounted, 2)
                };
            })
            .ToList();

        var unpostedCount = sales.Count(s => s.Status == SaleStatus.Unposted);
        var unpostedAmount = sales.Where(s => s.Status == SaleStatus.Unposted).Sum(s => s.AmountTry);

        var unmatchedRefunds = sales.Count(s => s.Status == SaleStatus.Refund && s.ReversedSaleId is null);

        var failedRows = await _db.ImportErrors
            .GroupBy(e => e.ErrorCode)
            .Select(g => new ImportErrorCountDto { ErrorCode = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return new ReconciliationReportDto
        {
            Period = $"{year:D4}-{month:D2}",
            Groups = groups,
            UnpostedCount = unpostedCount,
            UnpostedAmount = Math.Round(unpostedAmount, 2),
            UnmatchedRefundCount = unmatchedRefunds,
            ImportErrors = failedRows
        };
    }
}

public sealed record ReconciliationReportDto
{
    public string Period { get; init; } = string.Empty;
    public IReadOnlyList<ReconciliationGroupDto> Groups { get; init; } = Array.Empty<ReconciliationGroupDto>();

    /// <summary>ERP'de henuz muhasebelesmemis (UNPOSTED) satir sayisi ve tutari.</summary>
    public int UnpostedCount { get; init; }
    public decimal UnpostedAmount { get; init; }

    /// <summary>Orijinal satisi bulunamayan iade sayisi. Tutar yine dusulur, baglanti kurulamamistir.</summary>
    public int UnmatchedRefundCount { get; init; }

    public IReadOnlyList<ImportErrorCountDto> ImportErrors { get; init; } = Array.Empty<ImportErrorCountDto>();
}

public sealed record ReconciliationGroupDto
{
    public string ProductGroup { get; init; } = string.Empty;

    /// <summary>PMS + POS net cirosu (prim tabani).</summary>
    public decimal OperationalRevenue { get; init; }

    /// <summary>ERP'de muhasebelesmis net ciro.</summary>
    public decimal AccountedRevenue { get; init; }

    public decimal Difference { get; init; }
}

public sealed record ImportErrorCountDto
{
    public string ErrorCode { get; init; } = string.Empty;
    public int Count { get; init; }
}
