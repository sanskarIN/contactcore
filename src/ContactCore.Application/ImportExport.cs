using System.Globalization;
using System.Text;
using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record ImportResult(IReadOnlyList<Contact> Contacts, IReadOnlyList<string> Warnings);

public static class ContactCsvCodec
{
    private static readonly HashSet<string> KnownHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "GivenName", "FamilyName", "Nickname", "Email", "Phone", "Birthday", "Notes"
    };

    public static string Export(IEnumerable<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var sb = new StringBuilder();
        sb.AppendLine("GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes");
        foreach (var c in contacts)
        {
            ArgumentNullException.ThrowIfNull(c);
            sb.AppendLine(string.Join(',', new[]
            {
                Escape(c.GivenName), Escape(c.FamilyName), Escape(c.Nickname),
                Escape(c.Emails.FirstOrDefault()?.Address ?? ""), Escape(c.Phones.FirstOrDefault()?.Number ?? ""),
                Escape(c.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""), Escape(c.Notes)
            }));
        }
        return sb.ToString();
    }

    public static ImportResult Import(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = Parse(csv).ToList();
        if (rows.Count == 0) return new([], []);

        var warnings = new List<string>();
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rows[0].Count; index++)
        {
            var name = rows[0][index].Trim();
            if (name.Length == 0) continue;
            if (!map.TryAdd(name, index)) warnings.Add($"CSV header '{name}' appears more than once; the first column is used.");
        }

        if (!map.Keys.Any(KnownHeaders.Contains))
        {
            warnings.Add("CSV header does not contain any ContactCore-supported columns; no contacts were imported.");
            return new([], warnings);
        }

        var contacts = new List<Contact>();
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            string Get(string name) => map.TryGetValue(name, out var i) && i < row.Count ? row[i].Trim() : string.Empty;
            var contact = new Contact { GivenName = Get("GivenName"), FamilyName = Get("FamilyName"), Nickname = Get("Nickname"), Notes = Get("Notes") };
            var email = Get("Email");
            if (email.Length > 0) contact.Emails.Add(new(Guid.NewGuid(), "Email", email));
            var phone = Get("Phone");
            if (phone.Length > 0) contact.Phones.Add(new(Guid.NewGuid(), "Phone", phone));
            var birthday = Get("Birthday");
            if (birthday.Length > 0)
            {
                if (DateOnly.TryParseExact(birthday, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) contact.Birthday = parsed;
                else warnings.Add($"Row {rowIndex + 1}: birthday was not yyyy-MM-dd.");
            }
            if (HasSpreadsheetFormulaPrefix(contact))
                warnings.Add($"Row {rowIndex + 1}: a text field begins with a spreadsheet formula character. ContactCore stores it as text; use caution if this data is later opened in spreadsheet software.");
            contacts.Add(contact);
        }
        return new(contacts, warnings);
    }

    private static bool HasSpreadsheetFormulaPrefix(Contact contact)
    {
        static bool Risky(string value)
        {
            var trimmed = value.TrimStart();
            return trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@';
        }

        return Risky(contact.GivenName) || Risky(contact.FamilyName) || Risky(contact.Nickname) || Risky(contact.Notes) ||
               contact.Emails.Any(x => Risky(x.Address)) || contact.Phones.Any(x => Risky(x.Number));
    }

    private static string Escape(string value) => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static IEnumerable<List<string>> Parse(string text)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
            }
            else if (ch == '"' && field.Length == 0) quoted = true;
            else if (ch == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (ch == '\r') { }
            else if (ch == '\n') { row.Add(field.ToString()); field.Clear(); yield return row; row = []; }
            else field.Append(ch);
        }
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); yield return row; }
    }
}

public static class VCardCodec
{
    public static string Export(IEnumerable<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var sb = new StringBuilder();
        foreach (var c in contacts)
        {
            ArgumentNullException.ThrowIfNull(c);
            AppendLine(sb, "BEGIN:VCARD");
            AppendLine(sb, "VERSION:4.0");
            AppendLine(sb, $"N:{Esc(c.FamilyName)};{Esc(c.GivenName)};;;");
            AppendLine(sb, $"FN:{Esc(c.DisplayName)}");
            foreach (var phone in c.Phones) AppendLine(sb, $"TEL;TYPE={phone.Kind.ToString().ToLowerInvariant()}:{Esc(phone.Number)}");
            foreach (var email in c.Emails) AppendLine(sb, $"EMAIL;TYPE={email.Kind.ToString().ToLowerInvariant()}:{Esc(email.Address)}");
            if (c.Birthday is not null) AppendLine(sb, $"BDAY:{c.Birthday.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}");
            if (c.Notes.Length > 0) AppendLine(sb, $"NOTE:{Esc(c.Notes)}");
            AppendLine(sb, "END:VCARD");
        }
        return sb.ToString();
    }

