using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Import;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Persistence;

/// <summary>
/// Ilk calistirmada referans veriyi ve ornek kural setini yukler.
/// Kurallar burada "ornek" olarak yaratilir; uygulama calisirken arayuzden degistirilebilir.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        CommissionDbContext db, string? personnelCsvPath, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedProductGroupsAsync(db, ct);
        await SeedHotelsAsync(db, ct);
        await SeedDepartmentsAndEmployeesAsync(db, personnelCsvPath, ct);
        await SeedRulesAsync(db, ct);
    }

    /// <summary>
    /// Urun gruplari referans veridir; kurallar yabanci anahtarla buraya baglanir.
    /// Katalogdaki tanimlar tek kaynak: aktarim da bu kodlari uretiyor.
    /// </summary>
    private static async Task SeedProductGroupsAsync(CommissionDbContext db, CancellationToken ct)
    {
        if (await db.ProductGroups.AnyAsync(ct)) return;

        db.ProductGroups.AddRange(ProductCatalog.AllGroups
            .Select(g => new ProductGroup { Code = g.Code, Name = g.Name }));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedHotelsAsync(CommissionDbContext db, CancellationToken ct)
    {
        if (await db.Hotels.AnyAsync(ct)) return;

        db.Hotels.AddRange(
            new Hotel { Code = "GSR", Name = "Gloria Serenity Resort" },
            new Hotel { Code = "GGR", Name = "Gloria Golf Resort" },
            new Hotel { Code = "GVR", Name = "Gloria Verde Resort" });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDepartmentsAndEmployeesAsync(
        CommissionDbContext db, string? personnelCsvPath, CancellationToken ct)
    {
        if (await db.Employees.AnyAsync(ct)) return;
        if (personnelCsvPath is null || !File.Exists(personnelCsvPath)) return;

        var hotels = await db.Hotels.ToDictionaryAsync(h => h.Code, h => h.Id, ct);

        var content = await File.ReadAllTextAsync(personnelCsvPath, ct);
        var departments = new Dictionary<string, Department>(StringComparer.OrdinalIgnoreCase);
        var employees = new List<Employee>();

        foreach (var (_, _, cells) in CsvReaderHelper.ReadRows(content))
        {
            var employeeNo = CsvReaderHelper.Cell(cells, 0);
            var fullName = CsvReaderHelper.Cell(cells, 1);
            var departmentCode = CsvReaderHelper.Cell(cells, 2);
            var hotelCode = CsvReaderHelper.Cell(cells, 3);

            if (employeeNo is null || fullName is null || departmentCode is null) continue;
            if (hotelCode is null || !hotels.TryGetValue(hotelCode, out var hotelId)) continue;
            if (!CsvValueParser.TryParseDate(CsvReaderHelper.Cell(cells, 4), out var hireDate)) continue;

            if (!departments.TryGetValue(departmentCode, out var department))
            {
                department = new Department { Code = departmentCode, Name = departmentCode };
                departments[departmentCode] = department;
                db.Departments.Add(department);
            }

            DateOnly? terminationDate = CsvValueParser.TryParseDate(CsvReaderHelper.Cell(cells, 5), out var end)
                ? end
                : null;

            employees.Add(new Employee
            {
                EmployeeNo = employeeNo,
                FullName = fullName,
                Department = department,
                HotelId = hotelId,
                HireDate = hireDate,
                TerminationDate = terminationDate
            });
        }

        db.Employees.AddRange(employees);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Baslangic kural seti. Golf (GLF_LSN) icin bilerek kural tanimlanmaz:
    /// "kod yazmadan yeni kalem ekleme" senaryosu arayuzden gosterilebilsin diye.
    /// </summary>
    private static async Task SeedRulesAsync(CommissionDbContext db, CancellationToken ct)
    {
        if (await db.CommissionRules.AnyAsync(ct)) return;

        var groups = await db.ProductGroups.ToDictionaryAsync(g => g.Code, g => g.Id, ct);
        var from = new DateOnly(2026, 1, 1);

        var rules = new List<CommissionRule>
        {
            new()
            {
                Code = "SPA-PCT",
                Name = "SPA hizmet satisi - sabit yuzde",
                RuleType = CommissionRuleType.Percentage,
                SourceSystem = SourceSystem.Pms,
                ProductGroupId = groups["SPA"],
                Rate = 0.06m,
                Priority = 10,
                EffectiveFrom = from
            },
            new()
            {
                Code = "ALC-TIER",
                Name = "A la Carte rezervasyon - kademeli barem",
                RuleType = CommissionRuleType.Tiered,
                SourceSystem = SourceSystem.Pms,
                ProductGroupId = groups["ALC"],
                TierApplication = TierApplication.WholeAmount,
                Priority = 10,
                EffectiveFrom = from,
                Tiers =
                [
                    new CommissionRuleTier { MinAmount = 0m,     MaxAmount = 30000m, Rate = 0.03m },
                    new CommissionRuleTier { MinAmount = 30000m, MaxAmount = 60000m, Rate = 0.05m },
                    new CommissionRuleTier { MinAmount = 60000m, MaxAmount = null,   Rate = 0.07m }
                ]
            },
            new()
            {
                Code = "PAV-PCT",
                Name = "Pavillon kullanimi - sabit yuzde",
                RuleType = CommissionRuleType.Percentage,
                SourceSystem = SourceSystem.Pms,
                ProductGroupId = groups["PAVILLON"],
                Rate = 0.04m,
                Priority = 10,
                EffectiveFrom = from
            },
            new()
            {
                Code = "BUGGY-FIX",
                Name = "Buggy kiralama - islem basina sabit tutar",
                RuleType = CommissionRuleType.FixedAmount,
                SourceSystem = SourceSystem.Pms,
                ProductGroupId = groups["BUGGY"],
                FixedAmount = 75m,
                MultiplyByQuantity = false,
                Priority = 10,
                EffectiveFrom = from
            },
            new()
            {
                Code = "POS-SPA-FIX",
                Name = "SPA urun satisi (POS) - adet basina sabit tutar",
                RuleType = CommissionRuleType.FixedAmount,
                SourceSystem = SourceSystem.Pos,
                ProductGroupId = groups["SPA_RETAIL"],
                FixedAmount = 50m,
                MultiplyByQuantity = true,
                Priority = 10,
                EffectiveFrom = from
            },
            new()
            {
                Code = "POS-FB-PCT",
                Name = "Restoran ve bar urun satisi (POS) - sabit yuzde",
                RuleType = CommissionRuleType.Percentage,
                SourceSystem = SourceSystem.Pos,
                Rate = 0.02m,
                Priority = 5,
                EffectiveFrom = from
            }
        };

        db.CommissionRules.AddRange(rules);
        await db.SaveChangesAsync(ct);
    }
}
