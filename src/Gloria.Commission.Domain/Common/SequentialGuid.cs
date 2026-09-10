using System.Security.Cryptography;

namespace Gloria.Commission.Domain.Common;

/// <summary>
/// Zaman damgasi onekli kimlik uretir (UUIDv7 duzeni).
///
/// Neden rastgele <c>Guid.NewGuid()</c> degil: birincil anahtar indekste sirali tutuldugu
/// icin rastgele deger her eklemede indeksin ortasina yazar ve sayfa bolunmesine yol acar.
/// Zaman onekiyle uretilen deger sona eklenir, indeks yerelligi korunur.
///
/// Tahmin edilebilirlik sorunu dogurmaz: ilk 48 bit zaman, kalan 74 bit kriptografik
/// rastgeledir — bir kimligi bilmek bir sonrakini bilmeyi saglamaz.
///
/// Duzen (RFC 9562 / UUIDv7):
///   0-5   : Unix zamani, milisaniye, big-endian
///   6     : surum (7) + rastgele
///   8     : varyant + rastgele
///   7,9-15: rastgele
/// </summary>
public static class SequentialGuid
{
    public static Guid New()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 48 bitlik zaman damgasi en anlamli bayttan basa yazilir.
        bytes[0] = (byte)(timestamp >> 40);
        bytes[1] = (byte)(timestamp >> 32);
        bytes[2] = (byte)(timestamp >> 24);
        bytes[3] = (byte)(timestamp >> 16);
        bytes[4] = (byte)(timestamp >> 8);
        bytes[5] = (byte)timestamp;

        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);   // surum 7
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);   // varyant 10xx

        // bigEndian: true olmadan .NET ilk uc grubu ters yazar ve siralama bozulur.
        return new Guid(bytes, bigEndian: true);
    }
}
