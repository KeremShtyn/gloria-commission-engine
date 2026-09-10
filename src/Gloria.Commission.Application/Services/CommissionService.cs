using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Mappers;
using Gloria.Commission.Application.Repositories;
using Gloria.Commission.Application.Rules;
using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

/// <summary>
/// Kural motorunu veriye baglar: girdileri repository'lerden toplar, hesabi
/// <see cref="ICommissionCalculator"/> yaptirir, sonucu adimlariyla kalici hale getirir.
/// </summary>
public sealed class CommissionService : ICommissionService
{
    private readonly IEmployeeRepository _employees;
    private readonly ISaleRecordRepository _sales;
    private readonly ICommissionRuleRepository _rules;
    private readonly ICommissionResultRepository _results;
    private readonly IPeriodRepository _periods;
    private readonly ICommissionCalculator _calculator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CommissionService(
        IEmployeeRepository employees,
        ISaleRecordRepository sales,
        ICommissionRuleRepository rules,
        ICommissionResultRepository results,
        IPeriodRepository periods,
        ICommissionCalculator calculator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _employees = employees;
        _sales = sales;
        _rules = rules;
        _results = results;
        _periods = periods;
        _calculator = calculator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CommissionResultResponse> CalculateAsync(
        int year, int month, string employeeNo, CancellationToken ct = default)
    {
        Authorize(employeeNo);

        var employee = await _employees.FindByEmployeeNoAsync(employeeNo, ct)
                       ?? throw new DomainException("EMPLOYEE_NOT_FOUND",
                           $"'{employeeNo}' numarali personel yok.");

        var period = await GetOrCreatePeriodAsync(year, month, ct);
        var (from, to) = PeriodBounds(year, month);

        var sales = await _sales.FindByEmployeeAndPeriodAsync(employee.Id, from, to, ct);
        var rules = await _rules.FindEffectiveAsync(from, to, ct);

        var calculation = _calculator.Calculate(employee, sales, rules);

        // Kapali donemde hesap yeniden yazilmaz; mevcut sonuc oldugu gibi okunur.
        if (!period.IsClosed)
        {
            await PersistAsync(period, employee, calculation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return CommissionResultMapper.ToResponse(period, employee, calculation, sales);
    }

    public async Task<PeriodSummaryResponse> CalculatePeriodAsync(
        int year, int month, CancellationToken ct = default)
    {
        if (!_currentUser.CanSeeAllEmployees)
            throw new DomainException("FORBIDDEN", "Bu islem icin Admin veya Muhasebe rolu gerekir.");

        var period = await GetOrCreatePeriodAsync(year, month, ct);
        var (from, to) = PeriodBounds(year, month);

        var employees = await _employees.FindAllAsync(ct);
        var rules = await _rules.FindEffectiveAsync(from, to, ct);
        var sales = await _sales.FindByPeriodAsync(from, to, ct);

        var salesByEmployee = sales
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleRecord>)g.ToList());

        var rows = new List<EmployeeCommissionResponse>();

        foreach (var employee in employees)
        {
            var employeeSales = salesByEmployee.TryGetValue(employee.Id, out var list)
                ? list
                : Array.Empty<SaleRecord>();

            var calculation = _calculator.Calculate(employee, employeeSales, rules);

            if (!period.IsClosed)
                await PersistAsync(period, employee, calculation, ct);

            rows.Add(new EmployeeCommissionResponse
            {
                EmployeeId = employee.Id,
                EmployeeNo = employee.EmployeeNo,
                FullName = employee.FullName,
                Department = employee.Department.Code,
                Hotel = employee.Hotel.Code,
                TotalSalesBase = calculation.TotalSalesBase,
                TotalCommission = calculation.TotalCommission
            });
        }

        if (!period.IsClosed) await _unitOfWork.SaveChangesAsync(ct);

        return new PeriodSummaryResponse
        {
            Period = Period.Key(year, month),
            Closed = period.IsClosed,
            EmployeeCount = rows.Count(r => r.TotalCommission != 0m || r.TotalSalesBase != 0m),
            TotalSalesBase = rows.Sum(r => r.TotalSalesBase),
            TotalCommission = rows.Sum(r => r.TotalCommission),
            Employees = rows
        };
    }

    /// <summary>Personel rolu yalnizca kendi primini goruntuleyebilir.</summary>
    private void Authorize(string employeeNo)
    {
        if (_currentUser.CanSeeAllEmployees) return;

        if (!string.Equals(_currentUser.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("FORBIDDEN", "Personel yalnizca kendi primini goruntuleyebilir.");
    }

    private async Task<Period> GetOrCreatePeriodAsync(int year, int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");

        var period = await _periods.FindAsync(year, month, ct);
        if (period is not null) return period;

        period = new Period { Year = year, Month = month, Status = PeriodStatus.Open };
        _periods.Add(period);
        await _unitOfWork.SaveChangesAsync(ct);

        return period;
    }

    private async Task PersistAsync(
        Period period, Employee employee, CommissionCalculation calculation, CancellationToken ct)
    {
        var existing = await _results.FindWithLinesAsync(period.Id, employee.Id, ct);

        if (existing is not null)
        {
            _results.RemoveLines(existing.Lines);
            _results.Remove(existing);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        _results.Add(CommissionResultMapper.ToEntity(
            period.Id, employee.Id, calculation, _currentUser.UserId));
    }

    private static (DateOnly From, DateOnly To) PeriodBounds(int year, int month)
        => (new DateOnly(year, month, 1),
            new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
}
