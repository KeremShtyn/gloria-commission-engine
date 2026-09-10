namespace Gloria.Commission.Domain.Entities;

/// <summary>Kademeli barem satırı. Aralık [MinAmount, MaxAmount) şeklindedir.</summary>
public class CommissionRuleTier
{
    public int Id { get; set; }

    public int CommissionRuleId { get; set; }
    public CommissionRule CommissionRule { get; set; } = null!;

    /// <summary>Kademenin başladığı aylık toplam ciro (dahil).</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Kademenin bittiği tutar (hariç). Null ise üst sınır yoktur.</summary>
    public decimal? MaxAmount { get; set; }

    public decimal Rate { get; set; }

    public bool Contains(decimal amount) =>
        amount >= MinAmount && (MaxAmount is null || amount < MaxAmount);
}
