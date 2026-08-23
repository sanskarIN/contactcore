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

        // Country calling codes are at most three digits. Requiring at least seven
        // local digits avoids treating short extensions or service numbers as equal.
        return shorter.Length >= 7 &&
               longer.Length - shorter.Length <= 3 &&
               longer.EndsWith(shorter, StringComparison.Ordinal);
    }
}
