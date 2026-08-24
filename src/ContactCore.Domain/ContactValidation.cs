using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ContactCore.Domain;

public sealed record ValidationIssue(string Field, string Message);

public static partial class ContactValidation
{
    public static IReadOnlyList<ValidationIssue> Validate(Contact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);
        var issues = new List<ValidationIssue>();
        if (contact.GivenName.Length > 120) issues.Add(new("GivenName", "Given name must be 120 characters or fewer."));
        if (contact.FamilyName.Length > 120) issues.Add(new("FamilyName", "Family name must be 120 characters or fewer."));
        if (contact.Nickname.Length > 120) issues.Add(new("Nickname", "Nickname must be 120 characters or fewer."));
        if (contact.Notes.Length > 20_000) issues.Add(new("Notes", "Notes must be 20,000 characters or fewer."));

        foreach (var email in contact.Emails)
        {
            if (!IsEmail(email.Address)) issues.Add(new("Email", "Enter a valid email address."));
        }
        foreach (var phone in contact.Phones)
        {
            if (string.IsNullOrWhiteSpace(phone.Number) || !PhonePattern().IsMatch(phone.Number))
                issues.Add(new("Phone", "Enter a valid phone number."));
        }
        return issues;
    }

    private static bool IsEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 320) return false;
        try { return new MailAddress(value).Address.Equals(value, StringComparison.OrdinalIgnoreCase); }
        catch (FormatException) { return false; }
    }

    [GeneratedRegex(@"^[0-9+() .-]{3,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
