using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Domain.Entities;

/// <summary>
/// Üç kaynak sistemden gelen satış kayıtlarının ortak modeli.
/// </summary>
public class SaleRecord
{
    public long Id { get; set; }

    public SourceSystem SourceSystem { get; set; }

    /// <summary>Kaynak sistemdeki belge numarası (PMS BelgeNo, POS FisNo, ERP DocNumber).</summary>
    public string SourceDocumentNo { get; set; } = null!;

    /// <summary>
    /// Mükerrer kayıt engelleme anahtarı: kaynak sistem + belge no + satır içeriğinin hash'i.
    /// Aynı dosya iki kez yüklense de tekrar eklenmez.
    /// </summary>
    public string SourceHash { get; set; } = null!;

    public DateOnly TransactionDate { get; set; }

    public string EmployeeNo { get; set; } = null!;
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductGroup { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>İşlem para birimindeki tutar (kaynak sistemdeki ham değer).</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    /// <summary>TRY karşılığı. Prim hesabı daima bu alan üzerinden yapılır.</summary>
    public decimal AmountTry { get; set; }

    /// <summary>Tutarın TRY'ye çevrilmesinde kullanılan kur (TRY satırlarında 1).</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    public SaleStatus Status { get; set; } = SaleStatus.Normal;

    /// <summary>
    /// Kaynak sistemin verdiği referans (ERP RM satırlarında iptal edilen DocNumber).
    /// İçeriği kaynak sisteme göre değişir, güvenilir bir join anahtarı değildir; eşleştirmede ipucu olarak kullanılır.
    /// </summary>
    public string? SourceReference { get; set; }

    /// <summary>Bu kayıt bir iade ise, iptal ettiği satışın Id'si (eşleştirilebildiyse).</summary>
    public long? ReversedSaleId { get; set; }
    public SaleRecord? ReversedSale { get; set; }

    public string? Hotel { get; set; }
    public string? Outlet { get; set; }
    public string? RoomNo { get; set; }

    /// <summary>Prime esas mı? İade kayıtları da esastır (negatif tutarla düşülür).</summary>
    public bool IsCommissionable => Status is SaleStatus.Normal or SaleStatus.Refund;

    public int ImportBatchId { get; set; }
    public ImportBatch ImportBatch { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
