using FluentAssertions;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Persistence.Interceptors;
using Gloria.Commission.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>
/// Okuma uclarinin veri yazmadigini dogrular.
///
/// Bu ayrim onemli: okuma ucu yazdiginda iki kullanicinin ayni anda ekrani acmasi
/// ayni (donem, personel) satirini yazmaya calisir ve istek benzersizlik kisitindan
/// hata alir. Sorun bir kez yasandi; test o davranisin geri gelmemesi icin.
/// </summary>
public class CommissionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FakeUser _user = new();

    private static readonly Guid DepartmentId = Guid.Parse("aa000000-0000-0000-0000-000000000001");
    private static readonly Guid HotelId = Guid.Parse("aa000000-0000-0000-0000-000000000002");
    private static readonly Guid GroupId = Guid.Parse("aa000000-0000-0000-0000-000000000003");
    private static readonly Guid EmployeeId = Guid.Parse("aa000000-0000-0000-0000-000000000004");
    private static readonly Guid BatchId = Guid.Parse("aa000000-0000-0000-0000-000000000005");

    public CommissionServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var db = NewContext();
        Seed(db);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Donem_ozeti_okumasi_veri_yazmaz()
    {
        using var db = NewContext();
        var service = NewService(db);

        var summary = await service.GetPeriodSummaryAsync(2026, 8, null, null, null);

        summary.TotalCommission.Should().Be(600m);

        // Okuma ucu ne prim sonucu ne de donem kaydi olusturmali.
        db.CommissionResults.Should().BeEmpty();
        db.Periods.Should().BeEmpty();
    }

    [Fact]
    public async Task Personel_primi_okumasi_veri_yazmaz()
    {
        using var db = NewContext();
        var service = NewService(db);

        var result = await service.GetForEmployeeAsync(2026, 8, "P1001");

        result.TotalCommission.Should().Be(600m);
        db.CommissionResults.Should().BeEmpty();
        db.Periods.Should().BeEmpty();
    }

    [Fact]
    public async Task Hesabi_calistirmak_sonucu_adimlariyla_kaydeder()
    {
        using var db = NewContext();
        var service = NewService(db);

        await service.RunPeriodAsync(2026, 8);

        var stored = db.CommissionResults.Include(r => r.Lines).Single();
        stored.TotalCommission.Should().Be(600m);
        stored.Lines.Should().NotBeEmpty();
        stored.CalculatedBy.Should().Be("test-user");
    }

    [Fact]
    public async Task Hesap_iki_kez_calistirilinca_satir_cogalmaz()
    {
        using var db = NewContext();
        var service = NewService(db);

        await service.RunPeriodAsync(2026, 8);
        await service.RunPeriodAsync(2026, 8);

        // Sonuc satiri silinip yeniden eklenmiyor, yerinde guncelleniyor.
        db.CommissionResults.Should().HaveCount(1);
        db.CommissionResultLines.Should().HaveCount(1);
    }

    [Fact]
    public async Task Kapali_donemde_hesap_calistirilamaz()
    {
        using (var setup = NewContext())
        {
            setup.Periods.Add(new Period { Year = 2026, Month = 8, Status = PeriodStatus.Closed });
            setup.SaveChanges();
        }

        using var db = NewContext();
        var service = NewService(db);

        var act = () => service.RunPeriodAsync(2026, 8);

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("PERIOD_CLOSED");
    }

    // ---- kurulum ----

    private CommissionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CommissionDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ClosedPeriodGuardInterceptor(), new AuditSaveChangesInterceptor(_user))
            .Options;

        var db = new CommissionDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private CommissionService NewService(CommissionDbContext db) => new(
        new EmployeeRepository(db),
        new SaleRecordRepository(db),
        new CommissionRuleRepository(db),
        new CommissionResultRepository(db),
        new PeriodRepository(db),
        TestData.Calculator(),
        new UnitOfWork(db),
        _user);

    private static void Seed(CommissionDbContext db)
    {
        db.Departments.Add(new Department { Id = DepartmentId, Code = "SPA", Name = "SPA" });
        db.Hotels.Add(new Hotel { Id = HotelId, Code = "GSR", Name = "Gloria Serenity Resort" });
        db.ProductGroups.Add(new ProductGroup { Id = GroupId, Code = "SPA", Name = "SPA" });

        db.Employees.Add(new Employee
        {
            Id = EmployeeId,
            EmployeeNo = "P1001",
            FullName = "Test Personel",
            DepartmentId = DepartmentId,
            HotelId = HotelId,
            HireDate = new DateOnly(2024, 1, 1)
        });

        db.ImportBatches.Add(new ImportBatch
        {
            Id = BatchId,
            SourceSystem = SourceSystem.Pms,
            FileName = "test.csv",
            FileHash = "hash"
        });

        db.CommissionRules.Add(new CommissionRule
        {
            Code = "SPA-PCT",
            Name = "SPA sabit yuzde",
            RuleType = CommissionRuleType.Percentage,
            ProductGroupId = GroupId,
            Rate = 0.06m,
            EffectiveFrom = new DateOnly(2026, 1, 1)
        });

        db.SaleRecords.Add(new SaleRecord
        {
            SourceSystem = SourceSystem.Pms,
            SourceDocumentNo = "DOC1",
            SourceHash = "hash1",
            TransactionDate = new DateOnly(2026, 8, 10),
            SourceEmployeeNo = "P1001",
            EmployeeId = EmployeeId,
            ProductCode = "SPA_MSJ60",
            ProductName = "SPA Masaj 60dk",
            ProductGroupId = GroupId,
            Quantity = 1,
            Amount = 10_000m,
            AmountTry = 10_000m,
            Currency = "TRY",
            ImportBatchId = BatchId
        });

        db.SaveChanges();
    }

    private sealed class FakeUser : ICurrentUser
    {
        public string UserId => "test-user";
        public UserRole Role => UserRole.Admin;
        public string? EmployeeNo => null;
    }
}
