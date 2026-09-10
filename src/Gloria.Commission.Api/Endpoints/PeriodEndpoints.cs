using Gloria.Commission.Application.Models;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Infrastructure.Persistence;
using Gloria.Commission.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Api.Endpoints;

public static class PeriodEndpoints
{
    public static void MapPeriodEndpoints(this IEndpointRouteBuilder app)
    {
        var periods = app.MapGroup("/api/v1/periods").WithTags("Donem");

        periods.MapGet("/", async (IPeriodService service, CancellationToken ct)
                => Results.Ok(await service.ListAsync(ct)))
            .WithSummary("Donemleri ve kapali/acik durumlarini listeler");

        periods.MapPost("/{year:int}/{month:int}/close", async (
                int year, int month, IPeriodService service, CancellationToken ct)
                => Results.Ok(await service.CloseAsync(year, month, ct)))
            .WithSummary("Donemi kapatir (Admin)")
            .WithDescription("Kapatildiktan sonra o doneme ait satis ve prim kayitlari degistirilemez.");

        periods.MapPost("/{year:int}/{month:int}/reopen", async (
                int year, int month, IPeriodService service, CancellationToken ct)
                => Results.Ok(await service.ReopenAsync(year, month, ct)))
            .WithSummary("Donemi yeniden acar (Admin) - audit log'a duser");

        var audit = app.MapGroup("/api/v1/audit-logs").WithTags("Denetim");

        audit.MapGet("/", async (
                string? entityName, string? entityId, int? page, int? size,
                CommissionDbContext db, Gloria.Commission.Application.Abstractions.ICurrentUser currentUser,
                CancellationToken ct) =>
            {
                if (!currentUser.CanSeeAllEmployees)
                    throw new DomainException("FORBIDDEN", "Denetim kayitlari icin Admin veya Muhasebe rolu gerekir.");

                var pageSize = size is null or <= 0 or > 100 ? 20 : size.Value;
                var pageIndex = page is null or < 0 ? 0 : page.Value;

                var query = db.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(entityName))
                    query = query.Where(a => a.EntityName == entityName);

                if (!string.IsNullOrWhiteSpace(entityId))
                    query = query.Where(a => a.EntityId == entityId);

                var total = await query.CountAsync(ct);

                var items = await query
                    .OrderByDescending(a => a.ChangedAtUtc).ThenByDescending(a => a.Id)
                    .Skip(pageIndex * pageSize).Take(pageSize)
                    .Select(a => new AuditLogDto
                    {
                        Id = a.Id,
                        EntityName = a.EntityName,
                        EntityId = a.EntityId,
                        Action = a.Action.ToString(),
                        OldValues = a.OldValues,
                        NewValues = a.NewValues,
                        ChangedBy = a.ChangedBy,
                        ChangedByRole = a.ChangedByRole,
                        ChangedAtUtc = a.ChangedAtUtc
                    })
                    .ToListAsync(ct);

                return Results.Ok(new
                {
                    content = items,
                    page = pageIndex,
                    size = pageSize,
                    totalElements = total,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            })
            .WithSummary("Kural ve satis kayitlarindaki degisiklik gecmisi");
    }
}
