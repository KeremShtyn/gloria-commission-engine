namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// İçe aktarılamayan ham satır. Satır atılmaz, burada saklanır ki
/// düzeltilip yeniden yüklenebilsin ve mutabakat açığı görünür kalsın.
/// </summary>
public class ImportError
{
    public long Id { get; set; }

    public int ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    /// <summary>CSV'deki fiziksel satır numarası (başlık = 1).</summary>
    public int RowNumber { get; set; }

    /// <summary>Ham satırın kendisi — kaynağa dönmeden hata ayıklanabilsin diye.</summary>
    public string RawLine { get; set; } = null!;

    /// <summary>Makine tarafından okunabilir hata kodu: INVALID_DATE, UNKNOWN_EMPLOYEE, ...</summary>
    public string ErrorCode { get; set; } = null!;

    public string ErrorMessage { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
