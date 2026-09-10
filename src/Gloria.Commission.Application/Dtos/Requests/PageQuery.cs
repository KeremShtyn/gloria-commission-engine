using Gloria.Commission.Domain.Common;

namespace Gloria.Commission.Application.Dtos.Requests;

/// <summary>
/// Sayfali liste istegi: <c>?page=0&amp;size=20&amp;sort=totalCommission,desc</c>.
///
/// Gecersiz deger sessizce duzeltilmez. Istemci 500 satir isteyip 20 alirsa
/// eksik veriyle calistigini fark etmez; bu yuzden sinir disi istek 400 doner.
/// Siralanabilir alanlar listeyi ureten serviste beyaz listeye vurulur:
/// bir yazim hatasi ('totalComission') sessizce varsayilan siralamaya dusmez.
/// </summary>
public sealed record PageQuery
{
    public const int DefaultSize = 20;
    public const int MaxSize = 100;

    public int Page { get; init; }
    public int Size { get; init; } = DefaultSize;

    /// <summary>Beyaz listeden gecmis alan adi; istek siralama tasimiyorsa null.</summary>
    public string? SortField { get; init; }
    public bool Descending { get; init; }

    /// <summary>Siralama kabul etmeyen uclar icin (denetim kayitlari gibi).</summary>
    public static PageQuery Parse(int? page, int? size)
        => Parse(page, size, null, []);

    public static PageQuery Parse(int? page, int? size, string? sort, IReadOnlyCollection<string> sortable)
    {
        if (page is < 0)
            throw new DomainException("INVALID_QUERY", $"page 0 veya daha buyuk olmali: {page}");

        if (size is < 1 or > MaxSize)
            throw new DomainException("INVALID_QUERY",
                $"size 1-{MaxSize} araliginda olmali: {size}");

        var query = new PageQuery { Page = page ?? 0, Size = size ?? DefaultSize };

        if (string.IsNullOrWhiteSpace(sort)) return query;

        var parts = sort.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length > 2 || parts[0].Length == 0)
            throw new DomainException("INVALID_QUERY",
                $"sort biçimi 'alan' ya da 'alan,asc|desc' olmali: '{sort}'");

        var field = sortable.FirstOrDefault(
            candidate => string.Equals(candidate, parts[0], StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException("INVALID_QUERY",
                $"'{parts[0]}' alanina gore siralanamaz. Siralanabilir alanlar: {string.Join(", ", sortable)}");

        var direction = parts.Length == 2 ? parts[1] : "asc";
        var descending = direction.ToLowerInvariant() switch
        {
            "asc" => false,
            "desc" => true,
            _ => throw new DomainException("INVALID_QUERY",
                $"Siralama yonu 'asc' ya da 'desc' olmali: '{direction}'")
        };

        return query with { SortField = field, Descending = descending };
    }
}
