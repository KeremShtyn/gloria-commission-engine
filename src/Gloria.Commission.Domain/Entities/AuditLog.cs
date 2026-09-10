using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Kim, ne zaman, neyi, hangi değerden hangi değere değiştirdi.
/// DbContext.SaveChanges içinde otomatik üretilir; servis katmanı unutamaz.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    public string EntityName { get; set; } = null!;
    public string EntityId { get; set; } = null!;

    public AuditAction Action { get; set; }

    /// <summary>Değişen alanların eski değerleri (JSON). Insert'te null.</summary>
    public string? OldValues { get; set; }

    /// <summary>Değişen alanların yeni değerleri (JSON). Delete'te null.</summary>
    public string? NewValues { get; set; }

    public string ChangedBy { get; set; } = null!;
    public string ChangedByRole { get; set; } = null!;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
