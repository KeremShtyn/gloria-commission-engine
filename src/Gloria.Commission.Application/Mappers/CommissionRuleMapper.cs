using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Mappers;

/// <summary>
/// Kural entity'si ile DTO'lar arasindaki cevrim.
/// Elle yazildi: kutuphane bagimliligi getirmeden hangi alanin nereye gittigi okunur kaliyor.
/// </summary>
public static class CommissionRuleMapper
{
    public static CommissionRuleResponse ToResponse(CommissionRule rule) => new()
    {
        Id = rule.Id,
        Code = rule.Code,
        Name = rule.Name,
        RuleType = rule.RuleType.ToString(),
        SourceSystem = rule.SourceSystem?.ToString(),
        DepartmentCode = rule.DepartmentCode,
        ProductGroup = rule.ProductGroup,
        ProductCode = rule.ProductCode,
        Hotel = rule.Hotel,
        Rate = rule.Rate,
        FixedAmount = rule.FixedAmount,
        MultiplyByQuantity = rule.MultiplyByQuantity,
        TierApplication = rule.TierApplication.ToString(),
        Priority = rule.Priority,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo,
        IsActive = rule.IsActive,
        Tiers = rule.Tiers
            .OrderBy(t => t.MinAmount)
            .Select(t => new CommissionRuleTierResponse
            {
                Id = t.Id,
                MinAmount = t.MinAmount,
                MaxAmount = t.MaxAmount,
                Rate = t.Rate
            })
            .ToList()
    };

    /// <summary>Istegi mevcut entity uzerine uygular. Yeni kayitta bos bir entity ile cagrilir.</summary>
    public static void Apply(CommissionRule rule, CommissionRuleRequest request)
    {
        rule.Code = request.Code.Trim();
        rule.Name = request.Name.Trim();
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
}
