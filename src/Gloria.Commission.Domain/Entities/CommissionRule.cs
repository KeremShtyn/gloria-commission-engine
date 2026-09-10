using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Prim kurali. Tamamen veritabaninda tanimlidir; yeni kalem veya oran degisikligi
/// kod degisikligi gerektirmez.
///
/// Eslestirme (scope) alanlari null birakilirsa "hepsi" anlamina gelir.
/// Orn. ProductGroupId = SPA, digerleri null => tum SPA satislari.
///
/// Departman, otel ve urun grubu yabanci anahtardir: serbest metin olsalardi
/// bir yazim hatasi kuralin hicbir satisla eslesmemesine ve sessizce sifir prim
/// uretmesine yol acardi.
/// </summary>
public class CommissionRule
{
    public Guid Id { get; set; } = SequentialGuid.New();

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;

    public CommissionRuleType RuleType { get; set; }

    // ---- Eslestirme (scope) ----
    public SourceSystem? SourceSystem { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? ProductGroupId { get; set; }
    public ProductGroup? ProductGroup { get; set; }

    public Guid? HotelId { get; set; }
    public Hotel? Hotel { get; set; }

    /// <summary>Tekil urun kodu. Kaynak sistemler farkli sekillendirdigi icin serbest metin.</summary>
    public string? ProductCode { get; set; }

    // ---- Hesaplama parametreleri ----

    /// <summary>Percentage tipi icin oran (0.05 = %5).</summary>
    public decimal? Rate { get; set; }

    /// <summary>FixedAmount tipi icin islem basina TL.</summary>
    public decimal? FixedAmount { get; set; }

    /// <summary>FixedAmount tipinde tutarin adetle carpilip carpilmayacagi.</summary>
    public bool MultiplyByQuantity { get; set; }

    /// <summary>Tiered tipi icin oranin uygulanma sekli.</summary>
    public TierApplication TierApplication { get; set; } = TierApplication.WholeAmount;

    public ICollection<CommissionRuleTier> Tiers { get; set; } = new List<CommissionRuleTier>();

    // ---- Gecerlilik ----

    /// <summary>Ayni satisa birden fazla kural uyarsa yuksek oncelikli olan uygulanir.</summary>
    public int Priority { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>Kural belirtilen tarihte yururlukte mi?</summary>
    public bool IsEffectiveOn(DateOnly date) =>
        IsActive && date >= EffectiveFrom && (EffectiveTo is null || date <= EffectiveTo);

    /// <summary>
    /// Scope alanlarindan kac tanesi doluysa kural o kadar "spesifik"tir.
    /// Oncelik esitse daha spesifik kural kazanir.
    /// </summary>
    public int Specificity =>
        (SourceSystem is null ? 0 : 1) +
        (DepartmentId is null ? 0 : 1) +
        (ProductGroupId is null ? 0 : 1) +
        (HotelId is null ? 0 : 1) +
        (string.IsNullOrEmpty(ProductCode) ? 0 : 1);
}
