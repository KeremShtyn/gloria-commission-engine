namespace Gloria.Commission.Domain.Enums;

public enum UserRole
{
    /// <summary>Kuralları yönetir.</summary>
    Admin = 1,

    /// <summary>Tüm personelin primini görür.</summary>
    Accounting = 2,

    /// <summary>Yalnızca kendi primini görür.</summary>
    Employee = 3
}
