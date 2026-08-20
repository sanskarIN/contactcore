using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class RedactingLogTests
{
    [TestMethod]
    public void Sanitize_redacts_common_email_and_long_number_shapes()
    {
        const string email = "ada@example.test";
        const string number = "+91 99999 00000";
        var input = $"Failed while processing {email} and {number}.";

        var sanitized = RedactingLog.Sanitize(input);

        Assert.IsFalse(sanitized.Contains(email, StringComparison.Ordinal));
        Assert.IsFalse(sanitized.Contains(number, StringComparison.Ordinal));
        Assert.IsTrue(sanitized.Contains("[email-redacted]", StringComparison.Ordinal));
        Assert.IsTrue(sanitized.Contains("[number-redacted]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Sanitize_bounds_very_long_diagnostic_text()
    {
        var sanitized = RedactingLog.Sanitize(new string('x', 10_000));

        Assert.IsTrue(sanitized.Length <= 2_003, "Sanitized diagnostics should stay close to the documented 2,000-character cap.");
    }
}
