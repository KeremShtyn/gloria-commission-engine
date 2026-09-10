using Gloria.Commission.Application.Models;
using Gloria.Commission.Infrastructure.Services;

namespace Gloria.Commission.Api.Endpoints;

public static class RuleEndpoints
{
    public static void MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commission-rules").WithTags("Prim Kurallari");

        group.MapGet("/", async (IRuleService service, CancellationToken ct)
                => Results.Ok(await service.ListAsync(ct)))
            .WithSummary("Tum kurallari listeler");

        group.MapGet("/{id:int}", async (int id, IRuleService service, CancellationToken ct)
                => Results.Ok(await service.GetAsync(id, ct)))
            .WithSummary("Tek kural detayi");

        group.MapPost("/", async (
                CommissionRuleRequest request, IRuleService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/commission-rules/{created.Id}", created);
            })
            .WithSummary("Yeni kural olusturur (Admin)");

        group.MapPut("/{id:int}", async (
                int id, CommissionRuleRequest request, IRuleService service, CancellationToken ct)
                => Results.Ok(await service.UpdateAsync(id, request, ct)))
            .WithSummary("Kurali gunceller (Admin)");

        group.MapDelete("/{id:int}", async (int id, IRuleService service, CancellationToken ct) =>
            {
                await service.DeactivateAsync(id, ct);
                return Results.NoContent();
            })
            .WithSummary("Kurali pasife alir (Admin) - fiziksel silme yapilmaz");
    }
}
