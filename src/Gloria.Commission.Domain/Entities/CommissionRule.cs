using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Prim kuralı. Tamamen veritabanında tanımlıdır; yeni kalem veya oran değişikliği
/// kod değişikliği gerektirmez.
///
/// Eşleştirme (scope) alanları null bırakılırsa "hepsi" anlamına gelir.
/// Örn. ProductGroup="SPA", diğerleri null => tüm SPA satışları.
/// </summary>
public class CommissionRule
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public CommissionRuleType RuleType { get; set; }

    // ---- Eşleştirme (scope) ----
    public SourceSystem? SourceSystem { get; set; }
    public string? DepartmentCode { get; set; }
    public string? ProductGroup { get; set; }
    public string? ProductCode { get; set; }
    public string? Hotel { get; set; }

    // ---- Hesaplama parametreleri ----

    /// <summary>Percentage tipi için oran (0.05 = %5).</summary>
    public decimal? Rate { get; set; }

    /// <summary>FixedAmount tipi için işlem başına TL.</summary>
    public decimal? FixedAmount { get; set; }

    /// <summary>FixedAmount tipinde tutarın adetle çarpılıp çarpılmayacağı.</summary>
    public bool MultiplyByQuantity { get; set; }

    /// <summary>Tiered tipi için oranın uygulanma şekli.</summary>
    public TierApplication TierApplication { get; set; } = TierApplication.WholeAmount;

    public ICollection<CommissionRuleTier> Tiers { get; set; } = new List<CommissionRuleTier>();

    // ---- Geçerlilik ----

    /// <summary>Aynı satışa birden fazla kural uyarsa yüksek öncelikli olan uygulanır.</summary>
    public int Priority { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Kural belirtilen tarihte yürürlükte mi?</summary>
    public bool IsEffectiveOn(DateOnly date) =>
        IsActive && date >= EffectiveFrom && (EffectiveTo is null || date <= EffectiveTo);

    /// <summary>
    /// Scope alanlarından kaç tanesi doluysa kural o kadar "spesifik"tir.
    /// Öncelik eşitse daha spesifik kural kazanır.
    /// </summary>
    public int Specificity =>
        (SourceSystem is null ? 0 : 1) +
        (string.IsNullOrEmpty(DepartmentCode) ? 0 : 1) +
        (string.IsNullOrEmpty(ProductGroup) ? 0 : 1) +
        (string.IsNullOrEmpty(ProductCode) ? 0 : 1) +
        (string.IsNullOrEmpty(Hotel) ? 0 : 1);
}
