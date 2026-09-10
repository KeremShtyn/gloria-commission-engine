using System.Text.Json;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gloria.Commission.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Denetim kaydini SaveChanges icinde uretir. Servis katmani audit yazmayi unutamaz,
/// cunku kayit veri erisim katmaninda otomatik olusur.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    /// <summary>Denetlenen tipler: primi ve maasi etkileyen her sey.</summary>
    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(CommissionRule),
        typeof(CommissionRuleTier),
        typeof(SaleRecord),
        typeof(Period)
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly ICurrentUser _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUser currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        WriteAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        WriteAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAuditLogs(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => AuditedTypes.Contains(e.Entity.GetType()))
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0) return;

        var logs = new List<AuditLog>(entries.Count);
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var (action, oldValues, newValues) = Describe(entry);

            // Modified olarak isaretlenmis ama hicbir alani degismemis kayit icin log uretme.
            if (action == AuditAction.Update && oldValues is null) continue;

            logs.Add(new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = PrimaryKeyOf(entry),
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedBy = _currentUser.UserId,
                ChangedByRole = _currentUser.Role.ToString(),
                ChangedAtUtc = now
            });
        }

        if (logs.Count > 0) context.Set<AuditLog>().AddRange(logs);
    }

    private static (AuditAction Action, string? Old, string? New) Describe(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                return (AuditAction.Create, null, Serialize(Snapshot(entry, current: true)));

            case EntityState.Deleted:
                return (AuditAction.Delete, Serialize(Snapshot(entry, current: false)), null);

            default:
                var changed = entry.Properties.Where(p => p.IsModified).ToList();
                if (changed.Count == 0) return (AuditAction.Update, null, null);

                var before = changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                var after = changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                return (AuditAction.Update, Serialize(before), Serialize(after));
        }
    }

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool current)
        => entry.Properties.ToDictionary(
            p => p.Metadata.Name,
            p => current ? p.CurrentValue : p.OriginalValue);

    private static string Serialize(Dictionary<string, object?> values)
        => JsonSerializer.Serialize(values, JsonOptions);

    private static string PrimaryKeyOf(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return "?";

        var parts = key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "?");
        return string.Join(":", parts);
    }
}
