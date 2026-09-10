using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Rules;

/// <summary>
/// Bir satış satırına hangi kuralın uygulanacağını belirler.
/// Scope alanları null olan kural "hepsi" demektir; birden fazla kural uyarsa
/// önce <see cref="CommissionRule.Priority"/>, eşitlikte daha spesifik olan kazanır.
/// </summary>
public static class RuleMatcher
{
    public static bool Matches(CommissionRule rule, SaleRecord sale, string departmentCode)
    {
        if (!rule.IsEffectiveOn(sale.TransactionDate)) return false;
        if (rule.SourceSystem is { } src && src != sale.SourceSystem) return false;
        if (!IsWildcard(rule.DepartmentCode) && !Eq(rule.DepartmentCode, departmentCode)) return false;
        if (!IsWildcard(rule.ProductGroup) && !Eq(rule.ProductGroup, sale.ProductGroup)) return false;
        if (!IsWildcard(rule.ProductCode) && !Eq(rule.ProductCode, sale.ProductCode)) return false;
        if (!IsWildcard(rule.Hotel) && !Eq(rule.Hotel, sale.Hotel)) return false;
        return true;
    }

    public static CommissionRule? Resolve(
        IEnumerable<CommissionRule> rules,
        SaleRecord sale,
        string departmentCode)
        => rules
            .Where(r => Matches(r, sale, departmentCode))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.Specificity)
            .ThenBy(r => r.Id)
            .FirstOrDefault();

    private static bool IsWildcard(string? value) => string.IsNullOrWhiteSpace(value);

    private static bool Eq(string? a, string? b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
