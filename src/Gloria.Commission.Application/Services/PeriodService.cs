using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Mappers;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

/// <summary>
/// Donem kapatma. Kapali donemin kayitlarini asil koruyan sey bu servis degil,
/// veri erisim katmanindaki kilit; burasi yalnizca durumu yonetir ve yetkiyi denetler.
/// </summary>
public sealed class PeriodService : IPeriodService
{
    private readonly IPeriodRepository _periods;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public PeriodService(IPeriodRepository periods, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _periods = periods;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<PeriodResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var periods = await _periods.FindAllAsync(ct);
        return periods.Select(PeriodMapper.ToResponse).ToList();
    }

    public async Task<PeriodResponse> CloseAsync(int year, int month, CancellationToken ct = default)
    {
        RequireAdmin();
        RequireValidMonth(month);

        // Donem kaydi normalde ilk hesaplamada olusur. Hic hesaplanmamis bir ayi kapatmak da
        // gecerli bir islem: "bu aya artik yazma" demek, bu yuzden kayit burada da yaratilir.
        var period = await _periods.FindAsync(year, month, ct);

        if (period is null)
        {
            period = new Period { Year = year, Month = month, Status = PeriodStatus.Open };
            _periods.Add(period);
        }

        if (period.IsClosed)
            throw new DomainException("PERIOD_ALREADY_CLOSED", $"{period} donemi zaten kapali.");

        period.Status = PeriodStatus.Closed;
        period.ClosedAtUtc = DateTime.UtcNow;
        period.ClosedBy = _currentUser.UserId;

        await _unitOfWork.SaveChangesAsync(ct);

        return PeriodMapper.ToResponse(period);
    }

    /// <summary>
    /// Donemi yeniden acar. Kapali donemin acilmasi denetlenmesi gereken bir olay:
    /// yalnizca Admin yapabilir ve islem audit log'a duser.
    /// </summary>
    public async Task<PeriodResponse> ReopenAsync(int year, int month, CancellationToken ct = default)
    {
        RequireAdmin();
        RequireValidMonth(month);

        var period = await _periods.FindAsync(year, month, ct)
                     ?? throw new DomainException("PERIOD_NOT_FOUND",
                         $"{Period.Key(year, month)} donemi yok.");

        if (!period.IsClosed)
            throw new DomainException("PERIOD_NOT_CLOSED", $"{period} donemi zaten acik.");

        period.Status = PeriodStatus.Open;
        period.ClosedAtUtc = null;
        period.ClosedBy = null;

        await _unitOfWork.SaveChangesAsync(ct);

        return PeriodMapper.ToResponse(period);
    }

    private void RequireAdmin()
    {
        if (!_currentUser.IsAdmin)
            throw new DomainException("FORBIDDEN", "Donem kapatma/acma icin Admin rolu gerekir.");
    }

    private static void RequireValidMonth(int month)
    {
        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");
    }
}
