using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Rules.Strategies;

/// <summary>
/// Sabit tutar: işlem başına (veya adet başına) sabit TL.
/// İade satırlarında tutar negatife çevrilir — aksi halde iade prim kazandırırdı.
/// </summary>
public sealed class FixedAmountRuleStrategy : ICommissionRuleStrategy
{
    public CommissionRuleType RuleType => CommissionRuleType.FixedAmount;

    public IReadOnlyList<CalculationStep> Calculate(CommissionRule rule, IReadOnlyList<SaleRecord> sales)
    {
        if (rule.FixedAmount is not { } fixedAmount)
            throw new DomainException("RULE_FIXED_AMOUNT_MISSING", $"'{rule.Code}' kuralında sabit tutar tanımlı değil.");

        var steps = new List<CalculationStep>(sales.Count);

        foreach (var sale in sales)
        {
            var quantity = rule.MultiplyByQuantity ? Math.Abs(sale.Quantity) : 1;
            var sign = sale.Status == SaleStatus.Refund ? -1 : 1;
            var commission = Money.Round(fixedAmount * quantity * sign);

            var label = sale.Status == SaleStatus.Refund ? "İade" : "Satış";
            var basis = rule.MultiplyByQuantity ? $"{quantity} adet x " : "işlem başına ";

            steps.Add(new CalculationStep
            {
                SaleRecordId = sale.Id,
                BaseAmount = sale.AmountTry,
                AppliedFixedAmount = fixedAmount,
                Quantity = sale.Quantity,
                CommissionAmount = commission,
                Explanation =
                    $"{label} {sale.SourceDocumentNo} ({sale.ProductName}): " +
                    $"{basis}{Money.Format(fixedAmount)} TRY = {Money.Format(commission)} TRY"
            });
        }

        return steps;
    }
}
