using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Application.Import;

/// <summary>
/// Ayristiricinin urettigi satis. Entity degil: ayristirici veritabanina erisemedigi icin
/// personel, urun grubu gibi yabanci anahtarlari cozemez — onlari kodlariyla tasir,
/// kimlige cevirme isini <see cref="Services.ImportService"/> yapar.
/// </summary>
public sealed record ParsedSale
{
    public SourceSystem SourceSystem { get; init; }
    public string SourceDocumentNo { get; init; } = string.Empty;
    public string SourceHash { get; init; } = string.Empty;

    public DateOnly TransactionDate { get; init; }

    public string EmployeeNo { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ProductGroupCode { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public decimal ExchangeRate { get; init; } = 1m;
    public decimal AmountTry { get; init; }

    public SaleStatus Status { get; init; } = SaleStatus.Normal;

    public string? SourceReference { get; init; }
    public string? SourceHotel { get; init; }
    public string? Outlet { get; init; }
    public string? RoomNo { get; init; }
}

/// <summary>Tek bir CSV satirinin ayristirma sonucu.</summary>
public sealed record RowParseResult
{
    public int RowNumber { get; init; }
    public string RawLine { get; init; } = string.Empty;

    public ParsedSale? Sale { get; init; }

    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsValid => Sale is not null;

    public static RowParseResult Ok(int row, string raw, ParsedSale sale)
        => new() { RowNumber = row, RawLine = raw, Sale = sale };

    public static RowParseResult Fail(int row, string raw, string code, string message)
        => new() { RowNumber = row, RawLine = raw, ErrorCode = code, ErrorMessage = message };
}

/// <summary>Bir kaynak sistemin CSV formatini okuyan ayristirici.</summary>
public interface ISourceImporter
{
    SourceSystem SourceSystem { get; }

    /// <param name="content">CSV dosyasinin tam metni.</param>
    /// <param name="knownEmployees">Personel tablosundaki gecerli personel numaralari.</param>
    IReadOnlyList<RowParseResult> Parse(string content, IReadOnlySet<string> knownEmployees);
}

/// <summary>Bir yukleme isleminin ozeti.</summary>
public sealed record ImportSummary
{
    public Guid BatchId { get; init; }
    public SourceSystem SourceSystem { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int ImportedRows { get; init; }
    public int DuplicateRows { get; init; }
    public int FailedRows { get; init; }
    public int MatchedReversals { get; init; }
    public IReadOnlyList<ImportErrorSummary> Errors { get; init; } = [];
}

public sealed record ImportErrorSummary
{
    public int RowNumber { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string RawLine { get; init; } = string.Empty;
}
