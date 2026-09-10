using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Gloria.Commission.Api.Logging;
using Gloria.Commission.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gloria.Commission.Api.Security;

/// <summary>
/// Kimligi HTTP basligindan okuyup gercek bir <see cref="ClaimsPrincipal"/> uretir.
///
/// Bu sinif tek basina "kimlik nereden geliyor" sorusunun cevabidir. Uretimde
/// <c>AddJwtBearer</c> ile degistirilir; controller'lardaki <c>[Authorize]</c> nitelikleri,
/// policy'ler ve servis kontrolleri aynen kalir.
/// </summary>
public sealed class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawRole = Header(AuthenticationHeaders.Role);

        // Rol basligi yoksa istek kimliksizdir: 401. Yetersiz rol ise 403 doner.
        if (rawRole is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Enum.TryParse<UserRole>(rawRole, ignoreCase: true, out var role))
            return Task.FromResult(AuthenticateResult.Fail(
                $"'{rawRole}' gecerli bir rol degil. Beklenen: Admin, Accounting, Employee."));

        var userId = Header(AuthenticationHeaders.UserId) ?? "anonymous";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role.ToString())
        };

        // Personel rolunde hangi personel oldugu bilgisi yetki kararinin girdisidir.
        if (Header(AuthenticationHeaders.EmployeeNo) is { } employeeNo)
            claims.Add(new Claim(AuthenticationHeaders.EmployeeNoClaim, employeeNo));

        var identity = new ClaimsIdentity(claims, AuthenticationHeaders.Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationHeaders.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>Kimliksiz istek: 401. Govde uygulamanin tek hata bicimindedir.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => WriteErrorAsync(
            StatusCodes.Status401Unauthorized,
            "UNAUTHENTICATED",
            $"Kimlik bilgisi yok. '{AuthenticationHeaders.Role}' basligi gonderilmeli " +
            "(Admin, Accounting veya Employee).");

    /// <summary>Kimlik var ama yetki yetersiz: 403.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => WriteErrorAsync(
            StatusCodes.Status403Forbidden,
            "FORBIDDEN",
            "Bu islem icin yetkiniz yok.");

    private async Task WriteErrorAsync(int status, string code, string message)
    {
        if (Response.HasStarted) return;

        Response.StatusCode = status;
        Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            error = new
            {
                code,
                message,
                correlationId = CorrelationIdMiddleware.Of(Context),
                timestamp = DateTime.UtcNow,
                path = Request.Path.Value
            }
        };

        await Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private string? Header(string name)
    {
        var value = Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
