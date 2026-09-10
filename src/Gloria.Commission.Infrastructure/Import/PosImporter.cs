using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Flyby (POS) fis ekstresi.
/// Kolonlar: FisNo;Tarih;Saat;OutletKodu;PLU;UrunAdi;Adet;BirimFiyat;ToplamTutar;SatisPersoneli;IadeMi;OdemeTipi;OdaNo
/// </summary>
public sealed class PosImporter : ISourceImporter
{
    private const int FisNo = 0, Tarih = 1, OutletKodu = 3, Plu = 4, UrunAdi = 5,
                      Adet = 6, ToplamTutar = 8, SatisPersoneli = 9, IadeMi = 10, OdaNo = 12;

    private readonly IExchangeRateProvider _rates;

    public PosImporter(IExchangeRateProvider rates) => _rates = rates;

    public SourceSystem SourceSystem => SourceSystem.Pos;

    public IReadOnlyList<RowParseResult> Parse(string content, IReadOnlySet<string> knownEmployees)
    {
        var results = new List<RowParseResult>();

        foreach (var (rowNumber, rawLine, cells) in CsvReaderHelper.ReadRows(content))
        {
            var documentNo = CsvReaderHelper.Cell(cells, FisNo);
            if (documentNo is null)
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "MISSING_DOCUMENT_NO", "FisNo bos; kayit tekillestirilemez."));
                continue;
            }

            if (!CsvValueParser.TryParseDate(CsvReaderHelper.Cell(cells, Tarih), out var date))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_DATE", $"Tarih '{CsvReaderHelper.Cell(cells, Tarih)}' yyyy-MM-dd formatinda degil."));
                continue;
            }

            if (!CsvValueParser.TryParseDecimal(CsvReaderHelper.Cell(cells, ToplamTutar), out var amount))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_AMOUNT", $"ToplamTutar '{CsvReaderHelper.Cell(cells, ToplamTutar)}' sayiya cevrilemedi."));
                continue;
            }

            var employeeNo = CsvReaderHelper.Cell(cells, SatisPersoneli);
            if (employeeNo is null || !knownEmployees.Contains(employeeNo))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "UNKNOWN_EMPLOYEE", $"SatisPersoneli '{employeeNo ?? "(bos)"}' personel tablosunda yok."));
                continue;
            }

            CsvValueParser.TryParseInt(CsvReaderHelper.Cell(cells, Adet), out var quantity);

            // Adet 0 ve tutar 0 olan satirin ekonomik icerigi yok; prime de mutabakata da girmemeli.
            if (quantity == 0 && amount == 0)
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "ZERO_QUANTITY", "Adet ve tutar sifir; anlamli bir satis satiri degil."));
                continue;
            }

            var isRefund = string.Equals(CsvReaderHelper.Cell(cells, IadeMi), "E", StringComparison.OrdinalIgnoreCase)
                           || amount < 0;

            if (isRefund && amount > 0) amount = -amount;

            var outlet = CsvReaderHelper.Cell(cells, OutletKodu) ?? string.Empty;
            var plu = CsvReaderHelper.Cell(cells, Plu) ?? "PLU";
            const decimal rate = 1m; // POS ekstresi yalnizca TRY uretir

            results.Add(RowParseResult.Ok(rowNumber, rawLine, new SaleRecord
            {
                SourceSystem = SourceSystem.Pos,
                SourceDocumentNo = documentNo,
                SourceHash = ContentHash.ForDocument(nameof(SourceSystem.Pos), documentNo),
                TransactionDate = date,
                EmployeeNo = employeeNo,
                ProductCode = $"{outlet}-{plu}",
                ProductName = CsvReaderHelper.Cell(cells, UrunAdi) ?? plu,
                ProductGroup = ProductCatalog.GroupFromPosOutlet(outlet),
                Quantity = Math.Abs(quantity),
                Amount = amount,
                Currency = "TRY",
                ExchangeRate = rate,
                AmountTry = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero),
                Status = isRefund ? SaleStatus.Refund : SaleStatus.Normal,
                Outlet = outlet,
                RoomNo = CsvReaderHelper.Cell(cells, OdaNo)
            }));
        }

        return results;
    }
}
