namespace Gloria.Commission.Api.Security;

/// <summary>
/// Kimlik bilgisinin tasindigi basliklar.
///
/// Case gercek bir kimlik dogrulama sistemi istemiyor; kimlik bu basliklardan okunuyor.
/// Yetkilendirme ise ASP.NET'in kendi altyapisiyla yapiliyor — JWT'ye gecis
/// yalnizca kimlik kaynaginin degismesi demek, yetki kurallari oldugu gibi kalir.
/// </summary>
public static class AuthenticationHeaders
{
    public const string Scheme = "GloriaHeader";

    public const string UserId = "X-User-Id";
    public const string Role = "X-User-Role";
    public const string EmployeeNo = "X-Employee-No";

    /// <summary>Personel numarasinin tasindigi claim tipi.</summary>
    public const string EmployeeNoClaim = "employee_no";
}
