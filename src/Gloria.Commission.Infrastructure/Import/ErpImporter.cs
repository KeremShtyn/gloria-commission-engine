using Gloria.Commission.Application.Abstractions;
using Gloria.Commission.Application.Import;
using Gloria.Commission.Domain.Enums;

namespace Gloria.Commission.Infrastructure.Import;

/// <summary>
/// Oracle JDE (ERP) muhasebe ekstresi.
/// Kolonlar: DocNumber;DocType;GLDate;BusinessUnit;ObjectAccount;Amount;Currency;Reference;EmployeeNo;Description;Status
///
/// ERP prim tabani degil, dogrulama katmanidir (bkz. docs/adr/003).
/// UNPOSTED satirlar da alinir ama prime esas sayilmaz; mutabakat raporunda gorunur.
/// </summary>
public sealed class ErpImporter : ISourceImporter
{
    private const int DocNumber = 0, DocType = 1, GLDate = 2, ObjectAccount = 4,
                      Amount = 5, Currency = 6, Reference = 7, EmployeeNo = 8,
                      Description = 9, Status = 10;

    private readonly IExchangeRateProvider _rates;

    public ErpImporter(IExchangeRateProvider rates) => _rates = rates;

    public SourceSystem SourceSystem => SourceSystem.Erp;

    public IReadOnlyList<RowParseResult> Parse(string content, IReadOnlySet<string> knownEmployees)
    {
        var results = new List<RowParseResult>();

        foreach (var (rowNumber, rawLine, cells) in CsvReaderHelper.ReadRows(content))
        {
            var documentNo = CsvReaderHelper.Cell(cells, DocNumber);
            if (documentNo is null)
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "MISSING_DOCUMENT_NO", "DocNumber bos; kayit tekillestirilemez."));
                continue;
            }

            var docType = CsvReaderHelper.Cell(cells, DocType)?.ToUpperInvariant();
            if (docType is not ("RI" or "RM"))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "UNSUPPORTED_DOC_TYPE", $"DocType '{docType ?? "(bos)"}' desteklenmiyor; yalnizca RI ve RM islenir."));
                continue;
            }

            if (!CsvValueParser.TryParseDate(CsvReaderHelper.Cell(cells, GLDate), out var date))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_DATE", $"GLDate '{CsvReaderHelper.Cell(cells, GLDate)}' yyyy-MM-dd formatinda degil."));
                continue;
            }

            if (!CsvValueParser.TryParseDecimal(CsvReaderHelper.Cell(cells, Amount), out var amount))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "INVALID_AMOUNT", $"Amount '{CsvReaderHelper.Cell(cells, Amount)}' sayiya cevrilemedi."));
                continue;
            }

            var employeeNo = CsvReaderHelper.Cell(cells, EmployeeNo);
            if (employeeNo is null || !knownEmployees.Contains(employeeNo))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "UNKNOWN_EMPLOYEE", $"EmployeeNo '{employeeNo ?? "(bos)"}' personel tablosunda yok."));
                continue;
            }

            var isCreditMemo = docType == "RM";
            if (isCreditMemo && amount > 0) amount = -amount;

            var isPosted = string.Equals(
                CsvReaderHelper.Cell(cells, Status), "POSTED", StringComparison.OrdinalIgnoreCase);

            CsvValueParser.TryParseInt(CsvReaderHelper.Cell(cells, ObjectAccount), out var account);

            var description = CsvReaderHelper.Cell(cells, Description) ?? "ERP kaydi";
            var currency = CsvReaderHelper.Cell(cells, Currency) ?? "TRY";
            if (!_rates.TryGetRateToTry(currency, date, out var rate))
            {
                results.Add(RowParseResult.Fail(rowNumber, rawLine,
                    "EXCHANGE_RATE_MISSING", $"Currency '{currency}' icin kur tanimli degil."));
                continue;
            }

            var status = !isPosted
                ? SaleStatus.Unposted
                : isCreditMemo ? SaleStatus.Refund : SaleStatus.Normal;

            results.Add(RowParseResult.Ok(rowNumber, rawLine, new ParsedSale
            {
                SourceSystem = SourceSystem.Erp,
                SourceDocumentNo = documentNo,
                SourceHash = ContentHash.ForDocument(nameof(SourceSystem.Erp), documentNo),
                TransactionDate = date,
                EmployeeNo = employeeNo,
                ProductCode = ProductCatalog.CodeFromErpDescription(description),
                ProductName = description,
                ProductGroupCode = ProductCatalog.GroupFromErpAccount(account),
                Quantity = 1,
                Amount = amount,
                Currency = currency,
                ExchangeRate = rate,
                AmountTry = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero),
                Status = status,
                // RM satirlarinda Reference iptal edilen belgenin numarasidir.
                SourceReference = CsvReaderHelper.Cell(cells, Reference)
            }));
        }

        return results;
    }
}
