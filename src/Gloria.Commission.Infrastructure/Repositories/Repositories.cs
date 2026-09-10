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
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Hotel)
            .FirstOrDefaultAsync(e => e.EmployeeNo == employeeNo, ct);

    public async Task<IReadOnlyList<Employee>> FindAllAsync(CancellationToken ct = default)
        => await _db.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Hotel)
            .OrderBy(e => e.EmployeeNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, Guid>> GetIdsByEmployeeNoAsync(CancellationToken ct = default)
        => await _db.Employees.ToDictionaryAsync(e => e.EmployeeNo, e => e.Id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Employees.AnyAsync(ct);

    public void AddRange(IEnumerable<Employee> employees) => _db.Employees.AddRange(employees);
}

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly CommissionDbContext _db;

    public DepartmentRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<Department>> FindAllAsync(CancellationToken ct = default)
        => await _db.Departments.AsNoTracking().OrderBy(d => d.Code).ToListAsync(ct);

    public void Add(Department department) => _db.Departments.Add(department);
}

public sealed class HotelRepository : IHotelRepository
{
    private readonly CommissionDbContext _db;

    public HotelRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<Hotel>> FindAllAsync(CancellationToken ct = default)
        => await _db.Hotels.AsNoTracking().OrderBy(h => h.Code).ToListAsync(ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Hotels.AnyAsync(ct);

    public void AddRange(IEnumerable<Hotel> hotels) => _db.Hotels.AddRange(hotels);
}

public sealed class ProductGroupRepository : IProductGroupRepository
{
    private readonly CommissionDbContext _db;

    public ProductGroupRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductGroup>> FindAllAsync(CancellationToken ct = default)
        => await _db.ProductGroups.AsNoTracking().OrderBy(g => g.Code).ToListAsync(ct);

    public async Task<IReadOnlyDictionary<string, Guid>> GetIdsByCodeAsync(CancellationToken ct = default)
        => await _db.ProductGroups.ToDictionaryAsync(g => g.Code, g => g.Id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.ProductGroups.AnyAsync(ct);

    public void AddRange(IEnumerable<ProductGroup> groups) => _db.ProductGroups.AddRange(groups);
}

public sealed class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly CommissionDbContext _db;

    public CommissionRuleRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommissionRule>> FindAllAsync(CancellationToken ct = default)
        => await _db.CommissionRules
            .AsNoTracking()
            .Include(r => r.Tiers)
            .Include(r => r.Department).Include(r => r.ProductGroup).Include(r => r.Hotel)
            .OrderByDescending(r => r.Priority).ThenBy(r => r.Code)
            .ToListAsync(ct);

    public Task<CommissionRule?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.CommissionRules
            .Include(r => r.Tiers)
            .Include(r => r.Department).Include(r => r.ProductGroup).Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<CommissionRule>> FindEffectiveAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.CommissionRules
            .AsNoTracking()
            .Include(r => r.Tiers)
            .Where(r => r.IsActive && r.EffectiveFrom <= to && (r.EffectiveTo == null || r.EffectiveTo >= from))
            .ToListAsync(ct);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
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
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.SaleRecords
            .AsNoTracking()
            .Include(s => s.ProductGroup)
            .Where(s => s.EmployeeId == employeeId && s.TransactionDate >= from && s.TransactionDate <= to)
            .OrderBy(s => s.TransactionDate).ThenBy(s => s.SourceDocumentNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SaleRecord>> FindByPeriodAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.SaleRecords
            .AsNoTracking()
            .Include(s => s.ProductGroup)
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
    ///
    /// Eslesme mantigi serviste; burasi yalnizca adaylari tek sorguda getirir.
    /// Iade basina sorgu atmak binlerce satirlik bir ekstrede aktarimi lineer yavaslatiyordu.
    /// </summary>
    public async Task<IReadOnlyList<SaleRecord>> FindReversalCandidatesAsync(
        IReadOnlyCollection<SaleRecord> refunds, CancellationToken ct = default)
    {
        if (refunds.Count == 0) return [];

        var sources = refunds.Select(r => r.SourceSystem).Distinct().ToList();
        var references = refunds
            .Where(r => !string.IsNullOrWhiteSpace(r.SourceReference))
            .Select(r => r.SourceReference!)
            .Distinct()
            .ToList();
        var employeeIds = refunds.Select(r => r.EmployeeId).Distinct().ToList();
        var productCodes = refunds.Select(r => r.ProductCode).Distinct().ToList();

        // Adaylar izlenerek getirilir: eslesen orijinalin durumu Reversed'a cevrilecek.
        return await _db.SaleRecords
            .Where(s => s.Status == SaleStatus.Normal
                        && sources.Contains(s.SourceSystem)
                        && (references.Contains(s.SourceDocumentNo)
                            || (employeeIds.Contains(s.EmployeeId)
                                && productCodes.Contains(s.ProductCode))))
            .ToListAsync(ct);
    }

    public void AddRange(IEnumerable<SaleRecord> sales) => _db.SaleRecords.AddRange(sales);
}

public sealed class CommissionResultRepository : ICommissionResultRepository
{
    private readonly CommissionDbContext _db;

    public CommissionResultRepository(CommissionDbContext db) => _db = db;

    public async Task<IReadOnlyList<CommissionResult>> FindByPeriodAsync(
        Guid periodId, CancellationToken ct = default)
        => await _db.CommissionResults
            .Include(r => r.Lines)
            .Where(r => r.PeriodId == periodId)
            .ToListAsync(ct);

    public void Add(CommissionResult result) => _db.CommissionResults.Add(result);

    public void RemoveLines(IEnumerable<CommissionResultLine> lines)
        => _db.CommissionResultLines.RemoveRange(lines);

    public void AddLines(IEnumerable<CommissionResultLine> lines)
        => _db.CommissionResultLines.AddRange(lines);
}

public sealed class PeriodRepository : IPeriodRepository
{
    private readonly CommissionDbContext _db;

    public PeriodRepository(CommissionDbContext db) => _db = db;

    public Task<Period?> FindAsync(int year, int month, CancellationToken ct = default)
        => _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

    public async Task<IReadOnlyList<Period>> FindAllAsync(CancellationToken ct = default)
        => await _db.Periods
            .AsNoTracking()
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

    public void AddStagingRows(IEnumerable<StagingRow> rows) => _db.StagingRows.AddRange(rows);

    public async Task<IReadOnlyList<StagingRow>> FindStagingRowsAsync(
        Guid batchId, CancellationToken ct = default)
        => await _db.StagingRows
            .AsNoTracking()
            .Where(r => r.ImportBatchId == batchId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportBatch>> FindBatchesAsync(CancellationToken ct = default)
        => await _db.ImportBatches
            .AsNoTracking()
            .OrderByDescending(b => b.StartedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ImportError>> FindErrorsByBatchAsync(
        Guid batchId, CancellationToken ct = default)
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
