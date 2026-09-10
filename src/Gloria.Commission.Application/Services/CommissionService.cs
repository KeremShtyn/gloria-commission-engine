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

    public async Task<CommissionResultResponse> GetForEmployeeAsync(
        int year, int month, string employeeNo, CancellationToken ct = default)
    {
        Authorize(employeeNo);
        RequireValidMonth(month);

        var employee = await _employees.FindByEmployeeNoAsync(employeeNo, ct)
                       ?? throw new DomainException("EMPLOYEE_NOT_FOUND",
                           $"'{employeeNo}' numarali personel yok.");

        var period = await ReadPeriodAsync(year, month, ct);
        var (from, to) = PeriodBounds(year, month);

        var sales = await _sales.FindByEmployeeAndPeriodAsync(employee.Id, from, to, ct);
        var rules = await _rules.FindEffectiveAsync(from, to, ct);

        var calculation = _calculator.Calculate(employee, sales, rules);

        return CommissionResultMapper.ToResponse(period, employee, calculation, sales);
    }

    public async Task<PeriodSummaryResponse> GetPeriodSummaryAsync(
        int year, int month, CancellationToken ct = default)
    {
        RequirePrivileged();
        RequireValidMonth(month);

        var period = await ReadPeriodAsync(year, month, ct);
        var (calculations, rows) = await CalculateAllAsync(year, month, ct);

        _ = calculations;

        return Summarize(year, month, period.IsClosed, rows);
    }

    /// <summary>
    /// Hesabi kalici hale getirir. Okuma uclari veri yazmadigi icin
    /// commission_results yalnizca buradan ve donem kapatmadan guncellenir.
    /// </summary>
    public async Task<PeriodSummaryResponse> RunPeriodAsync(
        int year, int month, CancellationToken ct = default)
    {
        RequirePrivileged();
        RequireValidMonth(month);

        var period = await GetOrCreatePeriodAsync(year, month, ct);

        if (period.IsClosed)
            throw new DomainException("PERIOD_CLOSED",
                $"{period} donemi kapali; hesap yeniden calistirilamaz.");

        var (calculations, rows) = await CalculateAllAsync(year, month, ct);

        // Mevcut sonuclar tek seferde okunur; her personel icin ayri sorgu atilmaz.
        var existing = (await _results.FindByPeriodAsync(period.Id, ct))
            .ToDictionary(result => result.EmployeeId);

        foreach (var (employee, calculation) in calculations)
        {
            var fresh = CommissionResultMapper.ToEntity(
                period.Id, employee.Id, calculation, _currentUser.UserId);

            if (existing.TryGetValue(employee.Id, out var current))
            {
                // Sonuc satiri silinip yeniden eklenmiyor, yerinde guncelleniyor:
                // eszamanli iki istek ayni (donem, personel) satirini eklemeye
                // calissa benzersizlik kisiti ihlal edilirdi.
                _results.RemoveLines(current.Lines.ToList());

                current.TotalSalesBase = calculation.TotalSalesBase;
                current.TotalCommission = calculation.TotalCommission;
                current.CalculatedAtUtc = DateTime.UtcNow;
                current.CalculatedBy = _currentUser.UserId;

                foreach (var line in fresh.Lines) line.CommissionResultId = current.Id;
                _results.AddLines(fresh.Lines);
            }
            else
            {
                _results.Add(fresh);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return Summarize(year, month, period.IsClosed, rows);
    }

    /// <summary>Donemin tum personeli icin hesabi calistirir; veri yazmaz.</summary>
    private async Task<(List<(Employee Employee, CommissionCalculation Calculation)> Calculations,
        List<EmployeeCommissionResponse> Rows)> CalculateAllAsync(
        int year, int month, CancellationToken ct)
    {
        var (from, to) = PeriodBounds(year, month);

        var employees = await _employees.FindAllAsync(ct);
        var rules = await _rules.FindEffectiveAsync(from, to, ct);
        var sales = await _sales.FindByPeriodAsync(from, to, ct);

        var salesByEmployee = sales
            .GroupBy(s => s.EmployeeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleRecord>)g.ToList());

        var calculations = new List<(Employee, CommissionCalculation)>();
        var rows = new List<EmployeeCommissionResponse>();

        foreach (var employee in employees)
        {
            var employeeSales = salesByEmployee.TryGetValue(employee.Id, out var list)
                ? list
                : Array.Empty<SaleRecord>();

            var calculation = _calculator.Calculate(employee, employeeSales, rules);
            calculations.Add((employee, calculation));

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

        return (calculations, rows);
    }

    private static PeriodSummaryResponse Summarize(
        int year, int month, bool closed, List<EmployeeCommissionResponse> rows) => new()
    {
        Period = Period.Key(year, month),
        Closed = closed,
        EmployeeCount = rows.Count(r => r.TotalCommission != 0m || r.TotalSalesBase != 0m),
        TotalSalesBase = rows.Sum(r => r.TotalSalesBase),
        TotalCommission = rows.Sum(r => r.TotalCommission),
        Employees = rows
    };

    /// <summary>Personel rolu yalnizca kendi primini goruntuleyebilir.</summary>
    private void Authorize(string employeeNo)
    {
        if (_currentUser.CanSeeAllEmployees) return;

        if (!string.Equals(_currentUser.EmployeeNo, employeeNo, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("FORBIDDEN", "Personel yalnizca kendi primini goruntuleyebilir.");
    }

    private void RequirePrivileged()
    {
        if (!_currentUser.CanSeeAllEmployees)
            throw new DomainException("FORBIDDEN", "Bu islem icin Admin veya Muhasebe rolu gerekir.");
    }

    private static void RequireValidMonth(int month)
    {
        if (month is < 1 or > 12)
            throw new DomainException("INVALID_PERIOD", $"Ay degeri 1-12 araliginda olmali: {month}");
    }

    /// <summary>
    /// Okuma icin donem. Kayit yoksa yaratilmaz: okuma istegi veri yazmamali.
    /// Kaydi olmayan donem acik sayilir.
    /// </summary>
    private async Task<Period> ReadPeriodAsync(int year, int month, CancellationToken ct)
        => await _periods.FindAsync(year, month, ct)
           ?? new Period { Year = year, Month = month, Status = PeriodStatus.Open };

    private async Task<Period> GetOrCreatePeriodAsync(int year, int month, CancellationToken ct)
    {
        var period = await _periods.FindAsync(year, month, ct);
        if (period is not null) return period;

        period = new Period { Year = year, Month = month, Status = PeriodStatus.Open };
        _periods.Add(period);
        await _unitOfWork.SaveChangesAsync(ct);

        return period;
    }

    private static (DateOnly From, DateOnly To) PeriodBounds(int year, int month)
        => (new DateOnly(year, month, 1),
            new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
}
