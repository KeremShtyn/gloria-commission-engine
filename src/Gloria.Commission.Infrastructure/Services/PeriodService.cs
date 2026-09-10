using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Services;

public interface IPeriodService
{
    Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct = default);
    Task<PeriodDto> CloseAsync(int year, int month, CancellationToken ct = default);
    Task<PeriodDto> ReopenAsync(int year, int month, CancellationToken ct = default);
}

public sealed record PeriodDto
{
    public string Key { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ClosedAtUtc { get; init; }
    public string? ClosedBy { get; init; }
}

/// <summary>
/// Donem kapatma. Kapali donemin satis ve prim kayitlari
/// <see cref="Persistence.Interceptors.ClosedPeriodGuardInterceptor"/> tarafindan korunur.
/// </summary>
public sealed class PeriodService : IPeriodService
{
    private readonly CommissionDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PeriodService(CommissionDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct = default)
    {
        var periods = await _db.Periods
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .AsNoTracking()
            .ToListAsync(ct);

        return periods.Select(ToDto).ToList();
    }

    public async Task<PeriodDto> CloseAsync(int year, int month, CancellationToken ct = default)
    {
        RequireAdmin();

        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");

        // Donem kaydi normalde ilk hesaplamada olusur. Hic hesaplanmamis bir ayi kapatmak da
        // gecerli bir islem: "bu aya artik yazma" demek, bu yuzden kayit burada da yaratilir.
        var period = await _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

        if (period is null)
        {
            period = new Period { Year = year, Month = month, Status = PeriodStatus.Open };
            _db.Periods.Add(period);
        }

        if (period.IsClosed)
            throw new DomainException("PERIOD_ALREADY_CLOSED", $"{period} donemi zaten kapali.");

        period.Status = PeriodStatus.Closed;
        period.ClosedAtUtc = DateTime.UtcNow;
        period.ClosedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
        return ToDto(period);
    }

    /// <summary>
    /// Donemi yeniden acar. Yalnizca Admin yapabilir ve islem audit log'a duser —
    /// kapali donemin acilmasi denetlenmesi gereken bir olaydir.
    /// </summary>
    public async Task<PeriodDto> ReopenAsync(int year, int month, CancellationToken ct = default)
    {
        RequireAdmin();

        var period = await _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct)
                     ?? throw new DomainException("PERIOD_NOT_FOUND", $"{Period.Key(year, month)} donemi yok.");

        if (!period.IsClosed)
            throw new DomainException("PERIOD_NOT_CLOSED", $"{period} donemi zaten acik.");

        period.Status = PeriodStatus.Open;
        period.ClosedAtUtc = null;
        period.ClosedBy = null;

        await _db.SaveChangesAsync(ct);
        return ToDto(period);
    }

    private void RequireAdmin()
    {
        if (!_currentUser.IsAdmin)
            throw new DomainException("FORBIDDEN", "Donem kapatma/acma icin Admin rolu gerekir.");
    }

    private static PeriodDto ToDto(Period p) => new()
    {
        Key = p.ToString(),
        Year = p.Year,
        Month = p.Month,
        Status = p.Status.ToString(),
        ClosedAtUtc = p.ClosedAtUtc,
        ClosedBy = p.ClosedBy
    };
}
