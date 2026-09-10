namespace Gloria.Commission.Domain.Entities;

public class Department
{
    public int Id { get; set; }

    /// <summary>Kaynak sistemlerdeki departman adı (SPA, F&amp;B, ÖN BÜRO, GUEST RELATIONS).</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
