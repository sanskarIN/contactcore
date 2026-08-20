using System.Globalization;
using System.Text;
using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record ImportResult(IReadOnlyList<Contact> Contacts, IReadOnlyList<string> Warnings);

public static class ContactCsvCodec
{
    public static string Export(IEnumerable<Contact> contacts) => ExportCore(contacts, spreadsheetSafe: false);

    /// <summary>
    /// Exports CSV intended for direct opening in spreadsheet applications. Text cells that begin with
    /// characters commonly interpreted as formulas are prefixed with an apostrophe before CSV escaping.
    /// Use <see cref="Export"/> when exact text round-tripping is more important than spreadsheet safety.
    /// </summary>
    public static string ExportForSpreadsheet(IEnumerable<Contact> contacts) => ExportCore(contacts, spreadsheetSafe: true);

    private static string ExportCore(IEnumerable<Contact> contacts, bool spreadsheetSafe)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var sb = new StringBuilder();
        sb.AppendLine("GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes");
        foreach (var c in contacts)
        {
            ArgumentNullException.ThrowIfNull(c);
            string Encode(string value) => Escape(spreadsheetSafe ? NeutralizeSpreadsheetFormula(value) : value);
            sb.AppendLine(string.Join(',', new[]
            {
                Encode(c.GivenName), Encode(c.FamilyName), Encode(c.Nickname),
                Encode(c.Emails.FirstOrDefault()?.Address ?? ""), Encode(c.Phones.FirstOrDefault()?.Number ?? ""),
                Encode(c.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""), Encode(c.Notes)
            }));
        }
        return sb.ToString();
    }

    public static ImportResult Import(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = Parse(csv).ToList();
        if (rows.Count == 0) return new([], []);
        var header = rows[0].Select(x => x.Trim()).ToArray();
        var map = header.Select((name, index) => (name, index)).ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var contacts = new List<Contact>();
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            string Get(string name) => map.TryGetValue(name, out var i) && i < row.Count ? row[i].Trim() : string.Empty;
            var contact = new Contact { GivenName = Get("GivenName"), FamilyName = Get("FamilyName"), Nickname = Get("Nickname"), Notes = Get("Notes") };
            var email = Get("Email"); if (email.Length > 0) contact.Emails.Add(new(Guid.NewGuid(), "Email", email));
            var phone = Get("Phone"); if (phone.Length > 0) contact.Phones.Add(new(Guid.NewGuid(), "Phone", phone));
            var birthday = Get("Birthday");
            if (birthday.Length > 0)
            {
                if (DateOnly.TryParseExact(birthday, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) contact.Birthday = parsed;
                else warnings.Add($"Row {rowIndex + 1}: birthday was not yyyy-MM-dd.");
            }
            contacts.Add(contact);
        }
        return new(contacts, warnings);
    }

    private static string NeutralizeSpreadsheetFormula(string value)
    {
        if (value.Length == 0) return value;
        return IsSpreadsheetFormulaPrefix(value[0]) ? "'" + value : value;
    }

    private static bool IsSpreadsheetFormulaPrefix(char value) => value is
        '=' or '+' or '-' or '@' or '\t' or '\r' or '\n' or
        '\uFF1D' or '\uFF0B' or '\uFF0D' or '\uFF20';

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
            sb.AppendLine("BEGIN:VCARD"); sb.AppendLine("VERSION:4.0");
            sb.AppendLine($"N:{Esc(c.FamilyName)};{Esc(c.GivenName)};;;");
            sb.AppendLine($"FN:{Esc(c.DisplayName)}");
            foreach (var phone in c.Phones) sb.AppendLine($"TEL;TYPE={phone.Kind.ToString().ToLowerInvariant()}:{Esc(phone.Number)}");
            foreach (var email in c.Emails) sb.AppendLine($"EMAIL;TYPE={email.Kind.ToString().ToLowerInvariant()}:{Esc(email.Address)}");
            if (c.Birthday is not null) sb.AppendLine($"BDAY:{c.Birthday.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}");
            if (c.Notes.Length > 0) sb.AppendLine($"NOTE:{Esc(c.Notes)}");
            sb.AppendLine("END:VCARD");
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
            if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase)) { current = new Contact(); continue; }
            if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase)) { if (current is not null) contacts.Add(current); current = null; continue; }
            if (current is null) continue;
            var colon = line.IndexOf(':'); if (colon <= 0) continue;
            var key = line[..colon]; var property = key.Split(';', 2)[0]; var value = Unesc(line[(colon + 1)..]);
            if (property.Equals("FN", StringComparison.OrdinalIgnoreCase) && current.DisplayName == "Unnamed contact") current.GivenName = value;
            else if (property.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                var parts = value.Split(';');
                if (parts.Length > 0) current.FamilyName = parts[0];
                if (parts.Length > 1) current.GivenName = parts[1];
            }
            else if (property.Equals("TEL", StringComparison.OrdinalIgnoreCase)) current.Phones.Add(new(Guid.NewGuid(), "Imported", value));
            else if (property.Equals("EMAIL", StringComparison.OrdinalIgnoreCase)) current.Emails.Add(new(Guid.NewGuid(), "Imported", value));
            else if (property.Equals("BDAY", StringComparison.OrdinalIgnoreCase))
            {
                if (DateOnly.TryParseExact(value.Replace("-", "", StringComparison.Ordinal), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) current.Birthday = date;
                else warnings.Add($"Could not parse vCard birthday '{value}'.");
            }
            else if (property.Equals("NOTE", StringComparison.OrdinalIgnoreCase)) current.Notes = value;
        }
        if (current is not null) warnings.Add("A vCard did not contain END:VCARD and was ignored.");
        return new(contacts, warnings);
    }

    private static IEnumerable<string> Unfold(string text)
    {
        var output = new List<string>();
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && output.Count > 0) output[^1] += line[1..];
            else output.Add(line);
        }
        return output;
    }
    private static string Esc(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal).Replace(",", "\\,", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal);
    private static string Unesc(string value) => value.Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase).Replace("\\,", ",", StringComparison.Ordinal).Replace("\\;", ";", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
}
