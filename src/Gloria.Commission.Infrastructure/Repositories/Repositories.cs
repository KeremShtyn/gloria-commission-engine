using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly CommissionDbContext _db;

    public EmployeeRepository(CommissionDbContext db) => _db = db;

    public Task<Employee?> FindByEmployeeNoAsync(string employeeNo, CancellationToken ct = default)
        => _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeNo == employeeNo, ct);

    public async Task<IReadOnlyList<Employee>> FindAllAsync(CancellationToken ct = default)
        => await _db.Employees
            .Include(e => e.Department)
            .OrderBy(e => e.EmployeeNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, int>> GetIdsByEmployeeNoAsync(CancellationToken ct = default)
        => await _db.Employees.ToDictionaryAsync(e => e.EmployeeNo, e => e.Id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Employees.AnyAsync(ct);

    public void AddRange(IEnumerable<Employee> employees) => _db.Employees.AddRange(employees);
}

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly CommissionDbContext _db;

    public DepartmentRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<Department>> FindAllAsync(CancellationToken ct = default)
        => await _db.Departments.OrderBy(d => d.Code).ToListAsync(ct);

    public void Add(Department department) => _db.Departments.Add(department);
}

public sealed class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly CommissionDbContext _db;

    public CommissionRuleRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommissionRule>> FindAllAsync(CancellationToken ct = default)
        => await _db.CommissionRules
            .Include(r => r.Tiers)
            .OrderByDescending(r => r.Priority).ThenBy(r => r.Code)
            .ToListAsync(ct);

    public Task<CommissionRule?> FindByIdAsync(int id, CancellationToken ct = default)
        => _db.CommissionRules.Include(r => r.Tiers).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<CommissionRule>> FindEffectiveAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.CommissionRules
            .Include(r => r.Tiers)
            .Where(r => r.IsActive && r.EffectiveFrom <= to && (r.EffectiveTo == null || r.EffectiveTo >= from))
            .ToListAsync(ct);

    public Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default)
        => _db.CommissionRules.AnyAsync(r => r.Code == code && (excludeId == null || r.Id != excludeId), ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.CommissionRules.AnyAsync(ct);

    public void Add(CommissionRule rule) => _db.CommissionRules.Add(rule);

    public void AddRange(IEnumerable<CommissionRule> rules) => _db.CommissionRules.AddRange(rules);

    public void RemoveTiers(IEnumerable<CommissionRuleTier> tiers) => _db.CommissionRuleTiers.RemoveRange(tiers);
}

public sealed class SaleRecordRepository : ISaleRecordRepository
{
    private readonly CommissionDbContext _db;

