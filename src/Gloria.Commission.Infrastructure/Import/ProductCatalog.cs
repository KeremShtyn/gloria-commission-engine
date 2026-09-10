namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Kaynak sistemlerin ayri ayri kullandigi urun kodlarini ortak bir gruba baglar.
/// Kural yazarken tekil urun kodu yerine grup kullanilabilsin diye vardir
/// (orn. "tum SPA satislarina %6" kurali tek satirla tanimlanir).
/// </summary>
public static class ProductCatalog
{
    public const string Unknown = "DIGER";

    /// <summary>PMS urun kodu on eki -> grup.</summary>
    private static readonly Dictionary<string, string> PmsPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SPA_"] = "SPA",
        ["ALC_"] = "ALC",
        ["BUG_"] = "BUGGY",
        ["PAV_"] = "PAVILLON",
        ["GLF_"] = "GOLF"
    };

    /// <summary>POS outlet kodu -> grup. POS satislari ek urun satisidir, ayri gruplanir.</summary>
    private static readonly Dictionary<string, string> PosOutlets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SPA01"] = "SPA_RETAIL",
        ["ALC01"] = "ALC_RETAIL",
        ["ALC02"] = "ALC_RETAIL",
        ["BAR01"] = "BAR_RETAIL"
    };

    /// <summary>ERP muhasebe hesabi (ObjectAccount) -> grup.</summary>
    private static readonly Dictionary<int, string> ErpAccounts = new()
    {
        [41100] = "SPA",
        [41200] = "ALC",
        [41300] = "PAVILLON",
        [41400] = "BUGGY"
    };

    /// <summary>ERP'de urun kodu kolonu yok; aciklama PMS urun adiyla birebir esleser.</summary>
    private static readonly Dictionary<string, string> ErpDescriptionToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SPA Masaj 60dk"] = "SPA_MSJ60",
        ["SPA Masaj 90dk"] = "SPA_MSJ90",
        ["Hamam Paketi"] = "SPA_HMM",
        ["A la Carte Fish"] = "ALC_FSH",
        ["A la Carte Italian"] = "ALC_ITL",
        ["A la Carte Turkish"] = "ALC_TRK",
        ["Buggy 4 Saat"] = "BUG_4H",
        ["Buggy Gunluk"] = "BUG_DAY",
        ["Buggy Günlük"] = "BUG_DAY",
        ["Pavillon Standart"] = "PAV_STD",
        ["Pavillon VIP"] = "PAV_VIP",
        ["Golf Dersi"] = "GLF_LSN"
    };

    public static string GroupFromPmsCode(string productCode)
    {
        foreach (var (prefix, group) in PmsPrefixes)
            if (productCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return group;

        return Unknown;
    }

    public static string GroupFromPosOutlet(string outletCode)
        => PosOutlets.TryGetValue(outletCode, out var group) ? group : Unknown;

    public static string GroupFromErpAccount(int objectAccount)
        => ErpAccounts.TryGetValue(objectAccount, out var group) ? group : Unknown;

    public static string CodeFromErpDescription(string description)
        => ErpDescriptionToCode.TryGetValue(description.Trim(), out var code)
            ? code
            : $"ERP_{description.Trim().Replace(' ', '_').ToUpperInvariant()}";
}
