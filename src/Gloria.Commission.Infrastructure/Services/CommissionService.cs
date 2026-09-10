using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Models;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;
using Gloria.Commission.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gloria.Commission.Infrastructure.Services;

public interface ICommissionService
{
    Task<CommissionResultDto> CalculateAsync(int year, int month, string employeeNo, CancellationToken ct = default);
    Task<PeriodSummaryDto> CalculatePeriodAsync(int year, int month, CancellationToken ct = default);
}

/// <summary>
/// Kural motorunu veritabanina baglar: veriyi toplar, hesabi <see cref="ICommissionCalculator"/>
/// uzerinden yaptirir, sonucu adimlariyla birlikte kalici hale getirir.
/// </summary>
public sealed class CommissionService : ICommissionService
{
    private readonly CommissionDbContext _db;
    private readonly ICommissionCalculator _calculator;
    private readonly ICurrentUser _currentUser;

    public CommissionService(
        CommissionDbContext db, ICommissionCalculator calculator, ICurrentUser currentUser)
    {
        _db = db;
        _calculator = calculator;
        _currentUser = currentUser;
    }

    public async Task<CommissionResultDto> CalculateAsync(
        int year, int month, string employeeNo, CancellationToken ct = default)
    {
        Authorize(employeeNo);

        var employee = await _db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeNo == employeeNo, ct)
            ?? throw new DomainException("EMPLOYEE_NOT_FOUND", $"'{employeeNo}' numarali personel yok.");

        var period = await GetOrCreatePeriodAsync(year, month, ct);
        var (from, to) = PeriodBounds(year, month);

        var sales = await _db.SaleRecords
            .Where(s => s.EmployeeNo == employeeNo && s.TransactionDate >= from && s.TransactionDate <= to)
            .OrderBy(s => s.TransactionDate).ThenBy(s => s.SourceDocumentNo)
            .ToListAsync(ct);

        var rules = await ActiveRulesAsync(from, to, ct);

        var calculation = _calculator.Calculate(employee, sales, rules);

        // Kapali donemde hesap yeniden yazilamaz; mevcut sonuc oldugu gibi okunur.
        if (!period.IsClosed)
            await PersistAsync(period, employee, calculation, ct);

