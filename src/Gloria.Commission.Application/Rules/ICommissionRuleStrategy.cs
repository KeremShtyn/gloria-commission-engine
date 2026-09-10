using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Rules;

/// <summary>
/// Bir kural <b>tipinin</b> hesaplama davranışı.
/// Yeni bir kural eklemek için kod yazılmaz (kural veridir);
/// yeni bir kural <b>tipi</b> eklemek için bu arayüzün yeni bir implementasyonu eklenir.
/// </summary>
public interface ICommissionRuleStrategy
{
    CommissionRuleType RuleType { get; }

    /// <param name="rule">Uygulanacak kural.</param>
    /// <param name="sales">Bu kurala eşleşen, prime esas satışlar (iadeler negatif tutarlı).</param>
    IReadOnlyList<CalculationStep> Calculate(CommissionRule rule, IReadOnlyList<SaleRecord> sales);
}
