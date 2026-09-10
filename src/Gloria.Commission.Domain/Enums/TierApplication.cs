namespace Gloria.Commission.Domain.Enums;

/// <summary>Kademeli baremde oranın nasıl uygulanacağı.</summary>
public enum TierApplication
{
    /// <summary>Hedef aşılırsa yüksek oran cironun tamamına uygulanır.</summary>
    WholeAmount = 1,

    /// <summary>Her kademe yalnızca kendi aralığına düşen tutara uygulanır (dilimli).</summary>
    Marginal = 2
}
