using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

public interface IRuleService
{
    Task<IReadOnlyList<CommissionRuleResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CommissionRuleResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CommissionRuleResponse> CreateAsync(CommissionRuleRequest request, CancellationToken ct = default);
    Task<CommissionRuleResponse> UpdateAsync(Guid id, CommissionRuleRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}

public interface ICommissionService
{
    /// <summary>Bir personelin donem primini hesaplar ve dondurur. Veri yazmaz.</summary>
    Task<CommissionResultResponse> GetForEmployeeAsync(
        int year, int month, string employeeNo, CancellationToken ct = default);

    /// <summary>Donemin tum personel ozetini hesaplar ve dondurur. Veri yazmaz.</summary>
    Task<PeriodSummaryResponse> GetPeriodSummaryAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Donemi hesaplar ve sonuclari adimlariyla kalici hale getirir.
    /// Okuma uclari yazmaz; kalicilastirma yalnizca bu acik islemle yapilir.
    /// </summary>
    Task<PeriodSummaryResponse> RunPeriodAsync(int year, int month, CancellationToken ct = default);
}

public interface IPeriodService
{
    Task<IReadOnlyList<PeriodResponse>> GetAllAsync(CancellationToken ct = default);
    Task<PeriodResponse> CloseAsync(int year, int month, CancellationToken ct = default);
    Task<PeriodResponse> ReopenAsync(int year, int month, CancellationToken ct = default);
}

public interface IImportService
{
    Task<ImportSummary> ImportAsync(
        SourceSystem source, string fileName, string content, CancellationToken ct = default);

    Task<IReadOnlyList<ImportBatchResponse>> GetBatchesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ImportErrorResponse>> GetErrorsAsync(Guid batchId, CancellationToken ct = default);
    Task<IReadOnlyList<StagingRowResponse>> GetStagingRowsAsync(Guid batchId, CancellationToken ct = default);
}

public interface IReconciliationService
{
    Task<ReconciliationResponse> GetAsync(int year, int month, CancellationToken ct = default);
}

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponse>> SearchAsync(
        string? entityName, string? entityId, int? page, int? size, CancellationToken ct = default);
}

public interface IReferenceService
{
    Task<IReadOnlyList<EmployeeResponse>> GetEmployeesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentResponse>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductGroupResponse>> GetProductGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HotelResponse>> GetHotelsAsync(CancellationToken ct = default);
}
