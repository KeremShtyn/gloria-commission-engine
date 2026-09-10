using Gloria.Commission.Api.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Gloria.Commission.Api.Middleware;

/// <summary>
/// [ApiController]'in urettigi model dogrulama cevabini, uygulamanin
/// tek hata bicimine cevirir: { "error": { code, message, details, timestamp, path } }
/// Boylece istemci tarafinda iki ayri hata bicimi ele alinmaz.
/// </summary>
public static class ValidationProblemFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var details = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => new
            {
                field = entry.Key,
                message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Gecersiz deger."
                    : error.ErrorMessage
            }))
            .ToList();

        var payload = new
        {
            error = new
            {
                code = "VALIDATION_ERROR",
                message = "Gonderilen veri gecerli degil.",
                details,
                correlationId = CorrelationIdMiddleware.Of(context.HttpContext),
                timestamp = DateTime.UtcNow,
                path = context.HttpContext.Request.Path.Value
            }
        };

        return new BadRequestObjectResult(payload);
    }
}