        return ToDto(period, employee, calculation, sales);
    }

    public async Task<PeriodSummaryDto> CalculatePeriodAsync(
        int year, int month, CancellationToken ct = default)
    {
        if (!_currentUser.CanSeeAllEmployees)
            throw new DomainException("FORBIDDEN", "Bu islem icin Admin veya Muhasebe rolu gerekir.");

        var period = await GetOrCreatePeriodAsync(year, month, ct);
        var (from, to) = PeriodBounds(year, month);

        var employees = await _db.Employees.Include(e => e.Department).ToListAsync(ct);
        var rules = await ActiveRulesAsync(from, to, ct);

        var sales = await _db.SaleRecords
            .Where(s => s.TransactionDate >= from && s.TransactionDate <= to)
            .ToListAsync(ct);

        var salesByEmployee = sales.GroupBy(s => s.EmployeeNo)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleRecord>)g
                .OrderBy(s => s.TransactionDate).ThenBy(s => s.SourceDocumentNo).ToList());

        var rows = new List<EmployeeCommissionDto>();

        foreach (var employee in employees.OrderBy(e => e.EmployeeNo))
        {
            var employeeSales = salesByEmployee.TryGetValue(employee.EmployeeNo, out var list)
                ? list
                : Array.Empty<SaleRecord>();

            var calculation = _calculator.Calculate(employee, employeeSales, rules);

            if (!period.IsClosed)
                await PersistAsync(period, employee, calculation, ct);

            rows.Add(new EmployeeCommissionDto
            {
                EmployeeNo = employee.EmployeeNo,
                FullName = employee.FullName,
                Department = employee.Department.Code,
                Hotel = employee.Hotel,
                TotalSalesBase = calculation.TotalSalesBase,
                TotalCommission = calculation.TotalCommission
            });
        }

        return new PeriodSummaryDto
        {
            Period = Period.Key(year, month),
            Closed = period.IsClosed,
            EmployeeCount = rows.Count(r => r.TotalCommission != 0m || r.TotalSalesBase != 0m),
            TotalSalesBase = rows.Sum(r => r.TotalSalesBase),
            TotalCommission = rows.Sum(r => r.TotalCommission),
            Employees = rows
        };
    }

    private void Authorize(string employeeNo)
    {
        if (_currentUser.CanSeeAllEmployees) return;

        if (!string.Equals(_currentUser.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("FORBIDDEN", "Personel yalnizca kendi primini goruntuleyebilir.");
    }

    private Task<List<CommissionRule>> ActiveRulesAsync(DateOnly from, DateOnly to, CancellationToken ct)
        => _db.CommissionRules
            .Include(r => r.Tiers)
            .Where(r => r.IsActive && r.EffectiveFrom <= to && (r.EffectiveTo == null || r.EffectiveTo >= from))
            .ToListAsync(ct);

    private async Task<Period> GetOrCreatePeriodAsync(int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");

        var period = await _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);
        if (period is not null) return period;

        period = new Period { Year = year, Month = month, Status = PeriodStatus.Open };
        _db.Periods.Add(period);
        await _db.SaveChangesAsync(ct);
        return period;
    }

    private async Task PersistAsync(
        Period period, Employee employee, CommissionCalculation calculation, CancellationToken ct)
    {
        var existing = await _db.CommissionResults
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.PeriodId == period.Id && r.EmployeeId == employee.Id, ct);

        if (existing is not null)
        {
            _db.CommissionResultLines.RemoveRange(existing.Lines);
            _db.CommissionResults.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }

        var result = new CommissionResult
        {
            PeriodId = period.Id,
            EmployeeId = employee.Id,
            TotalSalesBase = calculation.TotalSalesBase,
            TotalCommission = calculation.TotalCommission,
            CalculatedBy = _currentUser.UserId,
            CalculatedAtUtc = DateTime.UtcNow,
            Lines = calculation.Lines.Select(l => new CommissionResultLine
            {
                SaleRecordId = l.Step.SaleRecordId,
                CommissionRuleId = l.Rule.Id,
                RuleCode = l.Rule.Code,
                RuleType = l.Rule.RuleType.ToString(),
                BaseAmount = l.Step.BaseAmount,
                AppliedRate = l.Step.AppliedRate,
                AppliedFixedAmount = l.Step.AppliedFixedAmount,
                Quantity = l.Step.Quantity,
                CommissionAmount = l.Step.CommissionAmount,
                Explanation = l.Step.Explanation,
                StepOrder = l.StepOrder
            }).ToList()
        };

        _db.CommissionResults.Add(result);
        await _db.SaveChangesAsync(ct);
    }

    private static CommissionResultDto ToDto(
        Period period, Employee employee, CommissionCalculation calculation, IReadOnlyList<SaleRecord> sales)
    {
        var salesById = sales.ToDictionary(s => s.Id);

        SaleRecord? SaleOf(long? id)
            => id is { } saleId && salesById.TryGetValue(saleId, out var sale) ? sale : null;

        return new CommissionResultDto
        {
            Period = period.ToString(),
            PeriodClosed = period.IsClosed,
            EmployeeNo = employee.EmployeeNo,
            FullName = employee.FullName,
            Department = employee.Department.Code,
            Hotel = employee.Hotel,
            TotalSalesBase = calculation.TotalSalesBase,
            TotalCommission = calculation.TotalCommission,
            CalculatedAtUtc = DateTime.UtcNow,
            Steps = calculation.Lines.Select(l => new CommissionStepDto
            {
                Order = l.StepOrder,
                RuleCode = l.Rule.Code,
                RuleName = l.Rule.Name,
                RuleType = l.Rule.RuleType.ToString(),
                SourceSystem = SaleOf(l.Step.SaleRecordId)?.SourceSystem.ToString(),
                SourceDocumentNo = SaleOf(l.Step.SaleRecordId)?.SourceDocumentNo,
                ProductName = SaleOf(l.Step.SaleRecordId)?.ProductName,
                TransactionDate = SaleOf(l.Step.SaleRecordId)?.TransactionDate,
                BaseAmount = l.Step.BaseAmount,
                AppliedRate = l.Step.AppliedRate,
                AppliedFixedAmount = l.Step.AppliedFixedAmount,
                Quantity = l.Step.Quantity,
                CommissionAmount = l.Step.CommissionAmount,
                Explanation = l.Step.Explanation
            }).ToList(),
            ExcludedSales = calculation.ExcludedSales.Select(e => new ExcludedSaleDto
            {
                SourceSystem = e.Sale.SourceSystem.ToString(),
                SourceDocumentNo = e.Sale.SourceDocumentNo,
                TransactionDate = e.Sale.TransactionDate,
                ProductCode = e.Sale.ProductCode,
                ProductName = e.Sale.ProductName,
                AmountTry = e.Sale.AmountTry,
                ReasonCode = e.ReasonCode,
                Reason = e.Reason
            }).ToList()
        };
    }

    private static (DateOnly From, DateOnly To) PeriodBounds(int year, int month)
        => (new DateOnly(year, month, 1),
            new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
}
