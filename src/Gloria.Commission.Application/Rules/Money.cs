namespace Gloria.Commission.Application.Rules;

internal static class Money
{
    /// <summary>Para tutarlarını 2 haneye yuvarlar. Bankacı yuvarlaması prim lehine sapma yaratmasın diye AwayFromZero.</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string Format(decimal value) => value.ToString("N2", Culture.Tr);
}
