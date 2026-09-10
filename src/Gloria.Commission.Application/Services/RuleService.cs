using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Mappers;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

/// <summary>
/// Prim kurallarinin yonetimi. Kural tanimi tamamen veridir; yeni kalem eklemek
/// veya oran degistirmek icin uygulama yeniden derlenmez.
/// </summary>
public sealed class RuleService : IRuleService
{
    private readonly ICommissionRuleRepository _rules;
    private readonly IDepartmentRepository _departments;
    private readonly IHotelRepository _hotels;
    private readonly IProductGroupRepository _productGroups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RuleService(
        ICommissionRuleRepository rules,
        IDepartmentRepository departments,
        IHotelRepository hotels,
        IProductGroupRepository productGroups,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _rules = rules;
        _departments = departments;
        _hotels = hotels;
        _productGroups = productGroups;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CommissionRuleResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var rules = await _rules.FindAllAsync(ct);
        return rules.Select(CommissionRuleMapper.ToResponse).ToList();
    }

    public async Task<CommissionRuleResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
        => CommissionRuleMapper.ToResponse(await RequireAsync(id, ct));

    public async Task<CommissionRuleResponse> CreateAsync(
        CommissionRuleRequest request, CancellationToken ct = default)
    {
        RequireAdmin();
        Validate(request);
        await ValidateReferencesAsync(request, ct);

        if (await _rules.ExistsByCodeAsync(request.Code, excludeId: null, ct))
            throw new DomainException("RULE_CODE_EXISTS", $"'{request.Code}' kodlu kural zaten var.");

        var rule = new CommissionRule();
        CommissionRuleMapper.Apply(rule, request);

        _rules.Add(rule);
        await _unitOfWork.SaveChangesAsync(ct);

        return CommissionRuleMapper.ToResponse(rule);
    }

    public async Task<CommissionRuleResponse> UpdateAsync(
        Guid id, CommissionRuleRequest request, CancellationToken ct = default)
    {
        RequireAdmin();
        Validate(request);
        await ValidateReferencesAsync(request, ct);

        var rule = await RequireAsync(id, ct);

        if (await _rules.ExistsByCodeAsync(request.Code, excludeId: id, ct))
            throw new DomainException("RULE_CODE_EXISTS", $"'{request.Code}' kodlu baska bir kural var.");

        _rules.RemoveTiers(rule.Tiers);
        CommissionRuleMapper.Apply(rule, request);
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        return CommissionRuleMapper.ToResponse(rule);
    }

    /// <summary>
    /// Kural silinmez, pasife alinir. Gecmis donemlerin hesap adimlari kurala referans verdigi
    /// icin fiziksel silme izlenebilirligi bozardi.
    /// </summary>
    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        RequireAdmin();

        var rule = await RequireAsync(id, ct);
        rule.IsActive = false;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private void RequireAdmin()
    {
        if (!_currentUser.IsAdmin)
            throw new DomainException("FORBIDDEN", "Kural yonetimi icin Admin rolu gerekir.");
    }

    private async Task<CommissionRule> RequireAsync(Guid id, CancellationToken ct)
        => await _rules.FindByIdAsync(id, ct)
           ?? throw new DomainException("RULE_NOT_FOUND", $"{id} numarali kural yok.");

    /// <summary>
    /// Kapsam olarak verilen kimlikler gercekten var mi?
    /// Veritabani yabanci anahtari zaten engelliyor ama oradan gelen hata istemciye
    /// "beklenmeyen hata" olarak yansir; burada anlasilir bir mesaja cevriliyor.
    /// </summary>
    private async Task ValidateReferencesAsync(CommissionRuleRequest request, CancellationToken ct)
    {
        if (request.DepartmentId is { } departmentId)
        {
            var departments = await _departments.FindAllAsync(ct);
            if (departments.All(d => d.Id != departmentId))
                throw new DomainException("DEPARTMENT_NOT_FOUND", "Secilen departman bulunamadi.");
        }

        if (request.HotelId is { } hotelId)
        {
            var hotels = await _hotels.FindAllAsync(ct);
            if (hotels.All(h => h.Id != hotelId))
                throw new DomainException("HOTEL_NOT_FOUND", "Secilen otel bulunamadi.");
        }

        if (request.ProductGroupId is { } groupId)
        {
            var groups = await _productGroups.FindAllAsync(ct);
            if (groups.All(g => g.Id != groupId))
                throw new DomainException("PRODUCT_GROUP_NOT_FOUND", "Secilen urun grubu bulunamadi.");
        }
    }

    /// <summary>
    /// Is kurali dogrulamasi. Bicimsel kontroller (zorunluluk, aralik) DTO attribute'larinda;
    /// burada tipe bagli tutarlilik denetleniyor.
    /// </summary>
    private static void Validate(CommissionRuleRequest request)
    {
        switch (request.RuleType)
        {
            case CommissionRuleType.Percentage when request.Rate is null or <= 0:
                throw new DomainException("RULE_RATE_REQUIRED", "Sabit yuzde kuralinda oran zorunludur.");

            case CommissionRuleType.FixedAmount when request.FixedAmount is null or <= 0:
                throw new DomainException("RULE_FIXED_AMOUNT_REQUIRED", "Sabit tutar kuralinda tutar zorunludur.");

            case CommissionRuleType.Tiered when request.Tiers.Count == 0:
                throw new DomainException("RULE_TIERS_REQUIRED", "Kademeli barem kuralinda en az bir kademe olmali.");
        }

        if (request.EffectiveTo is { } to && to < request.EffectiveFrom)
            throw new DomainException("RULE_INVALID_PERIOD", "Bitis tarihi baslangictan once olamaz.");

        if (request.RuleType != CommissionRuleType.Tiered) return;

        var ordered = request.Tiers.OrderBy(t => t.MinAmount).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var tier = ordered[i];

            if (tier.MaxAmount is { } max && max <= tier.MinAmount)
                throw new DomainException("RULE_TIER_INVALID",
                    $"Kademe ust siniri alt sinirdan buyuk olmali: {tier.MinAmount} - {max}");

            // Kademeler arasinda bosluk kalirsa bazi cirolar hicbir kademeye dusmez.
            if (i > 0 && ordered[i - 1].MaxAmount is { } previousMax && previousMax != tier.MinAmount)
                throw new DomainException("RULE_TIER_GAP",
                    $"Kademeler arasinda bosluk veya cakisma var: {previousMax} -> {tier.MinAmount}");
        }
    }
}
