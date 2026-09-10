using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Otel (GSR, GGR, GVR).
///
/// Otel personelin ozelligidir, satisin degil: kaynak dosyalarda otel bilgisi yalnizca
/// PMS'te var, POS ve ERP'de yok. Bu yuzden kural eslestirmesi personelin oteline bakar.
/// </summary>
public class Hotel
{
    public Guid Id { get; set; } = SequentialGuid.New();

    /// <summary>Kaynak sistemlerdeki kod: GSR, GGR, GVR.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
