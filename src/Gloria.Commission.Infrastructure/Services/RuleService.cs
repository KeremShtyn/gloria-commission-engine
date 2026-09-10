using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Models;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Services;

public interface IRuleService
{
    Task<IReadOnlyList<CommissionRuleDto>> ListAsync(CancellationToken ct = default);
    Task<CommissionRuleDto> GetAsync(int id, CancellationToken ct = default);
    Task<CommissionRuleDto> CreateAsync(CommissionRuleRequest request, CancellationToken ct = default);
    Task<CommissionRuleDto> UpdateAsync(int id, CommissionRuleRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Prim kurallarinin CRUD'u. Kural tanimi tamamen veridir; yeni kalem eklemek
/// veya oran degistirmek icin uygulama yeniden derlenmez.
/// </summary>
public sealed class RuleService : IRuleService
{
    private readonly CommissionDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RuleService(CommissionDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CommissionRuleDto>> ListAsync(CancellationToken ct = default)
    {
        var rules = await _db.CommissionRules
            .Include(r => r.Tiers)
            .OrderByDescending(r => r.Priority).ThenBy(r => r.Code)
            .AsNoTracking()
            .ToListAsync(ct);

        return rules.Select(ToDto).ToList();
    }

    public async Task<CommissionRuleDto> GetAsync(int id, CancellationToken ct = default)
        => ToDto(await FindAsync(id, ct));

    public async Task<CommissionRuleDto> CreateAsync(
        CommissionRuleRequest request, CancellationToken ct = default)
    {
        RequireAdmin();
        Validate(request);

        if (await _db.CommissionRules.AnyAsync(r => r.Code == request.Code, ct))
            throw new DomainException("RULE_CODE_EXISTS", $"'{request.Code}' kodlu kural zaten var.");

        var rule = new CommissionRule();
        Apply(rule, request);

        _db.CommissionRules.Add(rule);
        await _db.SaveChangesAsync(ct);

        return ToDto(rule);
    }

    public async Task<CommissionRuleDto> UpdateAsync(
        int id, CommissionRuleRequest request, CancellationToken ct = default)
    {
        RequireAdmin();
        Validate(request);

        var rule = await FindAsync(id, ct);

        if (rule.Code != request.Code
            && await _db.CommissionRules.AnyAsync(r => r.Code == request.Code && r.Id != id, ct))
            throw new DomainException("RULE_CODE_EXISTS", $"'{request.Code}' kodlu baska bir kural var.");

        _db.CommissionRuleTiers.RemoveRange(rule.Tiers);
        Apply(rule, request);
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    /// <summary>
    /// Kural silinmez, pasife alinir. Gecmis donemlerin hesap adimlari kurala referans verdigi
    /// icin fiziksel silme izlenebilirligi bozardi.
    /// </summary>
    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        RequireAdmin();

        var rule = await FindAsync(id, ct);
        rule.IsActive = false;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private void RequireAdmin()
    {
        if (!_currentUser.IsAdmin)
            throw new DomainException("FORBIDDEN", "Kural yonetimi icin Admin rolu gerekir.");
    }

    private async Task<CommissionRule> FindAsync(int id, CancellationToken ct)
        => await _db.CommissionRules.Include(r => r.Tiers).FirstOrDefaultAsync(r => r.Id == id, ct)
           ?? throw new DomainException("RULE_NOT_FOUND", $"{id} numarali kural yok.");

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

            if (i > 0 && ordered[i - 1].MaxAmount is { } previousMax && previousMax != tier.MinAmount)
                throw new DomainException("RULE_TIER_GAP",
                    $"Kademeler arasinda bosluk veya cakisma var: {previousMax} -> {tier.MinAmount}");
        }
    }

    private static void Apply(CommissionRule rule, CommissionRuleRequest request)
    {
        rule.Code = request.Code;
        rule.Name = request.Name;
        rule.RuleType = request.RuleType;
        rule.SourceSystem = request.SourceSystem;
        rule.DepartmentCode = request.DepartmentCode;
        rule.ProductGroup = request.ProductGroup;
        rule.ProductCode = request.ProductCode;
        rule.Hotel = request.Hotel;
        rule.Rate = request.Rate;
        rule.FixedAmount = request.FixedAmount;
        rule.MultiplyByQuantity = request.MultiplyByQuantity;
        rule.TierApplication = request.TierApplication;
        rule.Priority = request.Priority;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.IsActive = request.IsActive;

        rule.Tiers = request.Tiers
            .OrderBy(t => t.MinAmount)
            .Select(t => new CommissionRuleTier
            {
                MinAmount = t.MinAmount,
                MaxAmount = t.MaxAmount,
                Rate = t.Rate
            })
            .ToList();
    }

    private static CommissionRuleDto ToDto(CommissionRule r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        Name = r.Name,
        RuleType = r.RuleType.ToString(),
        SourceSystem = r.SourceSystem?.ToString(),
        DepartmentCode = r.DepartmentCode,
        ProductGroup = r.ProductGroup,
        ProductCode = r.ProductCode,
        Hotel = r.Hotel,
        Rate = r.Rate,
        FixedAmount = r.FixedAmount,
        MultiplyByQuantity = r.MultiplyByQuantity,
        TierApplication = r.TierApplication.ToString(),
        Priority = r.Priority,
        EffectiveFrom = r.EffectiveFrom,
        EffectiveTo = r.EffectiveTo,
        IsActive = r.IsActive,
        Tiers = r.Tiers.OrderBy(t => t.MinAmount).Select(t => new CommissionRuleTierDto
        {
            Id = t.Id,
            MinAmount = t.MinAmount,
            MaxAmount = t.MaxAmount,
            Rate = t.Rate
        }).ToList()
    };
}