    public SaleRecordRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<SaleRecord>> FindByEmployeeAndPeriodAsync(
        string employeeNo, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.SaleRecords
            .Where(s => s.EmployeeNo == employeeNo && s.TransactionDate >= from && s.TransactionDate <= to)
            .OrderBy(s => s.TransactionDate).ThenBy(s => s.SourceDocumentNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SaleRecord>> FindByPeriodAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.SaleRecords
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to)
            .OrderBy(s => s.TransactionDate).ThenBy(s => s.SourceDocumentNo)
            .ToListAsync(ct);

    public async Task<IReadOnlySet<string>> FindExistingHashesAsync(
        IReadOnlyCollection<string> hashes, CancellationToken ct = default)
    {
        if (hashes.Count == 0) return new HashSet<string>();

        var found = await _db.SaleRecords
            .Where(s => hashes.Contains(s.SourceHash))
            .Select(s => s.SourceHash)
            .ToListAsync(ct);

        return found.ToHashSet();
    }

    /// <summary>
    /// Once kaynak sistemin verdigi referansa bakilir (ERP'de vardir).
    /// PMS ve POS'ta boyle bir alan olmadigi icin
    /// personel + urun + mutlak tutar + tarih onceligi ile en yakin aday secilir.
    /// </summary>
    public async Task<SaleRecord?> FindOriginalForRefundAsync(
        SaleRecord refund, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(refund.SourceReference))
        {
            var byReference = await _db.SaleRecords.FirstOrDefaultAsync(
                s => s.SourceSystem == refund.SourceSystem
                     && s.SourceDocumentNo == refund.SourceReference
                     && s.Status == SaleStatus.Normal, ct);

            if (byReference is not null) return byReference;
        }

        var target = Math.Abs(refund.AmountTry);

        return await _db.SaleRecords
            .Where(s => s.SourceSystem == refund.SourceSystem
                        && s.EmployeeNo == refund.EmployeeNo
                        && s.ProductCode == refund.ProductCode
                        && s.Status == SaleStatus.Normal
                        && s.TransactionDate <= refund.TransactionDate
                        && s.AmountTry == target)
            .OrderByDescending(s => s.TransactionDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<string>> FindDistinctProductGroupsAsync(CancellationToken ct = default)
        => await _db.SaleRecords
            .Select(s => s.ProductGroup)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(ct);

    public void AddRange(IEnumerable<SaleRecord> sales) => _db.SaleRecords.AddRange(sales);
}

public sealed class CommissionResultRepository : ICommissionResultRepository
{
    private readonly CommissionDbContext _db;

    public CommissionResultRepository(CommissionDbContext db) => _db = db;

    public Task<CommissionResult?> FindWithLinesAsync(
        int periodId, int employeeId, CancellationToken ct = default)
        => _db.CommissionResults
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.PeriodId == periodId && r.EmployeeId == employeeId, ct);

    public void Add(CommissionResult result) => _db.CommissionResults.Add(result);

    public void Remove(CommissionResult result) => _db.CommissionResults.Remove(result);

    public void RemoveLines(IEnumerable<CommissionResultLine> lines)
        => _db.CommissionResultLines.RemoveRange(lines);
}

public sealed class PeriodRepository : IPeriodRepository
{
    private readonly CommissionDbContext _db;

    public PeriodRepository(CommissionDbContext db) => _db = db;

    public Task<Period?> FindAsync(int year, int month, CancellationToken ct = default)
        => _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

    public async Task<IReadOnlyList<Period>> FindAllAsync(CancellationToken ct = default)
        => await _db.Periods
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .ToListAsync(ct);

    public void Add(Period period) => _db.Periods.Add(period);
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly CommissionDbContext _db;

    public AuditLogRepository(CommissionDbContext db) => _db = db;

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> SearchAsync(
        string? entityName, string? entityId, int page, int size, CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(a => a.EntityId == entityId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.ChangedAtUtc).ThenByDescending(a => a.Id)
            .Skip(page * size).Take(size)
            .ToListAsync(ct);

        return (items, total);
    }
}

public sealed class ImportRepository : IImportRepository
{
    private readonly CommissionDbContext _db;

    public ImportRepository(CommissionDbContext db) => _db = db;

    public void AddBatch(ImportBatch batch) => _db.ImportBatches.Add(batch);

    public void AddErrors(IEnumerable<ImportError> errors) => _db.ImportErrors.AddRange(errors);

    public async Task<IReadOnlyList<ImportBatch>> FindBatchesAsync(CancellationToken ct = default)
        => await _db.ImportBatches
            .AsNoTracking()
            .OrderByDescending(b => b.StartedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportError>> FindErrorsByBatchAsync(
        int batchId, CancellationToken ct = default)
        => await _db.ImportErrors
            .AsNoTracking()
            .Where(e => e.ImportBatchId == batchId)
            .OrderBy(e => e.RowNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<(string ErrorCode, int Count)>> CountErrorsByCodeAsync(
        CancellationToken ct = default)
    {
        var rows = await _db.ImportErrors
            .GroupBy(e => e.ErrorCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return rows.Select(r => (r.Code, r.Count)).ToList();
    }
}
