using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Kaynak sistemden gelen ham satirin, hicbir donusum uygulanmadan onceki hali.
///
/// Neden var: ayristirici hatali calisirsa (orn. bir kolon yanlis okunursa) satir
/// sessizce yanlis kaydedilir ve karsilastirilacak orijinal kalmaz. Maas etkileyen
/// bir sistemde "kaynak tam olarak bunu gonderdi" diyebilmek gerekir.
///
/// Ayrica yeniden isleme imkani verir: ayristirici duzeltildiginde kaynaga donmeden
/// ayni parti tekrar islenebilir.
/// </summary>
public class StagingRow
{
    public Guid Id { get; set; } = SequentialGuid.New();

    public Guid ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public SourceSystem SourceSystem { get; set; }

    /// <summary>CSV'deki fiziksel satir numarasi (baslik = 1).</summary>
    public int RowNumber { get; set; }

    /// <summary>Satirin ham hali. Uzerinde hicbir kirpma veya donusum yapilmaz.</summary>
    public string RawLine { get; set; } = null!;

    public StagingRowStatus Status { get; set; } = StagingRowStatus.Pending;

    /// <summary>Isleme sonucu olusan satis kaydi; hatali satirlarda null kalir.</summary>
    public Guid? SaleRecordId { get; set; }

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
}
