namespace Gloria.Commission.Application.Dtos.Responses;

public sealed record CommissionRuleResponse
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public string? SourceSystem { get; init; }
    public string? DepartmentCode { get; init; }
    public string? ProductGroup { get; init; }
    public string? ProductCode { get; init; }
    public string? Hotel { get; init; }
    public decimal? Rate { get; init; }
    public decimal? FixedAmount { get; init; }
    public bool MultiplyByQuantity { get; init; }
    public string TierApplication { get; init; } = string.Empty;
    public int Priority { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<CommissionRuleTierResponse> Tiers { get; init; } = [];
}

public sealed record CommissionRuleTierResponse
{
    public int Id { get; init; }
    public decimal MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public decimal Rate { get; init; }
}
