using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Produces("application/json")]
[Tags("Denetim")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService) => _auditLogService = auditLogService;

    /// <summary>
    /// Kural ve satis kayitlarindaki degisiklik gecmisi: kim, ne zaman, eski deger, yeni deger.
    /// Admin veya Muhasebe rolu gerekir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> Search(
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] int? page,
        [FromQuery] int? size,
        CancellationToken ct)
        => Ok(await _auditLogService.SearchAsync(entityName, entityId, page, size, ct));
}
