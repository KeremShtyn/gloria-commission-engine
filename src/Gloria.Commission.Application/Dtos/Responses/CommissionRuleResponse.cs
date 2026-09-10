namespace Gloria.Commission.Application.Dtos.Responses;

public sealed record CommissionRuleResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public string? SourceSystem { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ProductGroupId { get; init; }
    public Guid? HotelId { get; init; }
    public string? ProductCode { get; init; }

    // Arayuzun ayrica sorgu atmamasi icin kodlar da doner.
    public string? DepartmentCode { get; init; }
    public string? ProductGroupCode { get; init; }
    public string? HotelCode { get; init; }
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
    public Guid Id { get; init; }
    public decimal MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public decimal Rate { get; init; }
}
