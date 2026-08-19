using System.Globalization;
using System.Text;

namespace ContactCore.Domain;

public static class TextNormalizer
{
    public static string SearchKey(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    public static string PhoneKey(string? input) =>
        string.Concat((input ?? string.Empty).Where(char.IsDigit));
}
