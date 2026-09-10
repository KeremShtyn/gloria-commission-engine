namespace Gloria.Commission.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    /// <summary>Personel numarası (P1001). Kaynak sistemlerin tamamında bu alan kullanılır.</summary>
    public string EmployeeNo { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>Otel kodu: GSR / GGR / GVR.</summary>
    public string Hotel { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    /// <summary>Ayrılış tarihi. Null ise personel aktiftir.</summary>
    public DateOnly? TerminationDate { get; set; }

    public ICollection<SaleRecord> Sales { get; set; } = new List<SaleRecord>();

    /// <summary>Satış tarihinde personel istihdamda mıydı?</summary>
    public bool IsEmployedOn(DateOnly date) =>
        date >= HireDate && (TerminationDate is null || date <= TerminationDate);
}
