using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/commissions")]
[Produces("application/json")]
[Tags("Prim Hesaplama")]
public class CommissionsController : ControllerBase
{
    private readonly ICommissionService _commissionService;
    private readonly IReconciliationService _reconciliationService;

    public CommissionsController(
        ICommissionService commissionService, IReconciliationService reconciliationService)
    {
        _commissionService = commissionService;
        _reconciliationService = reconciliationService;
    }

    /// <summary>
    /// Bir personelin donem primini hesaplama adimlariyla birlikte dondurur:
    /// hangi satis, hangi kural, hangi oran, ara toplamlar.
    /// Personel rolu yalnizca kendi numarasini sorgulayabilir.
    /// </summary>
    [HttpGet("{year:int}/{month:int}/employees/{employeeNo}")]
    [Authorize(Policy = Policies.SelfOrPrivileged)]
    [ProducesResponseType(typeof(CommissionResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionResultResponse>> GetForEmployee(
        int year, int month, string employeeNo, CancellationToken ct)
        => Ok(await _commissionService.GetForEmployeeAsync(year, month, employeeNo, ct));

    /// <summary>
    /// Donemin tum personel ozeti. Admin veya Muhasebe rolu gerekir.
    /// Personel listesi sayfalidir: <c>?page=0&amp;size=20&amp;sort=totalCommission,desc</c>.
    /// Siralanabilir alanlar: fullName, employeeNo, department, hotel, totalSalesBase,
    /// totalCommission. Donem toplamlari sayfadan bagimsiz, tum personeli kapsar.
    /// </summary>
    [HttpGet("{year:int}/{month:int}")]
    [Authorize(Policy = Policies.CanSeeAllEmployees)]
    [ProducesResponseType(typeof(PeriodSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PeriodSummaryResponse>> GetPeriodSummary(
        int year, int month,
        [FromQuery] int? page,
        [FromQuery] int? size,
        [FromQuery] string? sort,
        CancellationToken ct)
        => Ok(await _commissionService.GetPeriodSummaryAsync(year, month, page, size, sort, ct));

    /// <summary>
    /// Donemi hesaplar ve sonuclari adimlariyla kaydeder.
    /// Okuma uclari veri yazmaz; kalicilastirma bu acik islemle yapilir.
    /// </summary>
    [HttpPost("{year:int}/{month:int}/calculate")]
    [Authorize(Policy = Policies.CanSeeAllEmployees)]
    [ProducesResponseType(typeof(PeriodSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PeriodSummaryResponse>> RunPeriod(
        int year, int month, CancellationToken ct)
        => Ok(await _commissionService.RunPeriodAsync(year, month, ct));

    /// <summary>
    /// ERP mutabakat raporu: operasyonel ciroyu (PMS+POS) muhasebelesmis ciroyla (ERP)
    /// urun grubu bazinda karsilastirir.
    /// </summary>
    [HttpGet("{year:int}/{month:int}/reconciliation")]
    [Authorize(Policy = Policies.CanSeeAllEmployees)]
    [ProducesResponseType(typeof(ReconciliationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReconciliationResponse>> GetReconciliation(
        int year, int month, CancellationToken ct)
        => Ok(await _reconciliationService.GetAsync(year, month, ct));
}
