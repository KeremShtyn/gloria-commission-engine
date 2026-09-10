using System.Security.Claims;
using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Api.Security;

/// <summary>
/// Servis katmaninin gordugu kullanici. Degerler dogrudan basliktan degil,
/// kimlik dogrulama sonucunda olusan claim'lerden okunur — kimlik kaynagi JWT'ye
/// dondugunde bu sinif da degismez.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        EmployeeNo = user?.FindFirstValue(AuthenticationHeaders.EmployeeNoClaim);

        // Rol claim'i okunamazsa en dar yetki uygulanir.
        Role = Enum.TryParse<UserRole>(user?.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : UserRole.Employee;
    }

    public string UserId { get; }
    public UserRole Role { get; }
    public string? EmployeeNo { get; }
}
