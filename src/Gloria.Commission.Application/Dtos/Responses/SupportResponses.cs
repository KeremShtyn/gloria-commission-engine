namespace Gloria.Commission.Application.Dtos.Responses;

public sealed record PeriodResponse
{
    public string Key { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ClosedAtUtc { get; init; }
    public string? ClosedBy { get; init; }
}

public sealed record AuditLogResponse
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public string ChangedByRole { get; init; } = string.Empty;
    public DateTime ChangedAtUtc { get; init; }
}

public sealed record EmployeeResponse
{
    public Guid Id { get; init; }
    public string EmployeeNo { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Hotel { get; init; } = string.Empty;
    public DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
}

public sealed record DepartmentResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record HotelResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record ProductGroupResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record ImportBatchResponse
{
    public Guid Id { get; init; }
    public string SourceSystem { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int ImportedRows { get; init; }
    public int DuplicateRows { get; init; }
    public int FailedRows { get; init; }
    public string ImportedBy { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
}

/// <summary>Ham satirin donusum uygulanmadan onceki hali.</summary>
public sealed record StagingRowResponse
{
    public int RowNumber { get; init; }
    public string SourceSystem { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RawLine { get; init; } = string.Empty;
    public Guid? SaleRecordId { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public DateTime? ProcessedAtUtc { get; init; }
}

public sealed record ImportErrorResponse
{
    public int RowNumber { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string RawLine { get; init; } = string.Empty;
}

/// <summary>Operasyonel ciro (PMS+POS) ile muhasebelesmis ciroyu (ERP) karsilastirir.</summary>
public sealed record ReconciliationResponse
{
    public string Period { get; init; } = string.Empty;
    public IReadOnlyList<ReconciliationGroupResponse> Groups { get; init; } = [];

    public int UnpostedCount { get; init; }
    public decimal UnpostedAmount { get; init; }

    /// <summary>Orijinal satisi bulunamayan iade sayisi. Tutar yine dusulur, baglanti kurulamamistir.</summary>
    public int UnmatchedRefundCount { get; init; }

    public IReadOnlyList<ImportErrorCountResponse> ImportErrors { get; init; } = [];
}

public sealed record ReconciliationGroupResponse
{
    public string ProductGroup { get; init; } = string.Empty;
    public decimal OperationalRevenue { get; init; }
    public decimal AccountedRevenue { get; init; }
    public decimal Difference { get; init; }
}

public sealed record ImportErrorCountResponse
{
    public string ErrorCode { get; init; } = string.Empty;
    public int Count { get; init; }
}

/// <summary>Sayfali liste cevabi. Bicim API standardiyla ayni.</summary>
public sealed record PagedResponse<T>
{
    public IReadOnlyList<T> Content { get; init; } = [];
    public int Page { get; init; }
    public int Size { get; init; }
    public int TotalElements { get; init; }
    public int TotalPages { get; init; }
}
