using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>Tek bir CSV yükleme işleminin künyesi.</summary>
public class ImportBatch
{
    public int Id { get; set; }

    public SourceSystem SourceSystem { get; set; }

    public string FileName { get; set; } = null!;

    /// <summary>Dosya içeriğinin SHA-256'sı. Aynı dosyanın ikinci kez yüklenmesi tespit edilir.</summary>
    public string FileHash { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int DuplicateRows { get; set; }
    public int FailedRows { get; set; }

    public string ImportedBy { get; set; } = "system";

    public ICollection<ImportError> Errors { get; set; } = new List<ImportError>();
}
