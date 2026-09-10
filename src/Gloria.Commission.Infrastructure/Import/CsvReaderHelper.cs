namespace Gloria.Commission.Infrastructure.Import;

internal static class CsvReaderHelper
{
    /// <summary>Kaynak dosyalar noktali virgul ayracli ve BOM'lu UTF-8.</summary>
    private const char Delimiter = ';';

    /// <summary>Basliksiz, bos satirlari atilmis veri satirlarini fiziksel satir numarasiyla dondurur.</summary>
    public static IEnumerable<(int RowNumber, string RawLine, string[] Cells)> ReadRows(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (i == 0) continue;                       // baslik
            if (string.IsNullOrWhiteSpace(line)) continue;

            yield return (i + 1, line, line.Split(Delimiter));
        }
    }

    public static string? Cell(string[] cells, int index)
        => index < cells.Length ? CsvValueParser.Trimmed(cells[index]) : null;
}
