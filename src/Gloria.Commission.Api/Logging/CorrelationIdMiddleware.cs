using Serilog.Context;

namespace Gloria.Commission.Api.Logging;

/// <summary>
/// Her istege bir korelasyon kimligi bagler. Istemci <c>X-Correlation-Id</c> gonderirse
/// o kullanilir (cagri zinciri boyunca ayni kimlik akar), yoksa uretilir.
///
/// Kimlik uc yere gider: o istek boyunca yazilan tum log satirlarina,
/// cevap basligina ve hata cevabinin govdesine. Kullanici "hata aldim" dediginde
/// tek bir kimlikle butun izi bulmak icin.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Read(context) ?? Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;

        // Cevap basligi, govde yazilmaya baslamadan once eklenmeli.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // LogContext'e itildigi icin bu istek boyunca yazilan her log satirinda alan olarak yer alir.
        using (LogContext.PushProperty(ItemKey, correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>Hata cevabina yazmak icin; middleware disindan da okunabilsin.</summary>
    public static string? Of(HttpContext context)
        => context.Items.TryGetValue(ItemKey, out var value) ? value as string : null;

    private static string? Read(HttpContext context)
    {
        var value = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Disaridan gelen deger log alanina yaziliyor; uzunluk ve karakter kumesi sinirlanir.
        var trimmed = value.Trim();
        if (trimmed.Length > 64) trimmed = trimmed[..64];

        return trimmed.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') ? trimmed : null;
    }
}
