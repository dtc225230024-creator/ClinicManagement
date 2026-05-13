using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClinicManagement.Services;

public static class VietnameseTextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder
            .ToString()
            .Replace('\u0111', 'd')
            .Replace('\u0110', 'd')
            .Normalize(NormalizationForm.FormC);
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        return Regex.Matches(Normalize(text), "[a-z0-9]+")
            .Select(match => match.Value);
    }
}
