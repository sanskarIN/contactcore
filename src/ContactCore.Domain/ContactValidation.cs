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
        if (contact.Notes.Length > 20_000) issues.Add(new("Notes", "Primary notes must be 20,000 characters or fewer."));

        foreach (var email in contact.Emails)
        {
            if (!IsEmail(email.Address)) issues.Add(new("Email", $"Invalid email address: {email.Address}"));
            if (email.Label.Length > 80) issues.Add(new("EmailLabel", "Email labels must be 80 characters or fewer."));
        }

        foreach (var phone in contact.Phones)
        {
            if (string.IsNullOrWhiteSpace(phone.Number) || !PhonePattern().IsMatch(phone.Number))
                issues.Add(new("Phone", $"Invalid phone number: {phone.Number}"));
            if (phone.Label.Length > 80) issues.Add(new("PhoneLabel", "Phone labels must be 80 characters or fewer."));
        }

        foreach (var date in contact.Dates)
        {
            if (date.Label.Length > 80) issues.Add(new("DateLabel", "Date labels must be 80 characters or fewer."));
        }

        foreach (var note in contact.NoteEntries)
        {
            if (note.Label.Length > 120) issues.Add(new("NoteLabel", "Note labels must be 120 characters or fewer."));
            if (note.Content.Length > 20_000) issues.Add(new("Note", "Each note must be 20,000 characters or fewer."));
        }

        foreach (var group in contact.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name) || group.Name.Length > 120)
                issues.Add(new("Group", "Group names must contain 1 to 120 characters."));
        }

        foreach (var tag in contact.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Name) || tag.Name.Length > 120)
                issues.Add(new("Tag", "Tag names must contain 1 to 120 characters."));
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
