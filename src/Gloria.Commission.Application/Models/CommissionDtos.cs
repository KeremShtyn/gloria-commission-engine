namespace Gloria.Commission.Application.Models;

/// <summary>Prim hesabinin API cevabi: sonuc + hesaplama adimlari.</summary>
public sealed record CommissionResultDto
{
    public string Period { get; init; } = string.Empty;
    public bool PeriodClosed { get; init; }

    public string EmployeeNo { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Hotel { get; init; } = string.Empty;

    public decimal TotalSalesBase { get; init; }
    public decimal TotalCommission { get; init; }

    public DateTime CalculatedAtUtc { get; init; }

    public IReadOnlyList<CommissionStepDto> Steps { get; init; } = Array.Empty<CommissionStepDto>();
    public IReadOnlyList<ExcludedSaleDto> ExcludedSales { get; init; } = Array.Empty<ExcludedSaleDto>();
}

public sealed record CommissionStepDto
{
    public int Order { get; init; }

    public string RuleCode { get; init; } = string.Empty;
    public string RuleName { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;

    public string? SourceSystem { get; init; }
    public string? SourceDocumentNo { get; init; }
    public string? ProductName { get; init; }
    public DateOnly? TransactionDate { get; init; }

    public decimal BaseAmount { get; init; }
    public decimal? AppliedRate { get; init; }
    public decimal? AppliedFixedAmount { get; init; }
    public int Quantity { get; init; }
    public decimal CommissionAmount { get; init; }

    public string Explanation { get; init; } = string.Empty;
}

public sealed record ExcludedSaleDto
{
    public string SourceSystem { get; init; } = string.Empty;
    public string SourceDocumentNo { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal AmountTry { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Muhasebe ekraninin donem ozeti.</summary>
public sealed record PeriodSummaryDto
{
    public string Period { get; init; } = string.Empty;
    public bool Closed { get; init; }
    public int EmployeeCount { get; init; }
    public decimal TotalSalesBase { get; init; }
    public decimal TotalCommission { get; init; }
    public IReadOnlyList<EmployeeCommissionDto> Employees { get; init; } = Array.Empty<EmployeeCommissionDto>();
}

public sealed record EmployeeCommissionDto
{
    public string EmployeeNo { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Hotel { get; init; } = string.Empty;
    public decimal TotalSalesBase { get; init; }
    public decimal TotalCommission { get; init; }
}
