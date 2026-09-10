using System.Text;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

[ApiController]
[Route("api/v1/imports")]
[Produces("application/json")]
[Tags("Veri Aktarimi")]
public class ImportsController : ControllerBase
{
    /// <summary>Tek seferde kabul edilen en buyuk dosya. Kaynak ekstreler bunun cok altinda kalir.</summary>
    private const long MaxFileBytes = 20 * 1024 * 1024;

    private readonly IImportService _importService;

    public ImportsController(IImportService importService) => _importService = importService;

    /// <summary>
    /// CSV dosyasini ana tablolara aktarir. source: pms | pos | erp.
    /// Mukerrer kayitlar yazilmaz, ayristirilamayan satirlar import_errors tablosuna loglanir.
    /// </summary>
    [HttpPost("{source}")]
    [ProducesResponseType(typeof(ImportSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ImportSummary>> Import(
        string source, IFormFile file, CancellationToken ct)
    {
        if (!Enum.TryParse<SourceSystem>(source, ignoreCase: true, out var sourceSystem))
            throw new DomainException("SOURCE_UNSUPPORTED",
                $"'{source}' gecerli bir kaynak degil. Beklenen: pms, pos, erp.");

        if (file is null || file.Length == 0)
            throw new DomainException("EMPTY_FILE", "Yuklenen dosya bos.");

        if (file.Length > MaxFileBytes)
            throw new DomainException("FILE_TOO_LARGE", "Dosya 20 MB sinirini asiyor.");

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var content = await reader.ReadToEndAsync(ct);

        return Ok(await _importService.ImportAsync(sourceSystem, file.FileName, content, ct));
    }

    /// <summary>Gecmis yukleme islemleri.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ImportBatchResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImportBatchResponse>>> GetBatches(CancellationToken ct)
        => Ok(await _importService.GetBatchesAsync(ct));

    /// <summary>Bir yuklemenin hatali satirlari, ham hali ve gerekcesiyle.</summary>
    [HttpGet("{batchId:int}/errors")]
    [ProducesResponseType(typeof(IReadOnlyList<ImportErrorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImportErrorResponse>>> GetErrors(
        int batchId, CancellationToken ct)
        => Ok(await _importService.GetErrorsAsync(batchId, ct));
}
