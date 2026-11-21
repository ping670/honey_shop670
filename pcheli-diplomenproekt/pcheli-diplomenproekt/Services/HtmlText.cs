using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace pcheli_diplomenproekt.Services;

/// <summary>
/// HTML енкодер, който оставя кирилицата четлива („Мед“, а не „&amp;#x41C;&amp;#x435;&amp;#x434;“), но продължава да кодира
/// всички опасни символи (&lt; &gt; &amp; " '). Ползва се за страниците на блога, писмата и товарителниците.
/// </summary>
public static class HtmlText
{
    public static readonly HtmlEncoder Encoder = HtmlEncoder.Create(
        new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic, UnicodeRanges.GeneralPunctuation, UnicodeRanges.LatinExtendedA));

    public static string Encode(string? value) => Encoder.Encode(value ?? string.Empty);
}
