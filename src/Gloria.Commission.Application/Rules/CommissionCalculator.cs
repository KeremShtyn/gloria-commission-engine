using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Rules;

/// <inheritdoc cref="ICommissionCalculator"/>
public sealed class CommissionCalculator : ICommissionCalculator
{
    private readonly IReadOnlyDictionary<CommissionRuleType, ICommissionRuleStrategy> _strategies;

    public CommissionCalculator(IEnumerable<ICommissionRuleStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.RuleType);
    }

    public CommissionCalculation Calculate(
        Employee employee,
        IReadOnlyList<SaleRecord> sales,
        IReadOnlyList<CommissionRule> rules)
    {
        var departmentCode = employee.Department?.Code ?? string.Empty;

        var excluded = new List<ExcludedSale>();
        var byRule = new Dictionary<int, (CommissionRule Rule, List<SaleRecord> Sales)>();

        foreach (var sale in sales)
        {
            if (!sale.IsCommissionable)
            {
                excluded.Add(Exclude(sale, "NOT_COMMISSIONABLE",
                    $"Kayıt durumu '{sale.Status}' olduğu için prime esas değil."));
                continue;
            }

            if (!employee.IsEmployedOn(sale.TransactionDate))
            {
                excluded.Add(Exclude(sale, "OUTSIDE_EMPLOYMENT",
                    $"Satış tarihi {sale.TransactionDate:yyyy-MM-dd}, personelin istihdam aralığı dışında."));
                continue;
            }

            var rule = RuleMatcher.Resolve(rules, sale, departmentCode);
            if (rule is null)
            {
                excluded.Add(Exclude(sale, "NO_MATCHING_RULE",
                    $"'{sale.ProductCode}' ürünü için yürürlükte kural yok."));
                continue;
            }

            if (!byRule.TryGetValue(rule.Id, out var bucket))
            {
                bucket = (rule, new List<SaleRecord>());
                byRule[rule.Id] = bucket;
            }

            bucket.Sales.Add(sale);
        }

        var lines = new List<CalculatedLine>();
        var order = 0;

        foreach (var (rule, ruleSales) in byRule.Values.OrderBy(b => b.Rule.Code))
        {
            if (!_strategies.TryGetValue(rule.RuleType, out var strategy))
                throw new DomainException("RULE_TYPE_UNSUPPORTED",
                    $"'{rule.RuleType}' kural tipi için strateji kayıtlı değil.");

            var ordered = ruleSales
                .OrderBy(s => s.TransactionDate)
                .ThenBy(s => s.SourceDocumentNo)
                .ToList();

            foreach (var step in strategy.Calculate(rule, ordered))
                lines.Add(new CalculatedLine { Rule = rule, Step = step, StepOrder = ++order });
        }

        var commissionable = byRule.Values.SelectMany(b => b.Sales).ToList();

        return new CommissionCalculation
        {
            TotalSalesBase = Money.Round(commissionable.Sum(s => s.AmountTry)),
            TotalCommission = Money.Round(lines.Sum(l => l.Step.CommissionAmount)),
            Lines = lines,
            ExcludedSales = excluded
        };
    }

    private static ExcludedSale Exclude(SaleRecord sale, string code, string reason)
        => new() { Sale = sale, ReasonCode = code, Reason = reason };
}
