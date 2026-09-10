using Gloria.Commission.Application.Dtos.Responses;
using Gloria.Commission.Domain.Entities;

namespace Gloria.Commission.Application.Mappers;

public static class PeriodMapper
{
    public static PeriodResponse ToResponse(Period period) => new()
    {
        Key = period.ToString(),
        Year = period.Year,
        Month = period.Month,
        Status = period.Status.ToString(),
        ClosedAtUtc = period.ClosedAtUtc,
        ClosedBy = period.ClosedBy
    };
}

public static class AuditLogMapper
{
    public static AuditLogResponse ToResponse(AuditLog log) => new()
    {
        Id = log.Id,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        Action = log.Action.ToString(),
        OldValues = log.OldValues,
        NewValues = log.NewValues,
        ChangedBy = log.ChangedBy,
        ChangedByRole = log.ChangedByRole,
        ChangedAtUtc = log.ChangedAtUtc
    };
}

public static class EmployeeMapper
{
    public static EmployeeResponse ToResponse(Employee employee) => new()
    {
        EmployeeNo = employee.EmployeeNo,
        FullName = employee.FullName,
        Department = employee.Department.Code,
        Hotel = employee.Hotel,
        HireDate = employee.HireDate,
        TerminationDate = employee.TerminationDate
    };
}

public static class DepartmentMapper
{
    public static DepartmentResponse ToResponse(Department department) => new()
    {
        Code = department.Code,
        Name = department.Name
    };
}

public static class ImportMapper
{
    public static ImportBatchResponse ToResponse(ImportBatch batch) => new()
    {
        Id = batch.Id,
        SourceSystem = batch.SourceSystem.ToString(),
        FileName = batch.FileName,
        TotalRows = batch.TotalRows,
        ImportedRows = batch.ImportedRows,
        DuplicateRows = batch.DuplicateRows,
        FailedRows = batch.FailedRows,
        ImportedBy = batch.ImportedBy,
        StartedAtUtc = batch.StartedAtUtc,
        CompletedAtUtc = batch.CompletedAtUtc
    };

    public static StagingRowResponse ToResponse(StagingRow row) => new()
    {
        RowNumber = row.RowNumber,
        SourceSystem = row.SourceSystem.ToString(),
        Status = row.Status.ToString(),
        RawLine = row.RawLine,
        SaleRecordId = row.SaleRecordId,
        ReceivedAtUtc = row.ReceivedAtUtc,
        ProcessedAtUtc = row.ProcessedAtUtc
    };

    public static ImportErrorResponse ToResponse(ImportError error) => new()
    {
        RowNumber = error.RowNumber,
        ErrorCode = error.ErrorCode,
        ErrorMessage = error.ErrorMessage,
        RawLine = error.RawLine
    };
}
