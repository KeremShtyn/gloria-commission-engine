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
    Task<IReadOnlyDictionary<string, Guid>> GetIdsByEmployeeNoAsync(CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);
    void AddRange(IEnumerable<Employee> employees);
}

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> FindAllAsync(CancellationToken ct = default);
    void Add(Department department);
}

public interface IHotelRepository
{
    Task<IReadOnlyList<Hotel>> FindAllAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    void AddRange(IEnumerable<Hotel> hotels);
}

public interface IProductGroupRepository
{
    Task<IReadOnlyList<ProductGroup>> FindAllAsync(CancellationToken ct = default);

    /// <summary>Aktarim sirasinda grup kodunu kimlige cevirmek icin.</summary>
    Task<IReadOnlyDictionary<string, Guid>> GetIdsByCodeAsync(CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);
    void AddRange(IEnumerable<ProductGroup> groups);
}

public interface ICommissionRuleRepository
{
    Task<IReadOnlyList<CommissionRule>> FindAllAsync(CancellationToken ct = default);
    Task<CommissionRule?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Verilen tarih araliginda yururlukte olan aktif kurallar.</summary>
    Task<IReadOnlyList<CommissionRule>> FindEffectiveAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);

    void Add(CommissionRule rule);
    void AddRange(IEnumerable<CommissionRule> rules);
    void RemoveTiers(IEnumerable<CommissionRuleTier> tiers);
}

public interface ISaleRecordRepository
{
    Task<IReadOnlyList<SaleRecord>> FindByEmployeeAndPeriodAsync(
        Guid employeeId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<IReadOnlyList<SaleRecord>> FindByPeriodAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Verilen tekillik anahtarlarindan veritabaninda halihazirda bulunanlar.</summary>
    Task<IReadOnlySet<string>> FindExistingHashesAsync(
        IReadOnlyCollection<string> hashes, CancellationToken ct = default);

    /// <summary>Bir iadenin iptal ettigi orijinal satis. Once referansa, sonra icerige bakilir.</summary>
    Task<SaleRecord?> FindOriginalForRefundAsync(SaleRecord refund, CancellationToken ct = default);

    void AddRange(IEnumerable<SaleRecord> sales);
}

public interface ICommissionResultRepository
{
    Task<CommissionResult?> FindWithLinesAsync(
        Guid periodId, Guid employeeId, CancellationToken ct = default);

    /// <summary>Donemin tum sonuclari, adimlariyla. Personel basina ayri sorgu atmamak icin.</summary>
    Task<IReadOnlyList<CommissionResult>> FindByPeriodAsync(
        Guid periodId, CancellationToken ct = default);

    void Add(CommissionResult result);
    void Remove(CommissionResult result);
    void RemoveLines(IEnumerable<CommissionResultLine> lines);

    /// <summary>
    /// Adimlari dogrudan ekler. Navigasyon koleksiyonu yeniden atanirsa EF ayni satiri
    /// iki kez silmeye calisiyor; bu yuzden iliski uzerinden degil kumeye yaziliyor.
    /// </summary>
    void AddLines(IEnumerable<CommissionResultLine> lines);
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

    /// <summary>Ham satirlari donusum uygulanmadan once kaydeder.</summary>
    void AddStagingRows(IEnumerable<StagingRow> rows);

    /// <summary>Bir partinin ham satirlari. Yeniden isleme ve mutabakat icin.</summary>
    Task<IReadOnlyList<StagingRow>> FindStagingRowsAsync(Guid batchId, CancellationToken ct = default);

    Task<IReadOnlyList<ImportBatch>> FindBatchesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ImportError>> FindErrorsByBatchAsync(
        Guid batchId, CancellationToken ct = default);

    /// <summary>Mutabakat raporu icin hata kodu dagilimlari.</summary>
    Task<IReadOnlyList<(string ErrorCode, int Count)>> CountErrorsByCodeAsync(
        CancellationToken ct = default);
}
