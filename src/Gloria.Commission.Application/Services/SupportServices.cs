using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Mappers;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

/// <summary>
/// ERP'yi prim tabani olarak degil dogrulama katmani olarak kullanan mutabakat raporu.
/// ERP'nin referans alani PMS belge numaralarina isaret etse de icerik tutmadigi icin
/// satir bazli eslestirme yapilmaz; karsilastirma urun grubu toplaminda yapilir.
/// </summary>
public sealed class ReconciliationService : IReconciliationService
{
    private readonly ISaleRecordRepository _sales;
    private readonly IImportRepository _imports;

    public ReconciliationService(ISaleRecordRepository sales, IImportRepository imports)
    {
        _sales = sales;
        _imports = imports;
    }

    public async Task<ReconciliationResponse> GetAsync(
        int year, int month, CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");

        var from = new DateOnly(year, month, 1);
        var to = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        var sales = await _sales.FindByPeriodAsync(from, to, ct);

        var groups = sales
            .Where(s => s.Status is SaleStatus.Normal or SaleStatus.Refund)
            .GroupBy(s => s.ProductGroup)
            .OrderBy(g => g.Key)
            .Select(g => new ReconciliationGroupResponse
            {
                ProductGroup = g.Key,
                OperationalRevenue = g.Where(s => s.SourceSystem != SourceSystem.Erp).Sum(s => s.AmountTry),
                AccountedRevenue = g.Where(s => s.SourceSystem == SourceSystem.Erp).Sum(s => s.AmountTry),
                Difference = Math.Round(
                    g.Where(s => s.SourceSystem != SourceSystem.Erp).Sum(s => s.AmountTry)
                    - g.Where(s => s.SourceSystem == SourceSystem.Erp).Sum(s => s.AmountTry), 2)
            })
            .ToList();

        var unposted = sales.Where(s => s.Status == SaleStatus.Unposted).ToList();
        var errorCounts = await _imports.CountErrorsByCodeAsync(ct);

        return new ReconciliationResponse
        {
            Period = $"{year:D4}-{month:D2}",
            Groups = groups,
            UnpostedCount = unposted.Count,
            UnpostedAmount = Math.Round(unposted.Sum(s => s.AmountTry), 2),
            UnmatchedRefundCount = sales.Count(s => s.Status == SaleStatus.Refund && s.ReversedSaleId is null),
            ImportErrors = errorCounts
                .Select(e => new ImportErrorCountResponse { ErrorCode = e.ErrorCode, Count = e.Count })
                .ToList()
        };
    }
}

public sealed class AuditLogService : IAuditLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IAuditLogRepository _auditLogs;
    private readonly Abstractions.ICurrentUser _currentUser;

    public AuditLogService(IAuditLogRepository auditLogs, Abstractions.ICurrentUser currentUser)
    {
        _auditLogs = auditLogs;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<AuditLogResponse>> SearchAsync(
        string? entityName, string? entityId, int? page, int? size, CancellationToken ct = default)
    {
        if (!_currentUser.CanSeeAllEmployees)
            throw new DomainException("FORBIDDEN", "Denetim kayitlari icin Admin veya Muhasebe rolu gerekir.");

        var pageSize = size is null or <= 0 or > MaxPageSize ? DefaultPageSize : size.Value;
        var pageIndex = page is null or < 0 ? 0 : page.Value;

        var (items, total) = await _auditLogs.SearchAsync(entityName, entityId, pageIndex, pageSize, ct);

        return new PagedResponse<AuditLogResponse>
        {
            Content = items.Select(AuditLogMapper.ToResponse).ToList(),
            Page = pageIndex,
            Size = pageSize,
            TotalElements = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }
}

/// <summary>Arayuzun acilis listeleri: personel, departman, urun grubu.</summary>
public sealed class ReferenceService : IReferenceService
{
    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly ISaleRecordRepository _sales;

    public ReferenceService(
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        ISaleRecordRepository sales)
    {
        _employees = employees;
        _departments = departments;
        _sales = sales;
    }

    public async Task<IReadOnlyList<EmployeeResponse>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var employees = await _employees.FindAllAsync(ct);
        return employees.Select(EmployeeMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<DepartmentResponse>> GetDepartmentsAsync(CancellationToken ct = default)
    {
        var departments = await _departments.FindAllAsync(ct);
        return departments.Select(DepartmentMapper.ToResponse).ToList();
    }

    public Task<IReadOnlyList<string>> GetProductGroupsAsync(CancellationToken ct = default)
        => _sales.FindDistinctProductGroupsAsync(ct);
}
