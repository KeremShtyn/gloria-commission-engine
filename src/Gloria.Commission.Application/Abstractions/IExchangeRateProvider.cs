namespace Gloria.Commission.Application.Abstractions;

/// <summary>
/// Yabancı para satışların TRY karşılığı. Case kapsamında sabit kur tablosu kullanılır;
/// gerçek sistemde TCMB günlük kuru bu arayüzün arkasına konur.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>Belirtilen tarihteki 1 birim <paramref name="currency"/> = ? TRY.</summary>
    decimal GetRateToTry(string currency, DateOnly date);

    /// <summary>
    /// Kur bulunamazsa istisna firlatmadan false doner.
    /// İçe aktarma sirasinda tek bir satirin tum dosyayi devirmemesi icin kullanilir.
    /// </summary>
    bool TryGetRateToTry(string currency, DateOnly date, out decimal rate);
}
