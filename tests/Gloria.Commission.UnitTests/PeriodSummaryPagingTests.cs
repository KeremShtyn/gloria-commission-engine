using FluentAssertions;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>
/// Donem ozetinin sayfalanmasi ve siralanmasi.
///
/// Iki davranis ozellikle korunuyor: donem toplamlari sayfa degistikce degismemeli
/// (yoksa ikinci sayfada "donem toplami" yariya duser) ve gecersiz sorgu parametresi
/// sessizce varsayilana dusmemeli (yoksa istemci eksik veriyle calistigini fark etmez).
/// </summary>
public class PeriodSummaryPagingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FakeUser _user = new();

    private static readonly Guid DepartmentId = Guid.Parse("bb000000-0000-0000-0000-000000000001");
    private static readonly Guid HotelId = Guid.Parse("bb000000-0000-0000-0000-000000000002");
    private static readonly Guid GroupId = Guid.Parse("bb000000-0000-0000-0000-000000000003");
    private static readonly Guid BatchId = Guid.Parse("bb000000-0000-0000-0000-000000000004");

    /// <summary>Ad, personel no ve ciro; uc alan da farkli siralama uretecek sekilde secildi.</summary>
    private static readonly (string No, string Name, decimal Amount)[] People =
    [
        ("P1003", "Ali Vural", 30_000m),
        ("P1001", "Çetin Aksu", 10_000m),
        ("P1002", "Zeynep Arslan", 20_000m),
    ];

    public PeriodSummaryPagingTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var db = NewContext();
        Seed(db);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Sayfa_istenen_boyutta_doner_ve_toplam_sayfa_hesaplanir()
    {
        using var db = NewContext();

        var summary = await NewService(db).GetPeriodSummaryAsync(2026, 8, 0, 2, null);

        summary.Employees.Content.Should().HaveCount(2);
        summary.Employees.Page.Should().Be(0);
        summary.Employees.Size.Should().Be(2);
        summary.Employees.TotalElements.Should().Be(3);
        summary.Employees.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Donem_toplamlari_sayfadan_bagimsizdir()
    {
        using var db = NewContext();
        var service = NewService(db);

        var first = await service.GetPeriodSummaryAsync(2026, 8, 0, 2, null);
        var second = await service.GetPeriodSummaryAsync(2026, 8, 1, 2, null);

        second.Employees.Content.Should().HaveCount(1);

        // Ikinci sayfada tek satir var ama donem toplami tum personeli kapsamali.
        second.TotalSalesBase.Should().Be(first.TotalSalesBase).And.Be(60_000m);
        second.TotalCommission.Should().Be(first.TotalCommission);
        second.EmployeeCount.Should().Be(3);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(200_000_000)] // page * size int sinirini asar
    public async Task Son_sayfanin_otesi_bos_doner(int page)
    {
        using var db = NewContext();

        var summary = await NewService(db).GetPeriodSummaryAsync(2026, 8, page, 20, null);

        summary.Employees.Content.Should().BeEmpty();
        summary.Employees.TotalElements.Should().Be(3);
    }

    [Fact]
    public async Task Varsayilan_siralama_ada_gore_turkce_harf_sirasindadir()
    {
        using var db = NewContext();

        var summary = await NewService(db).GetPeriodSummaryAsync(2026, 8, null, null, null);

        // 'Ç' harfi ordinal siralamada 'Z'den sonra gelirdi; Turkce siralamada 'C' ile 'D' arasinda.
        summary.Employees.Content.Select(e => e.FullName)
            .Should().ContainInOrder("Ali Vural", "Çetin Aksu", "Zeynep Arslan");
    }

    [Fact]
    public async Task Prime_gore_azalan_siralama_uygulanir()
    {
        using var db = NewContext();

        var summary = await NewService(db)
            .GetPeriodSummaryAsync(2026, 8, null, null, "totalCommission,desc");

        summary.Employees.Content.Select(e => e.EmployeeNo)
            .Should().ContainInOrder("P1003", "P1002", "P1001");
    }

    [Fact]
    public async Task Siralanamayan_alan_sessizce_yok_sayilmaz()
    {
        using var db = NewContext();
        var service = NewService(db);

        var act = () => service.GetPeriodSummaryAsync(2026, 8, null, null, "maas,desc");

        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be("INVALID_QUERY");
    }

    [Theory]
    [InlineData(-1, 20, null)]
    [InlineData(0, 101, null)]
    [InlineData(0, 20, "fullName,ters")]
    [InlineData(0, 20, "fullName,asc,desc")]
    public void Gecersiz_sayfa_istegi_reddedilir(int page, int size, string? sort)
    {
        var act = () => PageQuery.Parse(page, size, sort, ["fullName"]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("INVALID_QUERY");
    }

    [Fact]
    public void Sayfa_istegi_verilmezse_varsayilanlar_uygulanir()
    {
        var query = PageQuery.Parse(null, null, null, ["fullName"]);

        query.Page.Should().Be(0);
        query.Size.Should().Be(PageQuery.DefaultSize);
        query.SortField.Should().BeNull();
    }

    /// <summary>
    /// Sifir ve negatif size bir talep degil, bos ya da hatali form degeri sayilir;
    /// istegi reddetmek yerine varsayilan uygulanir. Ust sinir bundan farkli:
    /// 500 satir isteyip 20 alan istemci eksik veriyle calistigini fark etmez.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Sifir_ve_negatif_size_varsayilana_duser(int size)
    {
        var query = PageQuery.Parse(0, size, null, ["fullName"]);

        query.Size.Should().Be(PageQuery.DefaultSize);
    }

    // ---- kurulum ----

    private CommissionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<CommissionDbContext>()
            .UseSqlite(_connection)
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

        foreach (var (no, name, amount) in People)
        {
            var employeeId = Guid.NewGuid();

            db.Employees.Add(new Employee
            {
                Id = employeeId,
                EmployeeNo = no,
                FullName = name,
                DepartmentId = DepartmentId,
                HotelId = HotelId,
                HireDate = new DateOnly(2024, 1, 1)
            });

            db.SaleRecords.Add(new SaleRecord
            {
                SourceSystem = SourceSystem.Pms,
                SourceDocumentNo = $"DOC-{no}",
                SourceHash = $"hash-{no}",
                TransactionDate = new DateOnly(2026, 8, 10),
                SourceEmployeeNo = no,
                EmployeeId = employeeId,
                ProductCode = "SPA_MSJ60",
                ProductName = "SPA Masaj 60dk",
                ProductGroupId = GroupId,
                Quantity = 1,
                Amount = amount,
                AmountTry = amount,
                Currency = "TRY",
                ImportBatchId = BatchId
            });
        }

        db.SaveChanges();
    }

    private sealed class FakeUser : ICurrentUser
    {
        public string UserId => "test-user";
        public UserRole Role => UserRole.Admin;
        public string? EmployeeNo => null;
    }
}
