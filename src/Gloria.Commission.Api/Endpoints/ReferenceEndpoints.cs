using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Api.Endpoints;

/// <summary>Arayuzun acilis listeleri: personel, departman, urun grubu.</summary>
public static class ReferenceEndpoints
{
    public static void MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1").WithTags("Referans Veri");

        group.MapGet("/employees", async (CommissionDbContext db, CancellationToken ct) =>
            {
                var employees = await db.Employees
                    .Include(e => e.Department)
                    .OrderBy(e => e.EmployeeNo)
                    .Select(e => new
                    {
                        e.EmployeeNo,
                        e.FullName,
                        Department = e.Department.Code,
                        e.Hotel,
                        e.HireDate,
                        e.TerminationDate
                    })
                    .ToListAsync(ct);

                return Results.Ok(employees);
            })
            .WithSummary("Personel listesi");

        group.MapGet("/departments", async (CommissionDbContext db, CancellationToken ct) =>
            {
                var departments = await db.Departments
                    .OrderBy(d => d.Code)
                    .Select(d => new { d.Code, d.Name })
                    .ToListAsync(ct);

                return Results.Ok(departments);
            })
            .WithSummary("Departman listesi");

        group.MapGet("/product-groups", async (CommissionDbContext db, CancellationToken ct) =>
            {
                var groups = await db.SaleRecords
                    .Select(s => s.ProductGroup)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync(ct);

                return Results.Ok(groups);
            })
            .WithSummary("Satislarda gecen urun gruplari - kural tanimlarken kullanilir");
    }
}
