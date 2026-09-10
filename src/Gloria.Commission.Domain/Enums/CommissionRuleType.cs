namespace Gloria.Commission.Domain.Enums;

/// <summary>
/// Kural motorunun desteklediği hesaplama tipleri.
/// Yeni bir tip eklemek kod değişikliği gerektirir; yeni bir <b>kural</b> eklemek gerektirmez.
/// </summary>
public enum CommissionRuleType
{
    /// <summary>Sabit yüzde: satış tutarının %X'i.</summary>
    Percentage = 1,

    /// <summary>Kademeli barem: aylık toplam ciro hangi kademeye düşüyorsa o oran uygulanır.</summary>
    Tiered = 2,

    /// <summary>Sabit tutar: işlem (veya adet) başına sabit TL.</summary>
    FixedAmount = 3
}
