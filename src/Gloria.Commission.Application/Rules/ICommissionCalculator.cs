using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Rules;

public interface ICommissionCalculator
{
    /// <summary>
    /// Bir personelin bir dönemdeki primini hesaplar.
    /// Saf fonksiyondur: veritabanına dokunmaz, verilen girdilerden deterministik çıktı üretir.
    /// </summary>
    CommissionCalculation Calculate(
        Employee employee,
        IReadOnlyList<SaleRecord> sales,
        IReadOnlyList<CommissionRule> rules);
}
