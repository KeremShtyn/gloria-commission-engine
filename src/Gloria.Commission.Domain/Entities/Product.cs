namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Kaynak sistemlerdeki ürün kodlarının ortak kataloğu.
/// Kural eşleştirmesi tekil ürün koduna da ürün grubuna da yapılabilsin diye ayrı tablo.
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>Kaynak sistemdeki kod: SPA_MSJ60, PLU 5001, vb.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Kural yazımını kolaylaştıran üst grup: SPA, ALC, BUGGY, PAVILLON, GOLF, POS_RETAIL.</summary>
    public string ProductGroup { get; set; } = null!;
}
