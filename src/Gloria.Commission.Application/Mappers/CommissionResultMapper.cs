using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Mappers;

public static class CommissionResultMapper
{
    public static CommissionResultResponse ToResponse(
        Period period,
        Employee employee,
        CommissionCalculation calculation,
        IReadOnlyList<SaleRecord> sales)
    {
        var salesById = sales.ToDictionary(s => s.Id);

        SaleRecord? SaleOf(long? id)
            => id is { } saleId && salesById.TryGetValue(saleId, out var sale) ? sale : null;

        return new CommissionResultResponse
        {
            Period = period.ToString(),
            PeriodClosed = period.IsClosed,
            EmployeeNo = employee.EmployeeNo,
            FullName = employee.FullName,
            Department = employee.Department.Code,
            Hotel = employee.Hotel,
            TotalSalesBase = calculation.TotalSalesBase,
            TotalCommission = calculation.TotalCommission,
            CalculatedAtUtc = DateTime.UtcNow,
            Steps = calculation.Lines.Select(line =>
            {
                var sale = SaleOf(line.Step.SaleRecordId);

                return new CommissionStepResponse
                {
                    Order = line.StepOrder,
                    RuleCode = line.Rule.Code,
                    RuleName = line.Rule.Name,
                    RuleType = line.Rule.RuleType.ToString(),
                    SourceSystem = sale?.SourceSystem.ToString(),
                    SourceDocumentNo = sale?.SourceDocumentNo,
                    ProductName = sale?.ProductName,
                    TransactionDate = sale?.TransactionDate,
                    BaseAmount = line.Step.BaseAmount,
                    AppliedRate = line.Step.AppliedRate,
                    AppliedFixedAmount = line.Step.AppliedFixedAmount,
                    Quantity = line.Step.Quantity,
                    CommissionAmount = line.Step.CommissionAmount,
                    Explanation = line.Step.Explanation
                };
            }).ToList(),
            ExcludedSales = calculation.ExcludedSales.Select(excluded => new ExcludedSaleResponse
            {
                SourceSystem = excluded.Sale.SourceSystem.ToString(),
                SourceDocumentNo = excluded.Sale.SourceDocumentNo,
                TransactionDate = excluded.Sale.TransactionDate,
                ProductCode = excluded.Sale.ProductCode,
                ProductName = excluded.Sale.ProductName,
                AmountTry = excluded.Sale.AmountTry,
                ReasonCode = excluded.ReasonCode,
                Reason = excluded.Reason
            }).ToList()
        };
    }

    /// <summary>Hesap sonucunu kalici kayda cevirir; adimlar izlenebilirligin taşiyicisi.</summary>
    public static CommissionResult ToEntity(
        int periodId, int employeeId, CommissionCalculation calculation, string calculatedBy) => new()
    {
        PeriodId = periodId,
        EmployeeId = employeeId,
        TotalSalesBase = calculation.TotalSalesBase,
        TotalCommission = calculation.TotalCommission,
        CalculatedBy = calculatedBy,
        CalculatedAtUtc = DateTime.UtcNow,
        Lines = calculation.Lines.Select(line => new CommissionResultLine
        {
            SaleRecordId = line.Step.SaleRecordId,
            CommissionRuleId = line.Rule.Id,
            RuleCode = line.Rule.Code,
            RuleType = line.Rule.RuleType.ToString(),
            BaseAmount = line.Step.BaseAmount,
            AppliedRate = line.Step.AppliedRate,
            AppliedFixedAmount = line.Step.AppliedFixedAmount,
            Quantity = line.Step.Quantity,
            CommissionAmount = line.Step.CommissionAmount,
            Explanation = line.Step.Explanation,
            StepOrder = line.StepOrder
        }).ToList()
    };
}
