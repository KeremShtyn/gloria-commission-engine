using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Abstractions;

/// <summary>
/// İsteği yapan kullanıcı. Gerçek kimlik doğrulama yerine HTTP header'dan okunur
/// (X-User-Id, X-User-Role); denetim kayıtlarının aktörü budur.
/// </summary>
public interface ICurrentUser
{
    string UserId { get; }
    UserRole Role { get; }

    /// <summary>Rolü Employee ise kendi personel numarası; diğer rollerde null.</summary>
    string? EmployeeNo { get; }

    bool IsAdmin => Role == UserRole.Admin;
    bool CanSeeAllEmployees => Role is UserRole.Admin or UserRole.Accounting;
}
