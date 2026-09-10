namespace Gloria.Commission.Application.Rules;

/// <summary>
/// Kural motorunun ürettiği tek hesaplama adımı. Veritabanına
/// <see cref="Domain.Entities.CommissionResultLine"/> olarak yazılır.
/// </summary>
public sealed record CalculationStep
{
    public long? SaleRecordId { get; init; }
    public decimal BaseAmount { get; init; }
    public decimal? AppliedRate { get; init; }
    public decimal? AppliedFixedAmount { get; init; }
    public int Quantity { get; init; }
    public decimal CommissionAmount { get; init; }
    public string Explanation { get; init; } = string.Empty;
}
