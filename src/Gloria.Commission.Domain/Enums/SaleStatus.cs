namespace Gloria.Commission.Domain.Enums;

/// <summary>Satış kaydının prim hesabına girmeye uygunluğu.</summary>
public enum SaleStatus
{
    /// <summary>Normal satış, prime esas.</summary>
    Normal = 1,

    /// <summary>İade / ters kayıt. Tutarı negatiftir, prim tabanından düşülür.</summary>
    Refund = 2,

    /// <summary>Bir iade tarafından iptal edilmiş orijinal satış.</summary>
    Reversed = 3,

    /// <summary>Kaynak sistemde henüz muhasebeleşmemiş (ERP UNPOSTED). Prime esas değildir.</summary>
    Unposted = 4
}
