using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Rules;

/// <summary>Kural motorunun saf (veritabanından bağımsız) çıktısı.</summary>
public sealed record CommissionCalculation
{
    /// <summary>Prime esas net ciro (iadeler düşülmüş).</summary>
    public decimal TotalSalesBase { get; init; }

    public decimal TotalCommission { get; init; }

    public IReadOnlyList<CalculatedLine> Lines { get; init; } = Array.Empty<CalculatedLine>();

    /// <summary>Hiçbir kurala uymayan satışlar. Sessizce yutulmaz, raporlanır.</summary>
    public IReadOnlyList<ExcludedSale> ExcludedSales { get; init; } = Array.Empty<ExcludedSale>();
}

public sealed record CalculatedLine
{
    public CommissionRule Rule { get; init; } = null!;
    public CalculationStep Step { get; init; } = null!;
    public int StepOrder { get; init; }
}

public sealed record ExcludedSale
{
    public SaleRecord Sale { get; init; } = null!;
    public string ReasonCode { get; init; } = null!;
    public string Reason { get; init; } = null!;
}
