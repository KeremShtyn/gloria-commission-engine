using FluentAssertions;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>Kademeli barem: oran aylik net ciroya gore secilir.</summary>
public class TieredRuleTests
{
    [Fact]
    public void Hedefin_altinda_kalan_ciro_ilk_kademe_oranini_alir()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 20_000m, TestData.AlcGroupId) };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.TieredRule()]);

        // 20.000 -> 1. kademe (%3)
        result.TotalCommission.Should().Be(600m);
    }

    [Fact]
    public void Hedef_asilinca_oran_tum_ciroya_uygulanir()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 30_000m, TestData.AlcGroupId, day: 3),
            TestData.Sale(2, 10_000m, TestData.AlcGroupId, day: 9)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.TieredRule()]);

        // 40.000 -> 2. kademe (%5), oran cironun tamamina uygulanir
        result.TotalCommission.Should().Be(2_000m);
    }

    [Fact]
    public void Kademe_siniri_dahil_alt_sinirdan_baslar()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 60_000m, TestData.AlcGroupId) };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.TieredRule()]);

        // 60.000 tam sinirda -> 3. kademe (%7)
        result.TotalCommission.Should().Be(4_200m);
    }

    [Fact]
    public void Dilimli_baremde_her_kademe_kendi_araligina_uygulanir()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 70_000m, TestData.AlcGroupId) };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales,
            [TestData.TieredRule(TierApplication.Marginal)]);

        // 30.000 x %3 + 30.000 x %5 + 10.000 x %7 = 900 + 1.500 + 700
        result.TotalCommission.Should().Be(3_100m);
    }

    [Fact]
    public void Secilen_kademe_hesap_adimlarinda_aciklanir()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 45_000m, TestData.AlcGroupId) };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.TieredRule()]);

        result.Lines.Should().NotBeEmpty();
        result.Lines[0].Step.Explanation.Should().Contain("2. kademe");
        result.Lines[0].Step.AppliedRate.Should().Be(0.05m);
    }
}
