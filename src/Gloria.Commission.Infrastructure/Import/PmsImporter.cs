using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Domain.Entities;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Fidelio (PMS) ekstresi.
/// Kolonlar: BelgeNo;IslemTarihi;OdaNo;MisafirAdi;UrunKodu;UrunAdi;Adet;Tutar;ParaBirimi;KasiyerNo;IslemTipi;Otel
/// </summary>
public sealed class PmsImporter : ISourceImporter
{
    private const int BelgeNo = 0, IslemTarihi = 1, OdaNo = 2, UrunKodu = 4,
                      UrunAdi = 5, Adet = 6, Tutar = 7, ParaBirimi = 8,
                      KasiyerNo = 9, IslemTipi = 10, Otel = 11;

    private readonly IExchangeRateProvider _rates;

    public PmsImporter(IExchangeRateProvider rates) => _rates = rates;

    public SourceSystem SourceSystem => SourceSystem.Pms;

    public IReadOnlyList<RowParseResult> Parse(string content, IReadOnlySet<string> knownEmployees)
    {
        var results = new List<RowParseResult>();

        foreach (var (rowNumber, rawLine, cells) in CsvReaderHelper.ReadRows(content))
        {
            var documentNo = CsvReaderHelper.Cell(cells, BelgeNo);
            if (documentNo is null)
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "MISSING_DOCUMENT_NO", "BelgeNo bos; kayit tekillestirilemez."));
                continue;
            }

            if (!CsvValueParser.TryParseDate(CsvReaderHelper.Cell(cells, IslemTarihi), out var date))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_DATE", $"IslemTarihi '{CsvReaderHelper.Cell(cells, IslemTarihi)}' yyyy-MM-dd formatinda degil."));
                continue;
            }

            if (!CsvValueParser.TryParseDecimal(CsvReaderHelper.Cell(cells, Tutar), out var amount))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_AMOUNT", $"Tutar '{CsvReaderHelper.Cell(cells, Tutar)}' sayiya cevrilemedi."));
                continue;
            }

            var employeeNo = CsvReaderHelper.Cell(cells, KasiyerNo);
            if (employeeNo is null || !knownEmployees.Contains(employeeNo))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "UNKNOWN_EMPLOYEE", $"KasiyerNo '{employeeNo ?? "(bos)"}' personel tablosunda yok."));
                continue;
            }

            var productCode = CsvReaderHelper.Cell(cells, UrunKodu);
            if (productCode is null)
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "MISSING_PRODUCT_CODE", "UrunKodu bos."));
                continue;
            }

            CsvValueParser.TryParseInt(CsvReaderHelper.Cell(cells, Adet), out var quantity);
            if (quantity == 0) quantity = 1;

            var isReversal = string.Equals(
                CsvReaderHelper.Cell(cells, IslemTipi), "REVERSAL", StringComparison.OrdinalIgnoreCase);

            // Ters kayit her zaman negatif tutarla saklanir; kaynak pozitif gonderse de duzeltilir.
            if (isReversal && amount > 0) amount = -amount;

            var currency = CsvReaderHelper.Cell(cells, ParaBirimi) ?? "TRY";
            if (!_rates.TryGetRateToTry(currency, date, out var rate))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "EXCHANGE_RATE_MISSING", $"ParaBirimi '{currency}' icin kur tanimli degil."));
                continue;
            }

            results.Add(RowParseResult.Ok(rowNumber, rawLine, new SaleRecord
            {
                SourceSystem = SourceSystem.Pms,
                SourceDocumentNo = documentNo,
                SourceHash = CsvReaderHelper.Hash(nameof(SourceSystem.Pms), documentNo),
                TransactionDate = date,
                EmployeeNo = employeeNo,
                ProductCode = productCode,
                ProductName = CsvReaderHelper.Cell(cells, UrunAdi) ?? productCode,
                ProductGroup = ProductCatalog.GroupFromPmsCode(productCode),
                Quantity = Math.Abs(quantity),
                Amount = amount,
                Currency = currency,
                ExchangeRate = rate,
                AmountTry = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero),
                Status = isReversal ? SaleStatus.Refund : SaleStatus.Normal,
                Hotel = CsvReaderHelper.Cell(cells, Otel),
                RoomNo = CsvReaderHelper.Cell(cells, OdaNo)
            }));
        }

        return results;
    }
}
