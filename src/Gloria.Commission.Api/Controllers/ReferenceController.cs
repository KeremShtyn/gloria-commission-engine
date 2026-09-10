using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

/// <summary>Arayuzun acilis listeleri.</summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Referans Veri")]
public class ReferenceController : ControllerBase
{
    private readonly IReferenceService _referenceService;

    public ReferenceController(IReferenceService referenceService) => _referenceService = referenceService;

    [HttpGet("employees")]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetEmployees(CancellationToken ct)
        => Ok(await _referenceService.GetEmployeesAsync(ct));

    [HttpGet("departments")]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponse>>> GetDepartments(CancellationToken ct)
        => Ok(await _referenceService.GetDepartmentsAsync(ct));

    /// <summary>Satislarda gecen urun gruplari - kural tanimlarken kullanilir.</summary>
    [HttpGet("product-groups")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetProductGroups(CancellationToken ct)
        => Ok(await _referenceService.GetProductGroupsAsync(ct));
}
