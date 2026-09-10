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
        rule.ProductGroupId = null;

        var sale = TestData.Sale(1, 1_000m, Guid.Parse("88888888-8888-8888-8888-888888888888"));

        RuleMatcher.Matches(rule, sale, TestData.Employee()).Should().BeTrue();
    }

    [Fact]
    public void Urun_grubu_uyusmazsa_kural_eslesmez()
    {
        var rule = TestData.PercentageRule(0.05m, TestData.SpaGroupId);
        var sale = TestData.Sale(1, 1_000m, Guid.Parse("88888888-8888-8888-8888-888888888888"));

        RuleMatcher.Matches(rule, sale, TestData.Employee()).Should().BeFalse();
    }

    [Fact]
    public void Yururlukte_olmayan_kural_uygulanmaz()
    {
        var rule = TestData.PercentageRule(0.05m);
        rule.EffectiveFrom = new DateOnly(2026, 9, 1);

        var sale = TestData.Sale(1, 1_000m, day: 5);

        RuleMatcher.Matches(rule, sale, TestData.Employee()).Should().BeFalse();
    }

    [Fact]
    public void Yuksek_oncelikli_kural_kazanir()
    {
        var general = TestData.PercentageRule(0.05m);
        general.Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
        general.Priority = 1;

        var campaign = TestData.PercentageRule(0.12m);
        campaign.Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000b");
        campaign.Code = "PCT-CAMPAIGN";
        campaign.Priority = 50;

        var sale = TestData.Sale(1, 1_000m);

        RuleMatcher.Resolve([general, campaign], sale, TestData.Employee())!.Code.Should().Be("PCT-CAMPAIGN");
    }

    [Fact]
    public void Oncelik_esitse_daha_spesifik_kural_kazanir()
    {
        var byGroup = TestData.PercentageRule(0.05m);
        byGroup.Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000c");

        var byProduct = TestData.PercentageRule(0.09m);
        byProduct.Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000d");
        byProduct.Code = "PCT-PRODUCT";
        byProduct.ProductCode = "SPA_MSJ60";

        var sale = TestData.Sale(1, 1_000m, productCode: "SPA_MSJ60");

        RuleMatcher.Resolve([byGroup, byProduct], sale, TestData.Employee())!.Code.Should().Be("PCT-PRODUCT");
    }

    [Fact]
    public void Kurali_olmayan_satis_sessizce_yutulmaz()
    {
        // Golf dersi: katalogda var, kurali yok.
        var sales = new List<SaleRecord> { TestData.Sale(1, 3_500m, TestData.GolfGroupId, "GLF_LSN") };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.PercentageRule(0.06m)]);

        result.TotalCommission.Should().Be(0m);
        result.ExcludedSales.Should().ContainSingle()
            .Which.ReasonCode.Should().Be("NO_MATCHING_RULE");
    }

    [Fact]
    public void Yeni_kural_eklemek_kod_degisikligi_gerektirmez()
    {
        var sales = new List<SaleRecord> { TestData.Sale(1, 3_500m, TestData.GolfGroupId, "GLF_LSN") };

        // Kural yalnizca veri: veritabanina bir satir eklemekle esdeger.
        var golfRule = new CommissionRule
        {
            Id = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000002a"),
            Code = "GOLF-PCT",
            Name = "Golf dersi satisi",
            RuleType = CommissionRuleType.Percentage,
            ProductGroupId = TestData.GolfGroupId,
            Rate = 0.08m,
            EffectiveFrom = TestData.Ruleset,
            IsActive = true
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(TestData.FoodDepartmentId), sales, [TestData.PercentageRule(0.06m), golfRule]);

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
