using Gloria.Commission.Domain.Common;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Uc kaynak sistemden gelen satis kayitlarinin ortak modeli.
/// </summary>
public class SaleRecord
{
    public Guid Id { get; set; } = SequentialGuid.New();

    public SourceSystem SourceSystem { get; set; }

    /// <summary>Kaynak sistemdeki belge numarasi (PMS BelgeNo, POS FisNo, ERP DocNumber).</summary>
    public string SourceDocumentNo { get; set; } = null!;

    /// <summary>
    /// Mukerrer kayit engelleme anahtari: kaynak sistem + belge numarasinin hash'i.
    /// Ayni dosya iki kez yuklense de satir tekrar eklenmez.
    /// </summary>
    public string SourceHash { get; set; } = null!;

    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Kaynak sistemin bildirdigi personel numarasi. Denetim verisidir:
    /// sorgular <see cref="EmployeeId"/> uzerinden gider, bu alan "kaynak ne dedi" kaydidir.
    /// </summary>
    public string SourceEmployeeNo { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Kaynak sistemdeki urun kodu. Uc sistemde farkli sekillendigi icin serbest metin.</summary>
    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public Guid ProductGroupId { get; set; }
    public ProductGroup ProductGroup { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Islem para birimindeki tutar (kaynak sistemdeki ham deger).</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    /// <summary>TRY karsiligi. Prim hesabi daima bu alan uzerinden yapilir.</summary>
    public decimal AmountTry { get; set; }

    /// <summary>Tutarin TRY'ye cevrilmesinde kullanilan kur (TRY satirlarinda 1).</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    public SaleStatus Status { get; set; } = SaleStatus.Normal;

    /// <summary>Bu kayit bir iade ise, iptal ettigi satisin kimligi (eslestirilebildiyse).</summary>
    public Guid? ReversedSaleId { get; set; }
    public SaleRecord? ReversedSale { get; set; }

    /// <summary>
    /// Kaynak sistemin verdigi referans (ERP RM satirlarinda iptal edilen DocNumber).
    /// Icerigi kaynak sisteme gore degisir, guvenilir bir join anahtari degildir.
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>
    /// Kaynak kaydin bildirdigi otel. Yalnizca PMS dolduruyor; kaynak metadatasidir.
    /// Kural eslestirmesinde kullanilmaz — otel personelin ozelligidir.
    /// </summary>
    public string? SourceHotel { get; set; }

    public string? Outlet { get; set; }
    public string? RoomNo { get; set; }

    /// <summary>Prime esas mi? Iade kayitlari da esastir (negatif tutarla dusulur).</summary>
    public bool IsCommissionable => Status is SaleStatus.Normal or SaleStatus.Refund;

    public Guid ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
