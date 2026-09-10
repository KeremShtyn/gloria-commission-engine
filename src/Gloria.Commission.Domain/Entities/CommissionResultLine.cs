namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Hesaplamanın tek bir adımı: hangi satış, hangi kural, hangi oran, ne kadar prim.
/// İzlenebilirliğin taşıyıcısı — hesap bu satırlardan yeniden üretilebilir.
/// </summary>
public class CommissionResultLine
{
    public long Id { get; set; }

    public long CommissionResultId { get; set; }
    public CommissionResult CommissionResult { get; set; } = null!;

    /// <summary>Kademeli baremde satır bazında değil grup bazında hesap yapıldığı için null olabilir.</summary>
    public long? SaleRecordId { get; set; }
    public SaleRecord? SaleRecord { get; set; }

    public int CommissionRuleId { get; set; }
    public CommissionRule CommissionRule { get; set; } = null!;

    public string RuleCode { get; set; } = null!;
    public string RuleType { get; set; } = null!;

    /// <summary>Kuralın uygulandığı taban tutar.</summary>
    public decimal BaseAmount { get; set; }

    /// <summary>Uygulanan oran (Percentage/Tiered) — FixedAmount'ta null.</summary>
    public decimal? AppliedRate { get; set; }

    /// <summary>Uygulanan sabit tutar (FixedAmount) — diğerlerinde null.</summary>
    public decimal? AppliedFixedAmount { get; set; }

    public int Quantity { get; set; }

    public decimal CommissionAmount { get; set; }

    /// <summary>İnsan tarafından okunabilir açıklama: "SPA cirosu 45.000 TRY, 2. kademe (%7)".</summary>
    public string Explanation { get; set; } = null!;

    public int StepOrder { get; set; }
}
