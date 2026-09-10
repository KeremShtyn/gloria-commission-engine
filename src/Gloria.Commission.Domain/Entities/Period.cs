using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Prim dönemi (yıl-ay). Kapatıldıktan sonra o döneme ait satış ve prim kayıtları değiştirilemez.
/// </summary>
public class Period
{
    public int Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public PeriodStatus Status { get; set; } = PeriodStatus.Open;

    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }

    public bool IsClosed => Status == PeriodStatus.Closed;

    public static string Key(int year, int month) => $"{year:D4}-{month:D2}";

    public override string ToString() => Key(Year, Month);
}