    public static ImportResult Import(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var contacts = new List<Contact>();
        var warnings = new List<string>();
        Contact? current = null;
        foreach (var raw in Unfold(text))
        {
            var line = raw.TrimEnd('\r');
            if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) warnings.Add("A nested BEGIN:VCARD was encountered; the incomplete previous card was ignored.");
                current = new Contact();
                continue;
            }
            if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) contacts.Add(current);
                current = null;
                continue;
            }
            if (current is null) continue;
            var colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var key = line[..colon];
            var property = key.Split(';', 2)[0];
            var rawValue = line[(colon + 1)..];
            if (property.Equals("FN", StringComparison.OrdinalIgnoreCase) && current.DisplayName == "Unnamed contact")
            {
                current.GivenName = Unesc(rawValue);
            }
            else if (property.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                var parts = SplitEscaped(rawValue, ';').Select(Unesc).ToArray();
                if (parts.Length > 0) current.FamilyName = parts[0];
                if (parts.Length > 1) current.GivenName = parts[1];
            }
            else if (property.Equals("TEL", StringComparison.OrdinalIgnoreCase))
            {
                var kind = ParseFieldKind(key, ContactFieldKind.Mobile);
                current.Phones.Add(new(Guid.NewGuid(), kind.ToString(), Unesc(rawValue), kind));
            }
            else if (property.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                var kind = ParseFieldKind(key, ContactFieldKind.Home);
                current.Emails.Add(new(Guid.NewGuid(), kind.ToString(), Unesc(rawValue), kind));
            }
            else if (property.Equals("BDAY", StringComparison.OrdinalIgnoreCase))
            {
                var value = Unesc(rawValue);
                if (DateOnly.TryParseExact(value.Replace("-", "", StringComparison.Ordinal), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) current.Birthday = date;
                else warnings.Add("A vCard birthday could not be parsed; expected YYYYMMDD or YYYY-MM-DD.");
            }
            else if (property.Equals("NOTE", StringComparison.OrdinalIgnoreCase))
            {
                current.Notes = Unesc(rawValue);
            }
        }
        if (current is not null) warnings.Add("A vCard did not contain END:VCARD and was ignored.");
        return new(contacts, warnings);
    }

    private static ContactFieldKind ParseFieldKind(string key, ContactFieldKind fallback)
    {
        foreach (var parameter in key.Split(';').Skip(1))
        {
            var equals = parameter.IndexOf('=');
            if (equals <= 0 || !parameter[..equals].Equals("TYPE", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var token in parameter[(equals + 1)..].Split(','))
            {
                if (token.Equals("home", StringComparison.OrdinalIgnoreCase)) return ContactFieldKind.Home;
                if (token.Equals("work", StringComparison.OrdinalIgnoreCase)) return ContactFieldKind.Work;
                if (token.Equals("cell", StringComparison.OrdinalIgnoreCase) || token.Equals("mobile", StringComparison.OrdinalIgnoreCase)) return ContactFieldKind.Mobile;
                if (token.Equals("other", StringComparison.OrdinalIgnoreCase)) return ContactFieldKind.Other;
            }
        }
        return fallback;
    }

    private static List<string> SplitEscaped(string value, char separator)
    {
        var output = new List<string>();
        var field = new StringBuilder();
        var escaped = false;
        foreach (var ch in value)
        {
            if (ch == separator && !escaped)
            {
                output.Add(field.ToString());
                field.Clear();
                continue;
            }

            field.Append(ch);
            if (escaped) escaped = false;
            else if (ch == '\\') escaped = true;
        }
        output.Add(field.ToString());
        return output;
    }

    private static List<string> Unfold(string text)
    {
        var output = new List<string>();
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && output.Count > 0) output[^1] += line[1..];
            else output.Add(line);
        }
        return output;
    }

    private static void AppendLine(StringBuilder builder, string value) => builder.Append(value).Append("\r\n");

    private static string Esc(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Replace(",", "\\,", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal);

    private static string Unesc(string value)
    {
        var output = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];
            if (ch != '\\' || index + 1 >= value.Length)
            {
                output.Append(ch);
                continue;
            }

            var next = value[++index];
            output.Append(next switch
            {
                'n' or 'N' => '\n',
                ',' => ',',
                ';' => ';',
                '\\' => '\\',
                _ => next
            });
        }
        return output.ToString();
    }
}
