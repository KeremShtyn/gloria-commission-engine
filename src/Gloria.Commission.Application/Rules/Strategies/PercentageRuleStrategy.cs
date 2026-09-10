using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Rules.Strategies;

/// <summary>Sabit yüzde: her satış satırına aynı oran uygulanır.</summary>
public sealed class PercentageRuleStrategy : ICommissionRuleStrategy
{
    public CommissionRuleType RuleType => CommissionRuleType.Percentage;

    public IReadOnlyList<CalculationStep> Calculate(CommissionRule rule, IReadOnlyList<SaleRecord> sales)
    {
        if (rule.Rate is not { } rate)
            throw new DomainException("RULE_RATE_MISSING", $"'{rule.Code}' kuralında oran tanımlı değil.");

        var steps = new List<CalculationStep>(sales.Count);

        foreach (var sale in sales)
        {
            var commission = Money.Round(sale.AmountTry * rate);
            var label = sale.Status == SaleStatus.Refund ? "İade" : "Satış";

            steps.Add(new CalculationStep
            {
                SaleRecordId = sale.Id,
                BaseAmount = sale.AmountTry,
                AppliedRate = rate,
                Quantity = sale.Quantity,
                CommissionAmount = commission,
                Explanation =
                    $"{label} {sale.SourceDocumentNo} ({sale.ProductName}): " +
                    $"{Money.Format(sale.AmountTry)} TRY x %{Money.Format(rate * 100)} = {Money.Format(commission)} TRY"
            });
        }

        return steps;
    }
}
