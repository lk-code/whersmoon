using System.Globalization;
using System.Text;

namespace WheresMoon.Core.Places;

/// <summary>Reduces text to what a search compares, so that "Tromso" finds "Tromsø".</summary>
public static class SearchText
{
    /// <summary>Lower case without accents. Everything between letters and digits becomes one space.</summary>
    public static string Fold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string decomposed = Ascii.IsValid(text) ? text : text.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        bool needsSpace = false;

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark || IsApostrophe(c))
            {
                continue;
            }

            if (!char.IsLetterOrDigit(c))
            {
                needsSpace = result.Length > 0;
                continue;
            }

            if (needsSpace)
            {
                result.Append(' ');
                needsSpace = false;
            }

            AppendFolded(result, char.ToLowerInvariant(c));
        }

        return result.ToString();
    }

    /// <summary>The spelling with "ae", "oe" and "ue", or null when the name has none of these letters.</summary>
    public static string? ExpandedSpelling(string text)
    {
        string lower = text.ToLowerInvariant();

        return lower.AsSpan().IndexOfAny("äöüø") < 0
            ? null
            : Fold(lower.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ø", "oe"));
    }

    private static bool IsApostrophe(char c) => c is '\'' or '’' or '‘' or 'ʻ' or 'ʼ' or 'ʿ' or 'ʾ' or '`';

    /// <summary>These letters are no accented forms, so decomposing leaves them alone.</summary>
    private static void AppendFolded(StringBuilder result, char c)
    {
        switch (c)
        {
            case 'ß':
                result.Append("ss");
                break;
            case 'æ':
                result.Append("ae");
                break;
            case 'œ':
                result.Append("oe");
                break;
            case 'ø':
                result.Append('o');
                break;
            case 'đ' or 'ð':
                result.Append('d');
                break;
            case 'ł':
                result.Append('l');
                break;
            case 'þ':
                result.Append("th");
                break;
            case 'ı':
                result.Append('i');
                break;
            default:
                result.Append(c);
                break;
        }
    }
}
