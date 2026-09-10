using Gloria.Commission.Infrastructure.Services;

namespace Gloria.Commission.Api.Endpoints;

public static class CommissionEndpoints
{
    public static void MapCommissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commissions").WithTags("Prim Hesaplama");

        group.MapGet("/{year:int}/{month:int}/employees/{employeeNo}", async (
                int year, int month, string employeeNo,
                ICommissionService service, CancellationToken ct)
                => Results.Ok(await service.CalculateAsync(year, month, employeeNo, ct)))
            .WithSummary("Bir personelin donem primini hesaplama adimlariyla dondurur")
            .WithDescription(
                "Personel rolu yalnizca kendi numarasini sorgulayabilir. " +
                "Cevap; hangi satis, hangi kural, hangi oran ve ara toplamlar bilgisini icerir.");

        group.MapGet("/{year:int}/{month:int}", async (
                int year, int month, ICommissionService service, CancellationToken ct)
                => Results.Ok(await service.CalculatePeriodAsync(year, month, ct)))
            .WithSummary("Donemin tum personel ozeti (Admin / Muhasebe)");

        group.MapGet("/{year:int}/{month:int}/reconciliation", async (
                int year, int month, IReconciliationService service, CancellationToken ct)
                => Results.Ok(await service.GetAsync(year, month, ct)))
            .WithSummary("ERP mutabakat raporu")
            .WithDescription(
                "Operasyonel ciro (PMS+POS) ile muhasebelesmis ciroyu (ERP) urun grubu bazinda karsilastirir.");
    }
}
