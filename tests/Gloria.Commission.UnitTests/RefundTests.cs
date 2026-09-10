using FluentAssertions;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>Iade senaryolari: iade edilen satis prim hesabindan dusulur.</summary>
public class RefundTests
{
    [Fact]
    public void Eslesmis_iade_ile_orijinal_satis_birbirini_goturur()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 10_000m, status: SaleStatus.Reversed, day: 3),
            TestData.Sale(2, -10_000m, status: SaleStatus.Refund, day: 5, reversedSaleId: 1)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(), sales, [TestData.PercentageRule(0.06m)]);

        // Ne satis ne iade tabana girer; tutar iki kez dusulmez.
        result.TotalSalesBase.Should().Be(0m);
        result.TotalCommission.Should().Be(0m);
        result.ExcludedSales.Should().HaveCount(2);
    }

    [Fact]
    public void Orijinali_onceki_doneme_ait_iade_cari_aydan_dusulur()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 20_000m, day: 4),
            // Orijinali bu donemde olmayan iade: mahsup cari aya yazilir.
            TestData.Sale(2, -5_000m, status: SaleStatus.Refund, day: 10, reversedSaleId: 999)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(), sales, [TestData.PercentageRule(0.06m)]);

        result.TotalSalesBase.Should().Be(15_000m);
        result.TotalCommission.Should().Be(900m);
    }

    [Fact]
    public void Eslesmemis_iade_de_prim_tabanindan_dusulur()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 8_000m, day: 2),
            TestData.Sale(2, -3_000m, status: SaleStatus.Refund, day: 6)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(), sales, [TestData.PercentageRule(0.10m)]);

        result.TotalSalesBase.Should().Be(5_000m);
        result.TotalCommission.Should().Be(500m);
    }

    [Fact]
    public void Iade_kademeli_baremde_personeli_alt_kademeye_indirebilir()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 62_000m, "ALC", day: 4),
            TestData.Sale(2, -5_000m, "ALC", status: SaleStatus.Refund, day: 20)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee("F&B"), sales, [TestData.TieredRule()]);

        // Net ciro 57.000 -> 3. kademe (%7) degil 2. kademe (%5)
        result.TotalCommission.Should().Be(2_850m);
        result.Lines[0].Step.AppliedRate.Should().Be(0.05m);
    }

    [Fact]
    public void Sabit_tutarli_kuralda_iade_primi_geri_alir()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 1_000m, day: 2),
            TestData.Sale(2, -1_000m, status: SaleStatus.Refund, day: 7)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(), sales, [TestData.FixedAmountRule(50m)]);

        // 50 TL kazanildi, 50 TL geri alindi
        result.TotalCommission.Should().Be(0m);
    }

    [Fact]
    public void Iadeler_satislari_asarsa_prim_negatife_doner()
    {
        var sales = new List<SaleRecord>
        {
            TestData.Sale(1, 2_000m, day: 2),
            TestData.Sale(2, -6_000m, status: SaleStatus.Refund, day: 9)
        };

        var result = TestData.Calculator().Calculate(
            TestData.Employee(), sales, [TestData.PercentageRule(0.05m)]);

        // Mahsup gizlenmez; eksi bakiye bir sonraki doneme tasinabilsin diye gorunur kalir.
        result.TotalSalesBase.Should().Be(-4_000m);
        result.TotalCommission.Should().Be(-200m);
    }
}
