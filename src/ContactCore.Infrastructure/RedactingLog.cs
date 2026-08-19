using System.Text.RegularExpressions;

namespace ContactCore.Infrastructure;

public static partial class RedactingLog
{
    public static string Sanitize(string message)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        var sanitized = EmailPattern().Replace(message, "[email-redacted]");
        sanitized = LongNumberPattern().Replace(sanitized, "[number-redacted]");
        return sanitized.Length <= 2_000 ? sanitized : sanitized[..2_000] + "…";
    }
    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
    [GeneratedRegex(@"(?<!\d)\+?[\d() .-]{7,}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex LongNumberPattern();
}
