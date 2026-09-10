using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/periods")]
[Produces("application/json")]
[Tags("Donem")]
public class PeriodsController : ControllerBase
{
    private readonly IPeriodService _periodService;

    public PeriodsController(IPeriodService periodService) => _periodService = periodService;

    /// <summary>Donemleri ve kapali/acik durumlarini listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PeriodResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PeriodResponse>>> GetAll(CancellationToken ct)
        => Ok(await _periodService.GetAllAsync(ct));

    /// <summary>
    /// Donemi kapatir. Kapatildiktan sonra o doneme ait satis ve prim kayitlari degistirilemez.
    /// Admin rolu gerekir.
    /// </summary>
    [HttpPost("{year:int}/{month:int}/close")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PeriodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PeriodResponse>> Close(int year, int month, CancellationToken ct)
        => Ok(await _periodService.CloseAsync(year, month, ct));

    /// <summary>Donemi yeniden acar. Admin rolu gerekir ve islem audit log'a duser.</summary>
    [HttpPost("{year:int}/{month:int}/reopen")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PeriodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PeriodResponse>> Reopen(int year, int month, CancellationToken ct)
        => Ok(await _periodService.ReopenAsync(year, month, ct));
}
