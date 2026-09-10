using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Urun grubu. Kaynak sistemlerin ayri ayri kullandigi urun kodlarini ortak bir kumeye baglar
/// (PMS urun kodu on eki, POS outlet kodu, ERP muhasebe hesabi).
///
/// Kural yazarken tekil urun kodu yerine grup kullanilabilsin diye tablo:
/// "tum SPA satislarina %6" kurali tek satirla tanimlanir. Tablo olmasinin sebebi
/// serbest metin yazim hatasinin sessizce sifir prim uretmesini engellemek.
/// </summary>
public class ProductGroup
{
    /// <summary>Hicbir gruba eslesmeyen satislarin dustugu kod.</summary>
    public const string UnknownCode = "DIGER";

    public Guid Id { get; set; } = SequentialGuid.New();

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<SaleRecord> Sales { get; set; } = new List<SaleRecord>();
}
