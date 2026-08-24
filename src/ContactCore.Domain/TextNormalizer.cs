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

    public static bool PhoneEquivalent(string? left, string? right)
    {
        var leftKey = PhoneKey(left);
        var rightKey = PhoneKey(right);

        if (leftKey.Length == 0 || rightKey.Length == 0) return false;
        if (string.Equals(leftKey, rightKey, StringComparison.Ordinal)) return true;

        var shorter = leftKey.Length < rightKey.Length ? leftKey : rightKey;
        var longer = leftKey.Length < rightKey.Length ? rightKey : leftKey;

        // Keep country-code inference deliberately conservative. Country calling codes
        // are at most three digits, but accepting shorter suffixes can turn a different
        // subscriber number into a false duplicate (for example, dropping a leading
        // local digit as though it were part of the country code). Requiring a
        // ten-digit local representation covers the release's supported equivalence
        // cases while preferring false negatives over destructive false positives.
        return shorter.Length >= 10 &&
               longer.Length - shorter.Length <= 3 &&
               longer.EndsWith(shorter, StringComparison.Ordinal);
    }
}
