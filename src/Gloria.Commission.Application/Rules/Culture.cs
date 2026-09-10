using System.Globalization;

namespace Gloria.Commission.Application.Rules;

internal static class Culture
{
    /// <summary>Açıklama metinleri Türkçe biçimde üretilir (1.234,56).</summary>
    public static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
}
