using System.ComponentModel.DataAnnotations;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Models;

/// <summary>Kural tanimlama/duzenleme ekraninin gonderdigi istek.</summary>
public sealed record CommissionRuleRequest
{
    [Required, MaxLength(50)]
    public string Code { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public CommissionRuleType RuleType { get; init; }

    public SourceSystem? SourceSystem { get; init; }

    [MaxLength(50)] public string? DepartmentCode { get; init; }
    [MaxLength(50)] public string? ProductGroup { get; init; }
    [MaxLength(50)] public string? ProductCode { get; init; }
    [MaxLength(10)] public string? Hotel { get; init; }

    [Range(0, 1)]
    public decimal? Rate { get; init; }

    [Range(0, 1_000_000)]
    public decimal? FixedAmount { get; init; }

    public bool MultiplyByQuantity { get; init; }

    public TierApplication TierApplication { get; init; } = TierApplication.WholeAmount;

    public List<CommissionRuleTierRequest> Tiers { get; init; } = [];

    public int Priority { get; init; }

    [Required]
    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CommissionRuleTierRequest
{
    [Range(0, double.MaxValue)]
    public decimal MinAmount { get; init; }

    public decimal? MaxAmount { get; init; }

    [Range(0, 1)]
    public decimal Rate { get; init; }
}

public sealed record CommissionRuleDto
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
    public IReadOnlyList<CommissionRuleTierDto> Tiers { get; init; } = Array.Empty<CommissionRuleTierDto>();
}

public sealed record CommissionRuleTierDto
{
    public int Id { get; init; }
    public decimal MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public decimal Rate { get; init; }
}

public sealed record AuditLogDto
{
    public long Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public string ChangedByRole { get; init; } = string.Empty;
    public DateTime ChangedAtUtc { get; init; }
}
