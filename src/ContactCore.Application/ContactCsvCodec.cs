using System.Globalization;
using System.Text;
using ContactCore.Domain;

namespace ContactCore.Application;

public static class ContactCsvCodec
{
    private static readonly string[] Headers =
    [
        "id", "given_name", "family_name", "nickname", "birthday", "emails", "phones", "notes", "favorite", "archived"
    ];

    public static string Export(IEnumerable<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(',', Headers));
        foreach (var contact in contacts.OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            var row = new[]
            {
                contact.Id.ToString("D", CultureInfo.InvariantCulture),
                contact.GivenName,
                contact.FamilyName,
                contact.Nickname,
                contact.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                string.Join(';', contact.Emails.Select(e => e.Address)),
                string.Join(';', contact.Phones.Select(p => p.Number)),
                contact.Notes,
                contact.IsFavorite ? "true" : "false",
                contact.IsArchived ? "true" : "false"
            };
            builder.AppendLine(string.Join(',', row.Select(Escape)));
        }
        return builder.ToString();
    }

    public static IReadOnlyList<Contact> Import(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = ParseRows(csv);
        if (rows.Count == 0) return [];

        var headerMap = rows[0]
            .Select((value, index) => (value: value.Trim().ToLowerInvariant(), index))
            .ToDictionary(pair => pair.value, pair => pair.index, StringComparer.Ordinal);
        foreach (var required in new[] { "given_name", "family_name" })
            if (!headerMap.ContainsKey(required)) throw new FormatException($"CSV is missing required column '{required}'.");

        var contacts = new List<Contact>();
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;
            var idText = Value(row, headerMap, "id");
            var birthdayText = Value(row, headerMap, "birthday");
            DateOnly? birthday = null;
            if (birthdayText.Length > 0)
            {
                if (!DateOnly.TryParseExact(birthdayText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    throw new FormatException($"Invalid birthday at CSV row {rowIndex + 1}.");
                birthday = parsed;
            }

            var contact = new Contact
            {
                Id = Guid.TryParse(idText, out var id) ? id : Guid.NewGuid(),
                GivenName = Value(row, headerMap, "given_name"),
                FamilyName = Value(row, headerMap, "family_name"),
                Nickname = Value(row, headerMap, "nickname"),
                Birthday = birthday,
                Notes = Value(row, headerMap, "notes"),
                IsFavorite = bool.TryParse(Value(row, headerMap, "favorite"), out var favorite) && favorite,
                IsArchived = bool.TryParse(Value(row, headerMap, "archived"), out var archived) && archived
            };

            foreach (var email in SplitMany(Value(row, headerMap, "emails")))
                contact.Emails.Add(new(Guid.NewGuid(), "Imported", email));
            foreach (var phone in SplitMany(Value(row, headerMap, "phones")))
                contact.Phones.Add(new(Guid.NewGuid(), "Imported", phone));
            contacts.Add(contact);
        }
        return contacts;
    }

    private static string Value(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> map, string name) =>
        map.TryGetValue(name, out var index) && index < row.Count ? row[index].Trim() : string.Empty;

    private static IEnumerable<string> SplitMany(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Escape(string value)
    {
        if (!value.ContainsAny([',', '"', '\r', '\n'])) return value;
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static List<List<string>> ParseRows(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < csv.Length && csv[i + 1] == '"') { field.Append('"'); i++; }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
                continue;
            }

            if (ch == '"' && field.Length == 0) quoted = true;
            else if (ch == ',') { row.Add(field.ToString()); field.Clear(); }
            else if (ch is '\r' or '\n')
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                row.Add(field.ToString()); field.Clear();
                rows.Add(row); row = [];
            }
            else field.Append(ch);
        }

        if (quoted) throw new FormatException("CSV contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
        return rows;
    }
}
