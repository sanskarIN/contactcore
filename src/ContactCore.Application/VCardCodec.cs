using System.Globalization;
using System.Text;
using ContactCore.Domain;

namespace ContactCore.Application;

public static class VCardCodec
{
    public static string Export(IEnumerable<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var builder = new StringBuilder();
        foreach (var contact in contacts)
        {
            builder.AppendLine("BEGIN:VCARD");
            builder.AppendLine("VERSION:4.0");
            builder.Append("UID:").AppendLine(contact.Id.ToString("D", CultureInfo.InvariantCulture));
            builder.Append("FN:").AppendLine(Escape(contact.DisplayName));
            builder.Append("N:").Append(Escape(contact.FamilyName)).Append(';').Append(Escape(contact.GivenName)).AppendLine(";;;");
            if (contact.Nickname.Length > 0) builder.Append("NICKNAME:").AppendLine(Escape(contact.Nickname));
            if (contact.Birthday is { } birthday) builder.Append("BDAY:").AppendLine(birthday.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            foreach (var email in contact.Emails)
                builder.Append("EMAIL;TYPE=").Append(TypeValue(email.Kind)).Append(':').AppendLine(Escape(email.Address));
            foreach (var phone in contact.Phones)
                builder.Append("TEL;TYPE=").Append(TypeValue(phone.Kind)).Append(':').AppendLine(Escape(phone.Number));
            if (contact.Notes.Length > 0) builder.Append("NOTE:").AppendLine(Escape(contact.Notes));
            builder.AppendLine("END:VCARD");
        }
        return builder.ToString();
    }

    public static IReadOnlyList<Contact> Import(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = UnfoldLines(text);
        var result = new List<Contact>();
        Contact? current = null;

        foreach (var line in lines)
        {
            if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) throw new FormatException("Nested vCard is not valid.");
                current = new Contact();
                continue;
            }
            if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is null) throw new FormatException("END:VCARD without BEGIN:VCARD.");
                result.Add(current);
                current = null;
                continue;
            }
            if (current is null || line.StartsWith("VERSION:", StringComparison.OrdinalIgnoreCase)) continue;

            var colon = FindUnescaped(line, ':');
            if (colon < 0) continue;
            var descriptor = line[..colon];
            var value = Unescape(line[(colon + 1)..]);
            var semicolon = descriptor.IndexOf(';');
            var name = (semicolon >= 0 ? descriptor[..semicolon] : descriptor).ToUpperInvariant();
            var parameters = semicolon >= 0 ? descriptor[(semicolon + 1)..] : string.Empty;

            switch (name)
            {
                case "UID":
                    if (Guid.TryParse(value, out var id)) current = CopyWithId(current, id);
                    break;
                case "N":
                    var parts = SplitEscaped(line[(colon + 1)..], ';').Select(Unescape).ToArray();
                    if (parts.Length > 0) current.FamilyName = parts[0];
                    if (parts.Length > 1) current.GivenName = parts[1];
                    break;
                case "NICKNAME": current.Nickname = value; break;
                case "BDAY":
                    if (DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var birthday))
                        current.Birthday = birthday;
                    break;
                case "EMAIL": current.Emails.Add(new(Guid.NewGuid(), "Imported", value, ParseKind(parameters, ContactFieldKind.Home))); break;
                case "TEL": current.Phones.Add(new(Guid.NewGuid(), "Imported", value, ParseKind(parameters, ContactFieldKind.Mobile))); break;
                case "NOTE": current.Notes = value; break;
            }
        }

        if (current is not null) throw new FormatException("vCard is missing END:VCARD.");
        return result;
    }

    private static Contact CopyWithId(Contact source, Guid id)
    {
        var copy = new Contact
        {
            Id = id, GivenName = source.GivenName, FamilyName = source.FamilyName, Nickname = source.Nickname,
            Birthday = source.Birthday, Notes = source.Notes, IsFavorite = source.IsFavorite, IsArchived = source.IsArchived,
            CreatedAt = source.CreatedAt, UpdatedAt = source.UpdatedAt
        };
        copy.Phones.AddRange(source.Phones); copy.Emails.AddRange(source.Emails); copy.Addresses.AddRange(source.Addresses);
        copy.Organizations.AddRange(source.Organizations); copy.Groups.AddRange(source.Groups); copy.Tags.AddRange(source.Tags);
        return copy;
    }

    private static ContactFieldKind ParseKind(string parameters, ContactFieldKind fallback)
    {
        var lower = parameters.ToLowerInvariant();
        if (lower.Contains("work", StringComparison.Ordinal)) return ContactFieldKind.Work;
        if (lower.Contains("home", StringComparison.Ordinal)) return ContactFieldKind.Home;
        if (lower.Contains("cell", StringComparison.Ordinal) || lower.Contains("mobile", StringComparison.Ordinal)) return ContactFieldKind.Mobile;
        return fallback;
    }

    private static string TypeValue(ContactFieldKind kind) => kind switch
    {
        ContactFieldKind.Home => "home",
        ContactFieldKind.Work => "work",
        ContactFieldKind.Mobile => "cell",
        _ => "other"
    };

    private static IReadOnlyList<string> UnfoldLines(string text)
    {
        var physical = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        var logical = new List<string>();
        foreach (var line in physical)
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && logical.Count > 0)
                logical[^1] += line[1..];
            else if (line.Length > 0)
                logical.Add(line);
        }
        return logical;
    }

    private static int FindUnescaped(string value, char target)
    {
        var escaped = false;
        for (var i = 0; i < value.Length; i++)
        {
            if (!escaped && value[i] == target) return i;
            if (!escaped && value[i] == '\\') escaped = true; else escaped = false;
        }
        return -1;
    }

    private static IEnumerable<string> SplitEscaped(string value, char separator)
    {
        var builder = new StringBuilder();
        var escaped = false;
        foreach (var ch in value)
        {
            if (!escaped && ch == separator) { yield return builder.ToString(); builder.Clear(); continue; }
            if (!escaped && ch == '\\') escaped = true; else escaped = false;
            builder.Append(ch);
        }
        yield return builder.ToString();
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal);

    private static string Unescape(string value)
    {
        var builder = new StringBuilder(value.Length);
        var escaped = false;
        foreach (var ch in value)
        {
            if (escaped)
            {
                builder.Append(ch == 'n' || ch == 'N' ? '\n' : ch);
                escaped = false;
            }
            else if (ch == '\\') escaped = true;
            else builder.Append(ch);
        }
        if (escaped) builder.Append('\\');
        return builder.ToString();
    }
}
