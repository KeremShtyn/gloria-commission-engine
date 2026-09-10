using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Rules;

/// <summary>
/// Bir satis satirina hangi kuralin uygulanacagini belirler.
/// Scope alanlari null olan kural "hepsi" demektir; birden fazla kural uyarsa
/// once <see cref="CommissionRule.Priority"/>, esitlikte daha spesifik olan kazanir.
/// </summary>
public static class RuleMatcher
{
    public static bool Matches(CommissionRule rule, SaleRecord sale, Employee employee)
    {
        if (!rule.IsEffectiveOn(sale.TransactionDate)) return false;

        if (rule.SourceSystem is { } source && source != sale.SourceSystem) return false;

        if (rule.ProductGroupId is { } group && group != sale.ProductGroupId) return false;

        // Departman ve otel personelin ozellikleridir, satisin degil.
        // Otel bilgisi kaynak dosyalarda yalnizca PMS'te bulunuyor; satis uzerinden
        // eslestirilseydi POS ve ERP satislari hicbir otel kuraliyla eslesmezdi.
        if (rule.DepartmentId is { } department && department != employee.DepartmentId) return false;

        if (rule.HotelId is { } hotel && hotel != employee.HotelId) return false;

        if (!string.IsNullOrWhiteSpace(rule.ProductCode)
            && !string.Equals(rule.ProductCode, sale.ProductCode, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static CommissionRule? Resolve(
        IEnumerable<CommissionRule> rules,
        SaleRecord sale,
        Employee employee)
        => rules
            .Where(r => Matches(r, sale, employee))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Specificity)
            .ThenBy(r => r.Code, StringComparer.Ordinal)
            .FirstOrDefault();
}
