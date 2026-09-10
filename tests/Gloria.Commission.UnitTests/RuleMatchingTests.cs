using FluentAssertions;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>Kural eslestirme: scope, oncelik, yururluk ve kod yazmadan kural ekleme.</summary>
public class RuleMatchingTests
{
    [Fact]
    public void Bos_scope_alani_hepsi_anlamina_gelir()
    {
        var rule = TestData.PercentageRule(0.05m);
        rule.ProductGroup = null;

        var sale = TestData.Sale(1, 1_000m, productGroup: "BUGGY");

        RuleMatcher.Matches(rule, sale, "SPA").Should().BeTrue();
    }

    [Fact]
    public void Urun_grubu_uyusmazsa_kural_eslesmez()
    {
        var rule = TestData.PercentageRule(0.05m, productGroup: "SPA");
        var sale = TestData.Sale(1, 1_000m, productGroup: "BUGGY");

        RuleMatcher.Matches(rule, sale, "SPA").Should().BeFalse();
    }

    [Fact]
    public void Yururlukte_olmayan_kural_uygulanmaz()
    {
        var rule = TestData.PercentageRule(0.05m);
        rule.EffectiveFrom = new DateOnly(2026, 9, 1);

        var sale = TestData.Sale(1, 1_000m, day: 5);

        RuleMatcher.Matches(rule, sale, "SPA").Should().BeFalse();
    }

    [Fact]
    public void Yuksek_oncelikli_kural_kazanir()
    {
        var general = TestData.PercentageRule(0.05m);
        general.Id = 1;
        general.Priority = 1;

        var campaign = TestData.PercentageRule(0.12m);
        campaign.Id = 2;
        campaign.Code = "PCT-CAMPAIGN";
        campaign.Priority = 50;

        var sale = TestData.Sale(1, 1_000m);

        RuleMatcher.Resolve([general, campaign], sale, "SPA")!.Code.Should().Be("PCT-CAMPAIGN");
    }

    [Fact]
    public void Oncelik_esitse_daha_spesifik_kural_kazanir()
    {
        var byGroup = TestData.PercentageRule(0.05m);
        byGroup.Id = 1;

        var byProduct = TestData.PercentageRule(0.09m);
        byProduct.Id = 2;
        byProduct.Code = "PCT-PRODUCT";
        byProduct.ProductCode = "SPA_MSJ60";

        var sale = TestData.Sale(1, 1_000m, productCode: "SPA_MSJ60");

        RuleMatcher.Resolve([byGroup, byProduct], sale, "SPA")!.Code.Should().Be("PCT-PRODUCT");
    }

    [Fact]
    public void Kurali_olmayan_satis_sessizce_yutulmaz()
    {
        // Golf dersi: katalogda var, kurali yok.
        var sales = new List<SaleRecord> { TestData.Sale(1, 3_500m, "GOLF", "GLF_LSN") };

        var result = TestData.Calculator().Calculate(
            TestData.Employee("ÖN BÜRO"), sales, [TestData.PercentageRule(0.06m)]);

        result.TotalCommission.Should().Be(0m);
        result.ExcludedSales.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("NO_MATCHING_RULE");
    }

    [Fact]
    public void Yeni_kural_eklemek_kod_degisikligi_gerektirmez()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 3_500m, "GOLF", "GLF_LSN") };

        // Kural yalnizca veri: veritabanina bir satir eklemekle esdeger.
        var golfRule = new CommissionRule
        {
            Id = 42,
            Code = "GOLF-PCT",
            Name = "Golf dersi satisi",
            RuleType = CommissionRuleType.Percentage,
            ProductGroup = "GOLF",
            Rate = 0.08m,
            EffectiveFrom = TestData.Ruleset,
            IsActive = true
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee("ÖN BÜRO"), sales, [TestData.PercentageRule(0.06m), golfRule]);

        result.TotalCommission.Should().Be(280m);
        result.ExcludedSales.Should().BeEmpty();
    }

    [Fact]
    public void Istihdam_disindaki_satis_prime_esas_degildir()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 5_000m, day: 3),
            TestData.Sale(2, 5_000m, day: 25)
        };

        var employee = TestData.Employee(terminationDate: new DateOnly(2026, 8, 12));

        var result = TestData.Calculator().Calculate(employee, sales, [TestData.PercentageRule(0.10m)]);

        result.TotalCommission.Should().Be(500m);
        result.ExcludedSales.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("OUTSIDE_EMPLOYMENT");
    }

    [Fact]
    public void Muhasebelesmemis_erp_kaydi_prime_esas_degildir()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 5_000m, status: SaleStatus.Unposted, source: SourceSystem.Erp)
        };

        var rule = TestData.PercentageRule(0.10m);
        rule.SourceSystem = null;

        var result = TestData.Calculator().Calculate(TestData.Employee(), sales, [rule]);

        result.TotalCommission.Should().Be(0m);
        result.ExcludedSales.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("NOT_COMMISSIONABLE");
    }
}
