using System.Security.Cryptography;
using System.Text;

namespace Gloria.Commission.Application.Import;

/// <summary>Tekillik anahtarlarinin uretimi. Ayni dosya iki kez yuklendiginde fark edilsin diye.</summary>
public static class ContentHash
{
    public static string Of(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    /// <summary>
    /// Satir tekillik anahtari. Kaynak sistemdeki belge numarasi dogal anahtardir;
    /// ayni belge ikinci kez gelirse yazilmaz.
    /// </summary>
    public static string ForDocument(string sourceSystem, string documentNo)
        => Of($"{sourceSystem}|{documentNo}");
}
