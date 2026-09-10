namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Zamanlanmis aktarimin ayarlari. Kaynak sistemler ekstrelerini
/// <see cref="InboxPath"/> altina birakir; servis bulup isler.
/// </summary>
public sealed class ImportWatcherOptions
{
    public const string SectionName = "ImportWatcher";

    /// <summary>Varsayilan olarak kapali: case demosu sirasinda beklenmedik aktarim olmasin.</summary>
    public bool Enabled { get; set; }

    /// <summary>Kaynak sistemlerin dosya biraktigi klasor.</summary>
    public string InboxPath { get; set; } = "data/inbox";

    /// <summary>Basariyla islenen dosyalarin tasindigi klasor.</summary>
    public string ProcessedPath { get; set; } = "data/processed";

    /// <summary>Islenemeyen dosyalarin tasindigi klasor.</summary>
    public string FailedPath { get; set; } = "data/failed";

    /// <summary>
    /// Tarama araligi. Uretimde gecelik calisir (orn. 24 saat);
    /// gelistirmede kisa tutulup davranis gozlenebilir.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Ilk taramadan once beklenecek sure. Uygulama acilisini bloklamamak icin.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(15);
}
