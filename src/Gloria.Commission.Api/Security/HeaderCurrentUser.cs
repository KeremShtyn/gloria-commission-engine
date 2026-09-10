using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Api.Security;

/// <summary>
/// Rolu HTTP header'dan okur. Case kapsaminda gercek kimlik dogrulama beklenmiyor;
/// uretimde bu sinif JWT claim'lerini okuyan bir implementasyonla degistirilir,
/// cagiran hicbir kod degismez.
///
///   X-User-Id      : islemi yapan kullanici (audit log'un aktoru)
///   X-User-Role    : Admin | Accounting | Employee
///   X-Employee-No  : rol Employee ise kendi personel numarasi
/// </summary>
public sealed class HeaderCurrentUser : ICurrentUser
{
    public const string UserIdHeader = "X-User-Id";
    public const string RoleHeader = "X-User-Role";
    public const string EmployeeNoHeader = "X-Employee-No";

    public HeaderCurrentUser(IHttpContextAccessor accessor)
    {
        var headers = accessor.HttpContext?.Request.Headers;

        UserId = Header(headers, UserIdHeader) ?? "anonymous";
        EmployeeNo = Header(headers, EmployeeNoHeader);

        Role = Enum.TryParse<UserRole>(Header(headers, RoleHeader), ignoreCase: true, out var role)
            ? role
            : UserRole.Employee;   // Rol okunamazsa en dar yetki uygulanir.
    }

    public string UserId { get; }
    public UserRole Role { get; }
    public string? EmployeeNo { get; }

    private static string? Header(IHeaderDictionary? headers, string name)
    {
        if (headers is null) return null;
        var value = headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
