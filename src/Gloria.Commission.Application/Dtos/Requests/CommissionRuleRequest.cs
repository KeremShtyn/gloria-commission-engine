using System.ComponentModel.DataAnnotations;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Dtos.Requests;

/// <summary>
/// Kural tanimlama/duzenleme ekranindan gelen istek.
/// Bicimsel dogrulama attribute'larla, is kurali dogrulamasi servis katmaninda yapilir.
/// </summary>
public sealed record CommissionRuleRequest
{
    [Required(ErrorMessage = "Kural kodu zorunludur.")]
    [MaxLength(50)]
    public string Code { get; init; } = string.Empty;

    [Required(ErrorMessage = "Kural adi zorunludur.")]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(CommissionRuleType), ErrorMessage = "Gecersiz kural tipi.")]
    public CommissionRuleType RuleType { get; init; }

    public SourceSystem? SourceSystem { get; init; }

    [MaxLength(50)] public string? DepartmentCode { get; init; }
    [MaxLength(50)] public string? ProductGroup { get; init; }
    [MaxLength(50)] public string? ProductCode { get; init; }
    [MaxLength(10)] public string? Hotel { get; init; }

    [Range(0, 1, ErrorMessage = "Oran 0 ile 1 arasinda olmalidir (0,05 = %5).")]
    public decimal? Rate { get; init; }

    [Range(0, 1_000_000, ErrorMessage = "Sabit tutar 0 ile 1.000.000 arasinda olmalidir.")]
    public decimal? FixedAmount { get; init; }

    public bool MultiplyByQuantity { get; init; }

    public TierApplication TierApplication { get; init; } = TierApplication.WholeAmount;

    [MaxLength(20, ErrorMessage = "En fazla 20 kademe tanimlanabilir.")]
    public List<CommissionRuleTierRequest> Tiers { get; init; } = [];

    [Range(0, 1000)]
    public int Priority { get; init; }

    [Required]
    public DateOnly EffectiveFrom { get; init; }

    public DateOnly? EffectiveTo { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed record CommissionRuleTierRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Kademe alt siniri negatif olamaz.")]
    public decimal MinAmount { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "Kademe ust siniri negatif olamaz.")]
    public decimal? MaxAmount { get; init; }

    [Range(0, 1, ErrorMessage = "Kademe orani 0 ile 1 arasinda olmalidir.")]
    public decimal Rate { get; init; }
}
