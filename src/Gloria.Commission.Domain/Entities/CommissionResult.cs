using Gloria.Commission.Domain.Common;
namespace Gloria.Commission.Domain.Entities;

/// <summary>Bir personelin bir dönemdeki prim hesabının başlığı.</summary>
public class CommissionResult
{
    public Guid Id { get; set; } = SequentialGuid.New();

    public Guid PeriodId { get; set; }
    public Period Period { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Prime esas net ciro (iadeler düşülmüş).</summary>
    public decimal TotalSalesBase { get; set; }

    public decimal TotalCommission { get; set; }

    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CalculatedBy { get; set; } = "system";

    public ICollection<CommissionResultLine> Lines { get; set; } = new List<CommissionResultLine>();
}
