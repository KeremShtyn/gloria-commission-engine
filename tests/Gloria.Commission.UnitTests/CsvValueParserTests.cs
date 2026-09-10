using FluentAssertions;
using Gloria.Commission.Infrastructure.Import;
using Xunit;

namespace Gloria.Commission.UnitTests;

/// <summary>Ham CSV degerlerinin ayristirilmasi: tarihte kati, tutarda toleransli.</summary>
public class CsvValueParserTests
{
    [Theory]
    [InlineData("2500.00", 2500)]
    [InlineData("2.500,00", 2500)]
    [InlineData("-4200.00", -4200)]
    [InlineData("12000", 12000)]
    [InlineData("1.234.567,89", 1234567.89)]
    public void Iki_farkli_sayi_formati_da_okunur(string raw, decimal expected)
    {
        CsvValueParser.TryParseDecimal(raw, out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void Sayiya_cevrilemeyen_deger_reddedilir(string? raw)
    {
        CsvValueParser.TryParseDecimal(raw, out _).Should().BeFalse();
    }

    [Fact]
    public void Iso_formatindaki_tarih_okunur()
    {
        CsvValueParser.TryParseDate("2026-08-15", out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 8, 15));
    }

    [Theory]
    [InlineData("32/08/2026")]   // gecersiz gun
    [InlineData("08.15.2026")]   // belirsiz format
    [InlineData("15/08/2026")]
    [InlineData("")]
    public void Iso_disindaki_tarih_reddedilir(string raw)
    {
        // Tarih tahmini primi yanlis doneme yazar; belirsiz format kabul edilmez.
        CsvValueParser.TryParseDate(raw, out _).Should().BeFalse();
    }
}
