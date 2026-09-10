using FluentAssertions;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Persistence.Interceptors;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>
/// Denetim kaydi ve donem kilidi interceptor'da calisiyor; testler de veri erisim
/// katmanindan geciyor ki garanti gercekten dogrulanmis olsun.
/// </summary>
public class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FakeCurrentUser _user = new();

    public PersistenceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

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

    [Fact]
    public void Kural_degisikligi_eski_ve_yeni_degerle_loglanir()
    {
        using var db = NewContext();

        var rule = new CommissionRule
        {
            Code = "SPA-PCT",
            Name = "SPA",
            RuleType = CommissionRuleType.Percentage,
            Rate = 0.06m,
            EffectiveFrom = new DateOnly(2026, 1, 1)
        };

        db.CommissionRules.Add(rule);
        db.SaveChanges();

        rule.Rate = 0.07m;
        db.SaveChanges();

        var log = db.AuditLogs.Single(a => a.EntityName == nameof(CommissionRule)
                                           && a.Action == AuditAction.Update);

        log.OldValues.Should().Contain("0.06");
        log.NewValues.Should().Contain("0.07");
        log.ChangedBy.Should().Be("test-user");
        log.ChangedByRole.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void Kapali_doneme_satis_yazilamaz()
    {
        using var db = NewContext();
        Seed(db);

        db.Periods.Add(new Period { Year = 2026, Month = 8, Status = PeriodStatus.Closed });
        db.SaveChanges();

        db.SaleRecords.Add(NewSale(new DateOnly(2026, 8, 15)));

        var act = () => db.SaveChanges();

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("PERIOD_CLOSED");
    }

    [Fact]
    public void Acik_doneme_satis_yazilabilir()
    {
        using var db = NewContext();
        Seed(db);

        db.Periods.Add(new Period { Year = 2026, Month = 8, Status = PeriodStatus.Closed });
        db.SaveChanges();

        // Eylul acik: agustos kilidi diger donemleri etkilemez.
        db.SaleRecords.Add(NewSale(new DateOnly(2026, 9, 15)));
        db.SaveChanges();

        db.SaleRecords.Should().ContainSingle();
    }

    [Fact]
    public void Kapali_donemin_mevcut_satisi_da_degistirilemez()
    {
        using var db = NewContext();
        Seed(db);

        var sale = NewSale(new DateOnly(2026, 8, 15));
        db.SaleRecords.Add(sale);
        db.SaveChanges();

        db.Periods.Add(new Period { Year = 2026, Month = 8, Status = PeriodStatus.Closed });
        db.SaveChanges();

        sale.AmountTry = 999_999m;

        var act = () => db.SaveChanges();

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("PERIOD_CLOSED");
    }

    private static readonly Guid TestBatchId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid TestEmployeeId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    private static readonly Guid TestProductGroupId = Guid.Parse("11111111-0000-0000-0000-000000000003");
    private static readonly Guid TestDepartmentId = Guid.Parse("11111111-0000-0000-0000-000000000004");
    private static readonly Guid TestHotelId = Guid.Parse("11111111-0000-0000-0000-000000000005");

    private static void Seed(CommissionDbContext db)
    {
        db.Departments.Add(new Department { Id = TestDepartmentId, Code = "SPA", Name = "SPA" });
        db.Hotels.Add(new Hotel { Id = TestHotelId, Code = "GSR", Name = "Gloria Serenity Resort" });
        db.ProductGroups.Add(new ProductGroup { Id = TestProductGroupId, Code = "SPA", Name = "SPA" });
        db.Employees.Add(new Employee
        {
            Id = TestEmployeeId,
            EmployeeNo = "P1001",
            FullName = "Test Personel",
            DepartmentId = TestDepartmentId,
            HotelId = TestHotelId,
            HireDate = new DateOnly(2024, 1, 1)
        });
        db.ImportBatches.Add(new ImportBatch
        {
            Id = TestBatchId,
            SourceSystem = SourceSystem.Pms,
            FileName = "test.csv",
            FileHash = "hash"
        });
        db.SaveChanges();
    }

    private static SaleRecord NewSale(DateOnly date) => new()
    {
        SourceSystem = SourceSystem.Pms,
        SourceDocumentNo = $"DOC{date:yyyyMMdd}",
        SourceHash = $"hash{date:yyyyMMdd}",
        TransactionDate = date,
        SourceEmployeeNo = "P1001",
        EmployeeId = TestEmployeeId,
        ProductCode = "SPA_MSJ60",
        ProductName = "SPA Masaj 60dk",
        ProductGroupId = TestProductGroupId,
        Quantity = 1,
        Amount = 2800m,
        AmountTry = 2800m,
        Currency = "TRY",
        ImportBatchId = TestBatchId
    };

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserId => "test-user";
        public UserRole Role => UserRole.Admin;
        public string? EmployeeNo => null;
    }
}
