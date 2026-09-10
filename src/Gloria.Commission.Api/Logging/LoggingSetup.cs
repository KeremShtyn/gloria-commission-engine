using Gloria.Commission.Api.Security;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Gloria.Commission.Api.Logging;

/// <summary>
/// Uygulama logunun yapilandirmasi.
///
/// Log stdout'a yazilir; uygulama hangi backend'e gittigini bilmez.
/// Uretimde Filebeat/Fluent Bit gibi bir toplayici stdout'u okuyup Elastic'e tasir —
/// boylece log altyapisi cokse bile uygulama etkilenmez.
/// </summary>
public static class LoggingSetup
{
    public static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration logger)
    {
        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "gloria-commission-api");

        if (context.HostingEnvironment.IsDevelopment())
        {
            logger.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
        }
        else
        {
            // Satir basina bir JSON nesnesi: log toplayicilarin dogrudan okudugu bicim.
            logger.WriteTo.Console(new CompactJsonFormatter());
        }
    }

    /// <summary>
    /// Saglik kontrolu her 15 saniyede bir cagriliyor; basarili olanlari Information
    /// seviyesinde yazmak log toplayicisini gereksiz doldurur. Hata verirse gorunur kalir.
    /// </summary>
    public static LogEventLevel LevelForRequest(
        HttpContext context, double elapsedMs, Exception? exception)
    {
        if (exception is not null) return LogEventLevel.Error;
        if (context.Response.StatusCode >= 500) return LogEventLevel.Error;

        var isHealthCheck = context.Request.Path.StartsWithSegments("/health");
        if (isHealthCheck && context.Response.StatusCode < 400) return LogEventLevel.Verbose;

        return context.Response.StatusCode >= 400 ? LogEventLevel.Warning : LogEventLevel.Information;
    }

    /// <summary>
    /// Istek tamamlanma logunu zenginlestirir.
    ///
    /// Bilerek yazilmayanlar: prim tutari, ciro, personel adi. Bu sistem maasi etkiliyor;
    /// hassas degerlerin log toplayicida herkesin gorebilecegi bir yere dusmesi
    /// audit log'un sagladigi kontrolu anlamsiz kilardi. Loga yalnizca
    /// kim/ne yapti bilgisi gider, ne kadar bilgisi gitmez.
    /// </summary>
    public static void EnrichFromRequest(Serilog.IDiagnosticContext diagnosticContext, HttpContext context)
    {
        diagnosticContext.Set("UserId", Header(context, AuthenticationHeaders.UserId) ?? "anonymous");
        diagnosticContext.Set("UserRole", Header(context, AuthenticationHeaders.Role) ?? "unknown");

        // Personel numarasi kimlik bilgisi degil, kayit anahtari — audit log ile eslesme icin gerekli.
        var employeeNo = Header(context, AuthenticationHeaders.EmployeeNo);
        if (employeeNo is not null) diagnosticContext.Set("EmployeeNo", employeeNo);
    }

    private static string? Header(HttpContext context, string name)
    {
        var value = context.Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
