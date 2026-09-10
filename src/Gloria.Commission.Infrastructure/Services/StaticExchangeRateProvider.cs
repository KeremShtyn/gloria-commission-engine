using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Infrastructure.Services;

/// <summary>
/// Case kapsaminda sabit kur tablosu. Gercek sistemde bu arayuzun arkasina
/// TCMB gunluk kuru ve tarihsel kur tablosu konur; cagiran kod degismez.
///
/// Kur bulunamayan bir para birimi sessizce 1 kabul edilmez — tutar
/// oldugundan buyuk/kucuk prim uretmesin diye hata firlatilir.
/// </summary>
public sealed class StaticExchangeRateProvider : IExchangeRateProvider
{
    private static readonly Dictionary<string, decimal> Rates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TRY"] = 1m,
        ["EUR"] = 47.50m,
        ["USD"] = 41.20m,
        ["GBP"] = 55.30m
    };

    public decimal GetRateToTry(string currency, DateOnly date)
        => TryGetRateToTry(currency, date, out var rate)
            ? rate
            : throw new DomainException("EXCHANGE_RATE_MISSING",
                $"'{currency}' para birimi icin kur tanimli degil.");

    public bool TryGetRateToTry(string currency, DateOnly date, out decimal rate)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            rate = 1m;
            return true;
        }

        return Rates.TryGetValue(currency.Trim(), out rate);
    }
}
