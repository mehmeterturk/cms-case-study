using System.Text;
using System.Text.RegularExpressions;

namespace ContentService.Domain.Common;

/// <summary>
/// Bir başlığı URL dostu, ASCII bir "slug"a çevirir. Türkçe karakterleri
/// karşılıklarına dönüştürür (ç→c, ğ→g, ı→i, ö→o, ş→s, ü→u).
/// </summary>
public static class SlugGenerator
{
    private static readonly Dictionary<char, char> TurkishMap = new()
    {
        ['ç'] = 'c', ['Ç'] = 'c',
        ['ğ'] = 'g', ['Ğ'] = 'g',
        ['ı'] = 'i', ['İ'] = 'i', ['I'] = 'i',
        ['ö'] = 'o', ['Ö'] = 'o',
        ['ş'] = 's', ['Ş'] = 's',
        ['ü'] = 'u', ['Ü'] = 'u'
    };

    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(TurkishMap.TryGetValue(ch, out var mapped) ? mapped : ch);
        }

        var slug = sb.ToString().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-"); // alfanümerik olmayanları tireye çevir
        slug = slug.Trim('-');                            // baş/son tireleri temizle
        return slug;
    }
}
