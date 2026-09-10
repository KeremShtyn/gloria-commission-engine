using FluentAssertions;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Import;
using Gloria.Commission.Infrastructure.Services;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Persistence.Interceptors;
using Gloria.Commission.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>
/// Aktarimin iki garantisi: her orijinal satis en fazla bir iadeyi karsilar ve
/// yarim kalan bir aktarim geride kayit birakmaz.
/// </summary>
public class ImportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FakeUser _user = new();

    private static readonly Guid DepartmentId = Guid.Parse("cc000000-0000-0000-0000-000000000001");
    private static readonly Guid HotelId = Guid.Parse("cc000000-0000-0000-0000-000000000002");
    private static readonly Guid GroupId = Guid.Parse("cc000000-0000-0000-0000-000000000003");

    private const string Header =
        "BelgeNo;IslemTarihi;OdaNo;MisafirAdi;UrunKodu;UrunAdi;Adet;Tutar;ParaBirimi;KasiyerNo;IslemTipi;Otel";

    public ImportTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var db = NewContext();
        Seed(db);
    }

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Iki iade ayni satisa uyuyor: personel, urun ve tutar ayni. Yalnizca biri
    /// eslesmeli. Eslesme veritabanina sorularak yapildiginda henuz kaydedilmemis
    /// "Reversed" isareti sorguya yansimiyor ve ayni satir iki kez tuketilebiliyordu.
    /// </summary>
    [Fact]
    public async Task Bir_orijinal_satis_yalnizca_bir_iadeyi_karsilar()
    {
        using var db = NewContext();

        var csv = string.Join('\n',
            Header,
            "900001;2026-08-05;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;2800.00;TRY;P1001;POSTING;GSR",
            "900002;2026-08-06;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;-2800.00;TRY;P1001;REVERSAL;GSR",
            "900003;2026-08-07;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;-2800.00;TRY;P1001;REVERSAL;GSR");

        var summary = await NewService(db).ImportAsync(SourceSystem.Pms, "pms.csv", csv);

        summary.ImportedRows.Should().Be(3);
        summary.MatchedReversals.Should().Be(1);

        using var check = NewContext();
        var sales = await check.SaleRecords.AsNoTracking().ToListAsync();

        sales.Count(s => s.Status == SaleStatus.Reversed).Should().Be(1);
        sales.Count(s => s.ReversedSaleId != null).Should().Be(1);
    }

    [Fact]
    public async Task Belge_referansi_olan_iade_dogrudan_orijinaline_baglanir()
    {
        using var db = NewContext();

        var csv = string.Join('\n',
            Header,
            "900010;2026-08-05;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;2800.00;TRY;P1001;POSTING;GSR",
            "900011;2026-08-05;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;4000.00;TRY;P1001;POSTING;GSR",
            "900012;2026-08-06;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;-4000.00;TRY;P1001;REVERSAL;GSR");

        await NewService(db).ImportAsync(SourceSystem.Pms, "pms.csv", csv);

        using var check = NewContext();
        var reversed = await check.SaleRecords.AsNoTracking()
            .SingleAsync(s => s.Status == SaleStatus.Reversed);

        // Tutar esitligi belirleyici: 2.800 TL'lik satis degil, 4.000 TL'lik olan iptal edilmeli.
        reversed.AmountTry.Should().Be(4000m);
    }

    /// <summary>
    /// Aktarim ortasinda hata cikarsa parti kaydi da satislar da geride kalmamali.
    /// Aksi halde aktarim gecmisinde sayilari sifir gorunen, aslinda satir yazmis
    /// bir parti kaliyordu.
    /// </summary>
    [Fact]
    public async Task Yarida_kalan_aktarim_geride_kayit_birakmaz()
    {
        using var db = NewContext();

        var csv = string.Join('\n',
            Header,
            "900020;2026-08-05;101;Guest;SPA_MSJ60;SPA Masaj 60dk;1;2800.00;TRY;P1001;POSTING;GSR");

        // Ikinci SaveChanges'te patlar: parti ve ham satirlar yazilmis, satislar yazilmak uzere.
        var service = NewService(db, new FailingUnitOfWork(new UnitOfWork(db), failOnCall: 2));

        var act = () => service.ImportAsync(SourceSystem.Pms, "pms.csv", csv);

        await act.Should().ThrowAsync<InvalidOperationException>();

        using var check = NewContext();
        check.ImportBatches.Should().BeEmpty();
        check.StagingRows.Should().BeEmpty();
        check.SaleRecords.Should().BeEmpty();
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

    private ImportService NewService(CommissionDbContext db, IUnitOfWork? unitOfWork = null) => new(
        [new PmsImporter(new StaticExchangeRateProvider())],
        new EmployeeRepository(db),
        new ProductGroupRepository(db),
        new SaleRecordRepository(db),
        new ImportRepository(db),
        new PeriodRepository(db),
        unitOfWork ?? new UnitOfWork(db),
        _user);

    private static void Seed(CommissionDbContext db)
    {
        db.Departments.Add(new Department { Id = DepartmentId, Code = "SPA", Name = "SPA" });
        db.Hotels.Add(new Hotel { Id = HotelId, Code = "GSR", Name = "Gloria Serenity Resort" });
        db.ProductGroups.Add(new ProductGroup { Id = GroupId, Code = "SPA", Name = "SPA" });

        db.Employees.Add(new Employee
        {
            EmployeeNo = "P1001",
            FullName = "Test Personel",
            DepartmentId = DepartmentId,
            HotelId = HotelId,
            HireDate = new DateOnly(2024, 1, 1)
        });

        db.SaveChanges();
    }

    /// <summary>Belirtilen sıradaki <c>SaveChanges</c> cagrisinda hata firlatir.</summary>
    private sealed class FailingUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly int _failOnCall;
        private int _calls;

        public FailingUnitOfWork(IUnitOfWork inner, int failOnCall)
        {
            _inner = inner;
            _failOnCall = failOnCall;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            if (++_calls == _failOnCall)
                throw new InvalidOperationException("Aktarim ortasinda beklenmeyen hata.");

            return _inner.SaveChangesAsync(ct);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
            => _inner.ExecuteInTransactionAsync(operation, ct);
    }

    private sealed class FakeUser : ICurrentUser
    {
        public string UserId => "test-user";
        public UserRole Role => UserRole.Admin;
        public string? EmployeeNo => null;
    }
}
