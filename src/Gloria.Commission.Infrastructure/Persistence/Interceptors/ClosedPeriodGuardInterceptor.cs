using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gloria.Commission.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kapali doneme ait kayitlarin degistirilmesini engeller.
/// Kontrol servis katmaninda degil burada: hangi yoldan gelinirse gelinsin kural gecerli.
/// </summary>
public sealed class ClosedPeriodGuardInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Guard(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Guard(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Guard(DbContext? context)
    {
        if (context is null) return;

        var touchedSales = context.ChangeTracker.Entries<SaleRecord>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity)
            .ToList();

        var touchedResults = context.ChangeTracker.Entries<CommissionResult>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity)
            .ToList();

        if (touchedSales.Count == 0 && touchedResults.Count == 0) return;

        var closed = context.Set<Period>()
            .Where(p => p.Status == PeriodStatus.Closed)
            .Select(p => new { p.Id, p.Year, p.Month })
            .ToList();

        if (closed.Count == 0) return;

        var closedKeys = closed.Select(p => (p.Year, p.Month)).ToHashSet();
        var closedIds = closed.Select(p => p.Id).ToHashSet();

        var blockedSale = touchedSales.FirstOrDefault(s =>
            closedKeys.Contains((s.TransactionDate.Year, s.TransactionDate.Month)));

        if (blockedSale is not null)
            throw new DomainException("PERIOD_CLOSED",
                $"{Period.Key(blockedSale.TransactionDate.Year, blockedSale.TransactionDate.Month)} donemi kapali; " +
                "satis kayitlari degistirilemez.");

        var blockedResult = touchedResults.FirstOrDefault(r => closedIds.Contains(r.PeriodId));

        if (blockedResult is not null)
            throw new DomainException("PERIOD_CLOSED",
                "Kapali doneme ait prim sonuclari yeniden hesaplanamaz.");
    }
}
