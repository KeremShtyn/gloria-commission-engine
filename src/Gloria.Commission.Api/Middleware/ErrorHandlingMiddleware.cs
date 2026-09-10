using System.Text.Json;
using Gloria.Commission.Api.Logging;
using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Api.Middleware;

/// <summary>
/// Tum hatalari tek bir cevap formatinda dondurur:
/// { "error": { "code", "message", "correlationId", "timestamp", "path" } }
///
/// Is kurali ihlali 422, yetki hatasi 403, bulunamadi 404, cakisma 409, digerleri 500.
/// Beklenmeyen hatalarda ic detay istemciye sizmaz; korelasyon kimligi ile loga baglanir.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Is kurali ihlali {ErrorCode}: {ErrorMessage}", ex.Code, ex.Message);
            await WriteAsync(context, StatusFor(ex.Code), ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata: {Path}", context.Request.Path.Value);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR", "Beklenmeyen bir hata olustu.");
        }
    }

    private static int StatusFor(string code) => code switch
    {
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        "CONCURRENT_UPDATE" => StatusCodes.Status409Conflict,
        var c when c.EndsWith("_NOT_FOUND", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
        var c when c.EndsWith("_EXISTS", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity
    };

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            error = new
            {
                code,
                message,
                correlationId = CorrelationIdMiddleware.Of(context),
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.Value
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
