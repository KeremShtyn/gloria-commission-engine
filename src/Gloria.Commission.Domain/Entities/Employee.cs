using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; } = SequentialGuid.New();

    /// <summary>Personel numarasi (P1001). Kaynak sistemlerin tamaminda bu alan kullanilir.</summary>
    public string EmployeeNo { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    /// <summary>Ayrilis tarihi. Null ise personel aktiftir.</summary>
    public DateOnly? TerminationDate { get; set; }

    public ICollection<SaleRecord> Sales { get; set; } = new List<SaleRecord>();

    /// <summary>Satis tarihinde personel istihdamda miydi?</summary>
    public bool IsEmployedOn(DateOnly date) =>
        date >= HireDate && (TerminationDate is null || date <= TerminationDate);
}
