using System.Text;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Import;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Api.Endpoints;

public static class ImportEndpoints
{
    /// <summary>Tek seferde kabul edilen en buyuk dosya. Kaynak ekstreler bunun cok altinda kalir.</summary>
    private const long MaxFileBytes = 20 * 1024 * 1024;

    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/imports").WithTags("Veri Aktarimi");

        group.MapPost("/{source}", async (
                string source,
                IFormFile file,
                IImportService service,
                ICurrentUser currentUser,
                CancellationToken ct) =>
            {
                if (!currentUser.CanSeeAllEmployees)
                    throw new DomainException("FORBIDDEN", "Veri aktarimi icin Admin veya Muhasebe rolu gerekir.");

                if (!Enum.TryParse<SourceSystem>(source, ignoreCase: true, out var sourceSystem))
                    throw new DomainException("SOURCE_UNSUPPORTED",
                        $"'{source}' gecerli bir kaynak degil. Beklenen: pms, pos, erp.");

                if (file.Length == 0)
                    throw new DomainException("EMPTY_FILE", "Yuklenen dosya bos.");

                if (file.Length > MaxFileBytes)
                    throw new DomainException("FILE_TOO_LARGE", "Dosya 20 MB sinirini asiyor.");

                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                var content = await reader.ReadToEndAsync(ct);

                var summary = await service.ImportAsync(sourceSystem, file.FileName, content, ct);
                return Results.Ok(summary);
            })
            .DisableAntiforgery()
            .WithSummary("CSV dosyasini ana tablolara aktarir")
            .WithDescription(
                "source: pms | pos | erp. Mukerrer kayitlar yazilmaz, " +
                "ayristirilamayan satirlar import_errors tablosuna loglanir.");

        group.MapGet("/", async (CommissionDbContext db, CancellationToken ct) =>
            {
                var batches = await db.ImportBatches
                    .OrderByDescending(b => b.StartedAtUtc)
                    .Select(b => new
                    {
                        b.Id,
                        Source = b.SourceSystem.ToString(),
                        b.FileName,
                        b.TotalRows,
                        b.ImportedRows,
                        b.DuplicateRows,
                        b.FailedRows,
                        b.ImportedBy,
                        b.StartedAtUtc,
                        b.CompletedAtUtc
                    })
                    .ToListAsync(ct);

                return Results.Ok(batches);
            })
            .WithSummary("Gecmis yukleme islemleri");

        group.MapGet("/{batchId:int}/errors", async (
                int batchId, CommissionDbContext db, CancellationToken ct) =>
            {
                var errors = await db.ImportErrors
                    .Where(e => e.ImportBatchId == batchId)
                    .OrderBy(e => e.RowNumber)
                    .Select(e => new { e.RowNumber, e.ErrorCode, e.ErrorMessage, e.RawLine })
                    .ToListAsync(ct);

                return Results.Ok(errors);
            })
            .WithSummary("Bir yuklemenin hatali satirlari");
    }
}
