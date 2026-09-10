using Gloria.Commission.Application.Rules;
using Gloria.Commission.Application.Rules.Strategies;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.UnitTests;

/// <summary>Testlerin ortak kurulum yardimcilari.</summary>
internal static class TestData
{
    public static readonly DateOnly Ruleset = new(2026, 1, 1);

    public static CommissionCalculator Calculator() => new(
    [
        new PercentageRuleStrategy(),
        new TieredRuleStrategy(),
        new FixedAmountRuleStrategy()
    ]);

    public static Employee Employee(
        string departmentCode = "SPA",
        DateOnly? hireDate = null,
        DateOnly? terminationDate = null) => new()
    {
        Id = 1,
        EmployeeNo = "P1001",
        FullName = "Test Personel",
        Hotel = "GSR",
        HireDate = hireDate ?? new DateOnly(2024, 1, 1),
        TerminationDate = terminationDate,
        Department = new Department { Id = 1, Code = departmentCode, Name = departmentCode }
    };

    public static SaleRecord Sale(
        long id,
        decimal amount,
        string productGroup = "SPA",
        string productCode = "SPA_MSJ60",
        SaleStatus status = SaleStatus.Normal,
        int day = 5,
        int quantity = 1,
        long? reversedSaleId = null,
        SourceSystem source = SourceSystem.Pms) => new()
    {
        Id = id,
        SourceSystem = source,
        SourceDocumentNo = $"DOC{id}",
        SourceHash = $"hash{id}",
        TransactionDate = new DateOnly(2026, 8, day),
        EmployeeNo = "P1001",
        EmployeeId = 1,
        ProductCode = productCode,
        ProductName = productCode,
        ProductGroup = productGroup,
        Quantity = quantity,
        Amount = amount,
        AmountTry = amount,
        Currency = "TRY",
        ExchangeRate = 1m,
        Status = status,
        ReversedSaleId = reversedSaleId
    };

    public static CommissionRule PercentageRule(decimal rate, string productGroup = "SPA") => new()
    {
        Id = 1,
        Code = "PCT",
        Name = "Sabit yuzde",
        RuleType = CommissionRuleType.Percentage,
        ProductGroup = productGroup,
        Rate = rate,
        EffectiveFrom = Ruleset,
        IsActive = true
    };

    public static CommissionRule FixedAmountRule(
        decimal amount, bool multiplyByQuantity = false, string productGroup = "SPA") => new()
    {
        Id = 2,
        Code = "FIX",
        Name = "Sabit tutar",
        RuleType = CommissionRuleType.FixedAmount,
        ProductGroup = productGroup,
        FixedAmount = amount,
        MultiplyByQuantity = multiplyByQuantity,
        EffectiveFrom = Ruleset,
        IsActive = true
    };

    /// <summary>0-30k %3, 30k-60k %5, 60k+ %7 baremi.</summary>
    public static CommissionRule TieredRule(
        TierApplication application = TierApplication.WholeAmount,
        string productGroup = "ALC") => new()
    {
        Id = 3,
        Code = "TIER",
        Name = "Kademeli barem",
        RuleType = CommissionRuleType.Tiered,
        ProductGroup = productGroup,
        TierApplication = application,
        EffectiveFrom = Ruleset,
        IsActive = true,
        Tiers =
        [
            new CommissionRuleTier { Id = 1, MinAmount = 0m,     MaxAmount = 30000m, Rate = 0.03m },
            new CommissionRuleTier { Id = 2, MinAmount = 30000m, MaxAmount = 60000m, Rate = 0.05m },
            new CommissionRuleTier { Id = 3, MinAmount = 60000m, MaxAmount = null,   Rate = 0.07m }
        ]
    };
}
