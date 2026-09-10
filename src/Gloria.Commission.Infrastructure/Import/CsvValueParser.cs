using System.Globalization;

namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Ham CSV hucrelerini tipli degerlere cevirir.
///
/// Tasarim karari: <b>tarihlerde katı, tutarlarda toleransli</b> davranilir.
/// Tarih formati belirsizse (32/08/2026, 08.15.2026) satir reddedilir; yanlis tahmin
/// primi yanlis doneme yazar. Tutar formati ise belirsiz degildir (2.500,00 ile 2500.00
/// ayni sayidir) — bu satirlari reddetmek gercek ciroyu kaybettirir.
/// </summary>
public static class CsvValueParser
{
    /// <summary>Kabul edilen tek tarih formati. Digerleri hatali satir olarak loglanir.</summary>
    private const string IsoDate = "yyyy-MM-dd";

    public static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        return DateOnly.TryParseExact(
            value.Trim(), IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Hem "2500.00" hem "2.500,00" formatini okur.
    /// Son ayrac karakterinden sonra 2 hane varsa ondalik, 3 hane varsa binlik ayracidir.
    /// </summary>
    public static bool TryParseDecimal(string? value, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var raw = value.Trim();
        var lastDot = raw.LastIndexOf('.');
        var lastComma = raw.LastIndexOf(',');
        var lastSeparator = Math.Max(lastDot, lastComma);

        string normalized;

        if (lastSeparator < 0)
        {
            normalized = raw;
        }
        else
        {
            var fractionLength = raw.Length - lastSeparator - 1;
            var withoutSeparators = raw.Replace(".", string.Empty).Replace(",", string.Empty);

            normalized = fractionLength == 3
                // Son ayrac binlik: 1.234 -> 1234
                ? withoutSeparators
                // Son ayrac ondalik: 2.500,00 -> 2500.00
                : string.Concat(
                    withoutSeparators.AsSpan(0, withoutSeparators.Length - fractionLength),
                    ".",
                    withoutSeparators.AsSpan(withoutSeparators.Length - fractionLength));
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    public static bool TryParseInt(string? value, out int number)
    {
        number = 0;
        return !string.IsNullOrWhiteSpace(value)
               && int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    public static string? Trimmed(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
