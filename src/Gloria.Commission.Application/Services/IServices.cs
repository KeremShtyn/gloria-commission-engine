using Gloria.Commission.Application.Dtos.Requests;
using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Services;

public interface IRuleService
{
    Task<IReadOnlyList<CommissionRuleResponse>> GetAllAsync(CancellationToken ct = default);
    Task<CommissionRuleResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CommissionRuleResponse> CreateAsync(CommissionRuleRequest request, CancellationToken ct = default);
    Task<CommissionRuleResponse> UpdateAsync(int id, CommissionRuleRequest request, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public interface ICommissionService
{
    Task<CommissionResultResponse> CalculateAsync(
        int year, int month, string employeeNo, CancellationToken ct = default);

    Task<PeriodSummaryResponse> CalculatePeriodAsync(int year, int month, CancellationToken ct = default);
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
    Task<IReadOnlyList<ImportErrorResponse>> GetErrorsAsync(int batchId, CancellationToken ct = default);
    Task<IReadOnlyList<StagingRowResponse>> GetStagingRowsAsync(int batchId, CancellationToken ct = default);
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
    Task<IReadOnlyList<string>> GetProductGroupsAsync(CancellationToken ct = default);
}
