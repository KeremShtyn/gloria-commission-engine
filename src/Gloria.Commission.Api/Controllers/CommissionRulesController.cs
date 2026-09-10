using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Services;
using Gloria.Commission.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gloria.Commission.Api.Controllers;

/// <summary>
/// Prim kurallarinin yonetimi.
/// Controller yalnizca HTTP isini yapar: baglama, dogrulama, durum kodu.
/// Is mantigi <see cref="IRuleService"/> icinde.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/commission-rules")]
[Produces("application/json")]
[Tags("Prim Kurallari")]
public class CommissionRulesController : ControllerBase
{
    private readonly IRuleService _ruleService;

    public CommissionRulesController(IRuleService ruleService) => _ruleService = ruleService;

    /// <summary>Tum kurallari onceligine gore listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CommissionRuleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommissionRuleResponse>>> GetAll(CancellationToken ct)
        => Ok(await _ruleService.GetAllAsync(ct));

    /// <summary>Tek bir kuralin detayi.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CommissionRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionRuleResponse>> GetById(Guid id, CancellationToken ct)
        => Ok(await _ruleService.GetByIdAsync(id, ct));

    /// <summary>Yeni kural olusturur. Admin rolu gerekir.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(CommissionRuleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CommissionRuleResponse>> Create(
        [FromBody] CommissionRuleRequest request, CancellationToken ct)
    {
        var created = await _ruleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Kurali gunceller. Admin rolu gerekir.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(CommissionRuleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommissionRuleResponse>> Update(
        Guid id, [FromBody] CommissionRuleRequest request, CancellationToken ct)
        => Ok(await _ruleService.UpdateAsync(id, request, ct));

    /// <summary>
    /// Kurali pasife alir. Fiziksel silme yapilmaz —
    /// gecmis hesap adimlari kurala referans veriyor.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _ruleService.DeactivateAsync(id, ct);
        return NoContent();
    }
}
