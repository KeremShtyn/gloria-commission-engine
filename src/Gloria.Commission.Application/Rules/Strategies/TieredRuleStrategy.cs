using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Rules.Strategies;

/// <summary>
/// Kademeli barem. Oran, personelin o kurala düşen <b>aylık net cirosuna</b> göre seçilir.
/// İadeler net ciroyu düşürdüğü için personeli alt kademeye indirebilir — istenen davranış budur.
/// </summary>
public sealed class TieredRuleStrategy : ICommissionRuleStrategy
{
    public CommissionRuleType RuleType => CommissionRuleType.Tiered;

    public IReadOnlyList<CalculationStep> Calculate(CommissionRule rule, IReadOnlyList<SaleRecord> sales)
    {
        var tiers = rule.Tiers.OrderBy(t => t.MinAmount).ToList();
        if (tiers.Count == 0)
            throw new DomainException("RULE_TIERS_MISSING", $"'{rule.Code}' kuralında kademe tanımlı değil.");

        var netBase = Money.Round(sales.Sum(s => s.AmountTry));

        return rule.TierApplication == TierApplication.Marginal
            ? CalculateMarginal(rule, tiers, netBase, sales.Count)
            : CalculateWholeAmount(rule, tiers, netBase, sales);
    }

    /// <summary>Hedef aşılırsa yüksek oran cironun tamamına uygulanır.</summary>
    private static List<CalculationStep> CalculateWholeAmount(
        CommissionRule rule,
        List<CommissionRuleTier> tiers,
        decimal netBase,
        IReadOnlyList<SaleRecord> sales)
    {
        var tier = ResolveTier(tiers, netBase);
        var tierIndex = tiers.IndexOf(tier) + 1;

        var steps = new List<CalculationStep>(sales.Count + 1)
        {
            new()
            {
                BaseAmount = netBase,
                AppliedRate = tier.Rate,
                CommissionAmount = 0m,
                Explanation =
                    $"'{rule.Name}' aylık net ciro {Money.Format(netBase)} TRY -> " +
                    $"{tierIndex}. kademe ({DescribeTier(tier)}), oran %{Money.Format(tier.Rate * 100)}"
            }
        };

        foreach (var sale in sales)
        {
            var commission = Money.Round(sale.AmountTry * tier.Rate);
            var label = sale.Status == SaleStatus.Refund ? "İade" : "Satış";

            steps.Add(new CalculationStep
            {
                SaleRecordId = sale.Id,
                BaseAmount = sale.AmountTry,
                AppliedRate = tier.Rate,
                Quantity = sale.Quantity,
                CommissionAmount = commission,
                Explanation =
                    $"{label} {sale.SourceDocumentNo} ({sale.ProductName}): " +
                    $"{Money.Format(sale.AmountTry)} TRY x %{Money.Format(tier.Rate * 100)} = {Money.Format(commission)} TRY"
            });
        }

        return steps;
    }

    /// <summary>Her kademe yalnızca kendi aralığına düşen tutara uygulanır.</summary>
    private static List<CalculationStep> CalculateMarginal(
        CommissionRule rule,
        List<CommissionRuleTier> tiers,
        decimal netBase,
        int saleCount)
    {
        var steps = new List<CalculationStep>
        {
            new()
            {
                BaseAmount = netBase,
                CommissionAmount = 0m,
                Explanation =
                    $"'{rule.Name}' aylık net ciro {Money.Format(netBase)} TRY, " +
                    $"{saleCount} satış satırı, dilimli barem uygulanıyor"
            }
        };

        // Negatif net ciroda dilimleme anlamsız; en alt kademe oranı ile ters kayıt yazılır.
        if (netBase <= 0)
        {
            var lowest = tiers[0];
            steps.Add(new CalculationStep
            {
                BaseAmount = netBase,
                AppliedRate = lowest.Rate,
                CommissionAmount = Money.Round(netBase * lowest.Rate),
                Explanation =
                    $"Net ciro negatif -> en alt kademe oranı %{Money.Format(lowest.Rate * 100)} ile mahsup"
            });
            return steps;
        }

        var remaining = netBase;
        var index = 0;

        foreach (var tier in tiers)
        {
            index++;
            if (remaining <= 0) break;
            if (netBase <= tier.MinAmount) break;

            var upper = tier.MaxAmount ?? netBase;
            var sliceTop = Math.Min(netBase, upper);
            var slice = sliceTop - tier.MinAmount;
            if (slice <= 0) continue;

            var commission = Money.Round(slice * tier.Rate);
            remaining -= slice;

            steps.Add(new CalculationStep
            {
                BaseAmount = slice,
                AppliedRate = tier.Rate,
                CommissionAmount = commission,
                Explanation =
                    $"{index}. dilim ({DescribeTier(tier)}): {Money.Format(slice)} TRY x " +
                    $"%{Money.Format(tier.Rate * 100)} = {Money.Format(commission)} TRY"
            });
        }

        return steps;
    }

    /// <summary>
    /// Net ciroyu kapsayan kademeyi bulur.
    /// Ciro negatifse (iadeler satışları aşmışsa) en alt kademe oranı ile mahsup edilir.
    /// </summary>
    private static CommissionRuleTier ResolveTier(List<CommissionRuleTier> tiers, decimal netBase)
        => tiers.FirstOrDefault(t => t.Contains(netBase)) ?? tiers[0];

    private static string DescribeTier(CommissionRuleTier tier)
        => tier.MaxAmount is null
            ? $"{Money.Format(tier.MinAmount)} TRY ve üzeri"
            : $"{Money.Format(tier.MinAmount)}-{Money.Format(tier.MaxAmount.Value)} TRY";
}
