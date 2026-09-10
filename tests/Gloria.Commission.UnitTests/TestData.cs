using Gloria.Commission.Application.Rules;
using Gloria.Commission.Application.Rules.Strategies;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.UnitTests;

/// <summary>Testlerin ortak kurulum yardimcilari.</summary>
internal static class TestData
{
    public static readonly DateOnly Ruleset = new(2026, 1, 1);

    // Referans veri testler boyunca sabit; kimlikler kural ile satis arasinda eslesmeli.
    public static readonly Guid SpaDepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid FoodDepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid GsrHotelId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid GgrHotelId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid SpaGroupId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid AlcGroupId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid GolfGroupId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static CommissionCalculator Calculator() => new(
    [
        new PercentageRuleStrategy(),
        new TieredRuleStrategy(),
        new FixedAmountRuleStrategy()
    ]);

    public static Employee Employee(
        Guid? departmentId = null,
        Guid? hotelId = null,
        DateOnly? hireDate = null,
        DateOnly? terminationDate = null) => new()
    {
        Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
        EmployeeNo = "P1001",
        FullName = "Test Personel",
        DepartmentId = departmentId ?? SpaDepartmentId,
        Department = new Department { Id = departmentId ?? SpaDepartmentId, Code = "SPA", Name = "SPA" },
        HotelId = hotelId ?? GsrHotelId,
        Hotel = new Hotel { Id = hotelId ?? GsrHotelId, Code = "GSR", Name = "Gloria Serenity Resort" },
        HireDate = hireDate ?? new DateOnly(2024, 1, 1),
        TerminationDate = terminationDate
    };

    public static SaleRecord Sale(
        int seed,
        decimal amount,
        Guid? productGroupId = null,
        string productCode = "SPA_MSJ60",
        SaleStatus status = SaleStatus.Normal,
        int day = 5,
        int quantity = 1,
        Guid? reversedSaleId = null,
        SourceSystem source = SourceSystem.Pms) => new()
    {
        Id = Guid.Parse($"00000000-0000-0000-0000-{seed:D12}"),
        SourceSystem = source,
        SourceDocumentNo = $"DOC{seed}",
        SourceHash = $"hash{seed}",
        TransactionDate = new DateOnly(2026, 8, day),
        SourceEmployeeNo = "P1001",
        EmployeeId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
        ProductCode = productCode,
        ProductName = productCode,
        ProductGroupId = productGroupId ?? SpaGroupId,
        Quantity = quantity,
        Amount = amount,
        AmountTry = amount,
        Currency = "TRY",
        ExchangeRate = 1m,
        Status = status,
        ReversedSaleId = reversedSaleId
    };

    public static CommissionRule PercentageRule(decimal rate, Guid? productGroupId = null) => new()
    {
        Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
        Code = "PCT",
        Name = "Sabit yuzde",
        RuleType = CommissionRuleType.Percentage,
        ProductGroupId = productGroupId ?? SpaGroupId,
        Rate = rate,
        EffectiveFrom = Ruleset,
        IsActive = true
    };

    public static CommissionRule FixedAmountRule(
        decimal amount, bool multiplyByQuantity = false, Guid? productGroupId = null) => new()
    {
        Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
        Code = "FIX",
        Name = "Sabit tutar",
        RuleType = CommissionRuleType.FixedAmount,
        ProductGroupId = productGroupId ?? SpaGroupId,
        FixedAmount = amount,
        MultiplyByQuantity = multiplyByQuantity,
        EffectiveFrom = Ruleset,
        IsActive = true
    };

    /// <summary>0-30k %3, 30k-60k %5, 60k+ %7 baremi.</summary>
    public static CommissionRule TieredRule(
        TierApplication application = TierApplication.WholeAmount,
        Guid? productGroupId = null) => new()
    {
        Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"),
        Code = "TIER",
        Name = "Kademeli barem",
        RuleType = CommissionRuleType.Tiered,
        ProductGroupId = productGroupId ?? AlcGroupId,
        TierApplication = application,
        EffectiveFrom = Ruleset,
        IsActive = true,
        Tiers =
        [
            new CommissionRuleTier { MinAmount = 0m,     MaxAmount = 30000m, Rate = 0.03m },
            new CommissionRuleTier { MinAmount = 30000m, MaxAmount = 60000m, Rate = 0.05m },
            new CommissionRuleTier { MinAmount = 60000m, MaxAmount = null,   Rate = 0.07m }
        ]
    };
}
