using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Repositories;

/// <summary>
/// Veri erisimi. Arayuzler burada, EF Core implementasyonlari Infrastructure'da —
/// servis katmani ORM'i bilmez, testte sahte repository ile calisir.
///
/// Yazma islemleri degisiklikleri isaretler; kalicilastirma <see cref="IUnitOfWork"/> ile yapilir.
/// Boylece bir servis metodu birden fazla tabloya tek islemde yazabilir.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> FindByEmployeeNoAsync(string employeeNo, CancellationToken ct = default);
    Task<IReadOnlyList<Employee>> FindAllAsync(CancellationToken ct = default);

    /// <summary>Aktarim sirasinda personel numarasini kimlige cevirmek icin.</summary>
    Task<IReadOnlyDictionary<string, int>> GetIdsByEmployeeNoAsync(CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);
    void AddRange(IEnumerable<Employee> employees);
}

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> FindAllAsync(CancellationToken ct = default);
    void Add(Department department);
}

public interface ICommissionRuleRepository
{
    Task<IReadOnlyList<CommissionRule>> FindAllAsync(CancellationToken ct = default);
    Task<CommissionRule?> FindByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Verilen tarih araliginda yururlukte olan aktif kurallar.</summary>
    Task<IReadOnlyList<CommissionRule>> FindEffectiveAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);

    void Add(CommissionRule rule);
    void AddRange(IEnumerable<CommissionRule> rules);
    void RemoveTiers(IEnumerable<CommissionRuleTier> tiers);
}

public interface ISaleRecordRepository
{
    Task<IReadOnlyList<SaleRecord>> FindByEmployeeAndPeriodAsync(
        string employeeNo, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<IReadOnlyList<SaleRecord>> FindByPeriodAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Verilen tekillik anahtarlarindan veritabaninda halihazirda bulunanlar.</summary>
    Task<IReadOnlySet<string>> FindExistingHashesAsync(
        IReadOnlyCollection<string> hashes, CancellationToken ct = default);

    /// <summary>Bir iadenin iptal ettigi orijinal satis. Once referansa, sonra icerige bakilir.</summary>
    Task<SaleRecord?> FindOriginalForRefundAsync(SaleRecord refund, CancellationToken ct = default);

    Task<IReadOnlyList<string>> FindDistinctProductGroupsAsync(CancellationToken ct = default);

    void AddRange(IEnumerable<SaleRecord> sales);
}

public interface ICommissionResultRepository
{
    Task<CommissionResult?> FindWithLinesAsync(
        int periodId, int employeeId, CancellationToken ct = default);

    void Add(CommissionResult result);
    void Remove(CommissionResult result);
    void RemoveLines(IEnumerable<CommissionResultLine> lines);
}

public interface IPeriodRepository
{
    Task<Period?> FindAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<Period>> FindAllAsync(CancellationToken ct = default);
    void Add(Period period);
}

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int Total)> SearchAsync(
        string? entityName, string? entityId, int page, int size, CancellationToken ct = default);
}

public interface IImportRepository
{
    void AddBatch(ImportBatch batch);
    void AddErrors(IEnumerable<ImportError> errors);

    Task<IReadOnlyList<ImportBatch>> FindBatchesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ImportError>> FindErrorsByBatchAsync(
        int batchId, CancellationToken ct = default);

    /// <summary>Mutabakat raporu icin hata kodu dagilimlari.</summary>
    Task<IReadOnlyList<(string ErrorCode, int Count)>> CountErrorsByCodeAsync(
        CancellationToken ct = default);
}
