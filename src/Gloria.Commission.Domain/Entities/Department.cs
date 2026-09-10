using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Domain.Entities;

public class Department
{
    public Guid Id { get; set; } = SequentialGuid.New();

    /// <summary>Kaynak sistemlerdeki departman adi (SPA, F&amp;B, ÖN BÜRO, GUEST RELATIONS).</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
