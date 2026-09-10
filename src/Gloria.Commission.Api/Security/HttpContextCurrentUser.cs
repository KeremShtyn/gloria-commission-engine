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
    public HttpContextCurrentUser(ClaimsPrincipal user)
    {
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        EmployeeNo = user.FindFirstValue(AuthenticationHeaders.EmployeeNoClaim);

        // Rol claim'i okunamazsa en dar yetki uygulanir.
        Role = Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : UserRole.Employee;
    }

    public string UserId { get; }
    public UserRole Role { get; }
    public string? EmployeeNo { get; }
}

/// <summary>
/// Zamanlanmis aktarim gibi HTTP istegi olmadan calisan islerin kullanicisi.
///
/// Rol olarak Muhasebe verilir, Admin degil: zamanlanmis is, muhasebenin elle
/// yaptigi aktarimin gozetimsiz calisan halidir — kural degistirme yetkisine ihtiyaci yok.
/// Denetim kayitlarinda aktor "system" olarak gorunur.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public string UserId => "system";
    public UserRole Role => UserRole.Accounting;
    public string? EmployeeNo => null;
}
