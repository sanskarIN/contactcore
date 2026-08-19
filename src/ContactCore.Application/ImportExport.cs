using System.Text;
using System.Text.Json;
using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record ImportResult(
    IReadOnlyList<Contact> Contacts,
    IReadOnlyList<string> Warnings);

public static class ContactCsvCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Export(IEnumerable<Contact> contacts, bool spreadsheetSafe = true)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var sb = new StringBuilder();
        sb.AppendLine("GivenName,FamilyName,Nickname,Birthday,PrimaryEmail,PrimaryPhone,Notes,PhonesJson,EmailsJson,AddressesJson,OrganizationsJson,GroupsJson,TagsJson,Favorite,Archived");

        foreach (var contact in contacts)
        {
            var values = new[]
            {
                contact.GivenName,
                contact.FamilyName,
                contact.Nickname,
                contact.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty,
                contact.Emails.FirstOrDefault()?.Address ?? string.Empty,
                contact.Phones.FirstOrDefault()?.Number ?? string.Empty,
                contact.Notes,
                JsonSerializer.Serialize(contact.Phones, JsonOptions),
                JsonSerializer.Serialize(contact.Emails, JsonOptions),
                JsonSerializer.Serialize(contact.Addresses, JsonOptions),
                JsonSerializer.Serialize(contact.Organizations, JsonOptions),
                JsonSerializer.Serialize(contact.Groups, JsonOptions),
                JsonSerializer.Serialize(contact.Tags, JsonOptions),
                contact.IsFavorite ? "true" : "false",
                contact.IsArchived ? "true" : "false"
            };
            sb.AppendLine(string.Join(',', values.Select(value => Escape(value, spreadsheetSafe))));
        }

        return sb.ToString();
    }

    public static ImportResult Import(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var rows = Parse(csv).ToList();
        if (rows.Count == 0) return new([], []);

        var header = rows[0].Select(x => x.Trim()).ToArray();
        var map = header
            .Select((name, index) => (name, index))
            .GroupBy(x => x.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().index, StringComparer.OrdinalIgnoreCase);

        var warnings = new List<string>();
        var contacts = new List<Contact>();
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            string Get(string name, bool trim = true)
            {
                if (!map.TryGetValue(name, out var index) || index >= row.Count) return string.Empty;
                var value = UndoSpreadsheetNeutralization(row[index]);
                return trim ? value.Trim() : value;
            }

            string GetFirst(params string[] names)
            {
                foreach (var name in names)
                {
                    var value = Get(name);
                    if (value.Length > 0) return value;
                }
                return string.Empty;
            }

            var contact = new Contact
            {
                GivenName = Get("GivenName"),
                FamilyName = Get("FamilyName"),
                Nickname = Get("Nickname"),
                Notes = Get("Notes", trim: false),
                IsFavorite = ParseBoolean(Get("Favorite")),
                IsArchived = ParseBoolean(Get("Archived"))
            };

            var birthday = Get("Birthday");
            if (birthday.Length > 0)
            {
                if (DateOnly.TryParseExact(birthday, "yyyy-MM-dd", out var parsed))
                    contact.Birthday = parsed;
                else
                    warnings.Add($"Row {rowIndex + 1}: birthday was not yyyy-MM-dd.");
            }

            AddJson(contact.Phones, Get("PhonesJson", trim: false), "phones", rowIndex, warnings);
            AddJson(contact.Emails, Get("EmailsJson", trim: false), "emails", rowIndex, warnings);
            AddJson(contact.Addresses, Get("AddressesJson", trim: false), "addresses", rowIndex, warnings);
            AddJson(contact.Organizations, Get("OrganizationsJson", trim: false), "organizations", rowIndex, warnings);
            AddJson(contact.Groups, Get("GroupsJson", trim: false), "groups", rowIndex, warnings);
            AddJson(contact.Tags, Get("TagsJson", trim: false), "tags", rowIndex, warnings);

            // Backwards compatibility with the original ContactCore CSV columns and friendly
            // interoperability with simple spreadsheets that only provide one phone/email.
            if (contact.Emails.Count == 0)
            {
                var email = GetFirst("PrimaryEmail", "Email");
                if (email.Length > 0) contact.Emails.Add(new(Guid.NewGuid(), "Email", email));
            }
            if (contact.Phones.Count == 0)
            {
                var phone = GetFirst("PrimaryPhone", "Phone");
                if (phone.Length > 0) contact.Phones.Add(new(Guid.NewGuid(), "Phone", phone));
            }

            contacts.Add(contact);
        }

        return new(contacts, warnings);
    }

    private static void AddJson<T>(
        ICollection<T> target,
        string json,
        string fieldName,
        int zeroBasedRowIndex,
        ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var values = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
            if (values is null) return;
            foreach (var value in values) target.Add(value);
        }
        catch (JsonException)
        {
            warnings.Add($"Row {zeroBasedRowIndex + 1}: {fieldName} JSON could not be parsed; simple columns were used when available.");
        }
    }

    private static bool ParseBoolean(string value) =>
        bool.TryParse(value, out var parsed) && parsed;

    private static string Escape(string value, bool spreadsheetSafe)
    {
        if (spreadsheetSafe) value = NeutralizeSpreadsheetFormula(value);
        return '"' + value.Replace("\"", "\"\"") + '"';
    }

    private static string NeutralizeSpreadsheetFormula(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var firstMeaningful = value.AsSpan().TrimStart();
        if (firstMeaningful.Length == 0) return value;
        return firstMeaningful[0] is '=' or '+' or '-' or '@' ? "'" + value : value;
    }

    private static string UndoSpreadsheetNeutralization(string value)
    {
        if (value.Length < 2 || value[0] != '\'') return value;
        var remainder = value.AsSpan(1).TrimStart();
        return remainder.Length > 0 && remainder[0] is '=' or '+' or '-' or '@' ? value[1..] : value;
    }

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
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(ch);
                }
            }
            else if (ch == '"' && field.Length == 0)
            {
                quoted = true;
            }
            else if (ch == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (ch == '\r')
            {
                // CRLF and bare CR are normalized by ignoring CR and handling LF below.
            }
            else if (ch == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                yield return row;
                row = [];
            }
            else
            {
                field.Append(ch);
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }
}

public static class VCardCodec
{
    public static string Export(IEnumerable<Contact> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var sb = new StringBuilder();
        foreach (var contact in contacts)
        {
            sb.AppendLine("BEGIN:VCARD");
            sb.AppendLine("VERSION:4.0");
            sb.AppendLine($"N:{Esc(contact.FamilyName)};{Esc(contact.GivenName)};;;");
            sb.AppendLine($"FN:{Esc(contact.DisplayName)}");
            if (contact.Nickname.Length > 0) sb.AppendLine($"NICKNAME:{Esc(contact.Nickname)}");

            foreach (var phone in contact.Phones)
                sb.AppendLine($"TEL;TYPE={phone.Kind.ToString().ToLowerInvariant()}:{Esc(phone.Number)}");
            foreach (var email in contact.Emails)
                sb.AppendLine($"EMAIL;TYPE={email.Kind.ToString().ToLowerInvariant()}:{Esc(email.Address)}");
            foreach (var address in contact.Addresses)
                sb.AppendLine($"ADR;TYPE={ParameterToken(address.Label)}:;;{Esc(address.Street)};{Esc(address.City)};{Esc(address.Region)};{Esc(address.PostalCode)};{Esc(address.Country)}");
            foreach (var organization in contact.Organizations)
            {
                sb.AppendLine($"ORG:{Esc(organization.Name)};{Esc(organization.Department ?? string.Empty)}");
                if (!string.IsNullOrWhiteSpace(organization.Title)) sb.AppendLine($"TITLE:{Esc(organization.Title)}");
            }

            if (contact.Birthday is not null) sb.AppendLine($"BDAY:{contact.Birthday:yyyyMMdd}");
            if (contact.Tags.Count > 0) sb.AppendLine($"CATEGORIES:{string.Join(',', contact.Tags.Select(x => Esc(x.Name)))}");
            if (contact.Groups.Count > 0) sb.AppendLine($"X-CONTACTCORE-GROUPS:{string.Join(',', contact.Groups.Select(x => Esc(x.Name)))}");
            if (contact.IsFavorite) sb.AppendLine("X-CONTACTCORE-FAVORITE:TRUE");
            if (contact.IsArchived) sb.AppendLine("X-CONTACTCORE-ARCHIVED:TRUE");
            if (contact.Notes.Length > 0) sb.AppendLine($"NOTE:{Esc(contact.Notes)}");
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
        var lastOrganizationIndex = -1;

        foreach (var raw in Unfold(text))
        {
            var line = raw.TrimEnd('\r');
            if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null)
                    warnings.Add("A nested BEGIN:VCARD discarded the preceding incomplete card.");
                current = new Contact();
                lastOrganizationIndex = -1;
                continue;
            }

            if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) contacts.Add(current);
                current = null;
                lastOrganizationIndex = -1;
                continue;
            }

            if (current is null) continue;
            var colon = FindUnescaped(line, ':');
            if (colon <= 0) continue;

            var key = line[..colon];
            var property = key.Split(';', 2)[0];
            var rawValue = line[(colon + 1)..];

            if (property.Equals("FN", StringComparison.OrdinalIgnoreCase))
            {
                var value = Unesc(rawValue);
                if (current.DisplayName == "Unnamed contact") current.GivenName = value;
            }
            else if (property.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                var parts = SplitEscaped(rawValue, ';').Select(Unesc).ToArray();
                if (parts.Length > 0) current.FamilyName = parts[0];
                if (parts.Length > 1) current.GivenName = parts[1];
            }
            else if (property.Equals("NICKNAME", StringComparison.OrdinalIgnoreCase))
            {
                current.Nickname = Unesc(rawValue);
            }
            else if (property.Equals("TEL", StringComparison.OrdinalIgnoreCase))
            {
                current.Phones.Add(new(
                    Guid.NewGuid(),
                    ReadTypeLabel(key, "Imported"),
                    Unesc(rawValue),
                    ReadFieldKind(key, ContactFieldKind.Mobile)));
            }
            else if (property.Equals("EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                current.Emails.Add(new(
                    Guid.NewGuid(),
                    ReadTypeLabel(key, "Imported"),
                    Unesc(rawValue),
                    ReadFieldKind(key, ContactFieldKind.Home)));
            }
            else if (property.Equals("ADR", StringComparison.OrdinalIgnoreCase))
            {
                var parts = SplitEscaped(rawValue, ';').Select(Unesc).ToArray();
                string Part(int index) => index < parts.Length ? parts[index] : string.Empty;
                current.Addresses.Add(new(
                    Guid.NewGuid(),
                    ReadTypeLabel(key, "Imported"),
                    Part(2), Part(3), Part(4), Part(5), Part(6)));
            }
            else if (property.Equals("ORG", StringComparison.OrdinalIgnoreCase))
            {
                var parts = SplitEscaped(rawValue, ';').Select(Unesc).ToArray();
                var organization = new ContactOrganization(
                    Guid.NewGuid(),
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    null,
                    NullIfBlank(parts.ElementAtOrDefault(1)));
                current.Organizations.Add(organization);
                lastOrganizationIndex = current.Organizations.Count - 1;
            }
            else if (property.Equals("TITLE", StringComparison.OrdinalIgnoreCase) && lastOrganizationIndex >= 0)
            {
                var existing = current.Organizations[lastOrganizationIndex];
                current.Organizations[lastOrganizationIndex] = existing with { Title = NullIfBlank(Unesc(rawValue)) };
            }
            else if (property.Equals("BDAY", StringComparison.OrdinalIgnoreCase))
            {
                var value = Unesc(rawValue);
                if (DateOnly.TryParseExact(value.Replace("-", string.Empty), "yyyyMMdd", out var date))
                    current.Birthday = date;
                else
                    warnings.Add($"Could not parse vCard birthday '{value}'.");
            }
            else if (property.Equals("CATEGORIES", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in SplitEscaped(rawValue, ',').Select(Unesc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                    current.Tags.Add(new(Guid.NewGuid(), name.Trim()));
            }
            else if (property.Equals("X-CONTACTCORE-GROUPS", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var name in SplitEscaped(rawValue, ',').Select(Unesc).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
                    current.Groups.Add(new(Guid.NewGuid(), name.Trim()));
            }
            else if (property.Equals("X-CONTACTCORE-FAVORITE", StringComparison.OrdinalIgnoreCase))
            {
                current.IsFavorite = rawValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }
            else if (property.Equals("X-CONTACTCORE-ARCHIVED", StringComparison.OrdinalIgnoreCase))
            {
                current.IsArchived = rawValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            }
            else if (property.Equals("NOTE", StringComparison.OrdinalIgnoreCase))
            {
                current.Notes = Unesc(rawValue);
            }
        }

        if (current is not null)
            warnings.Add("A vCard did not contain END:VCARD and was ignored.");

        return new(contacts, warnings);
    }

    private static IEnumerable<string> Unfold(string text)
    {
        var output = new List<string>();
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && output.Count > 0)
                output[^1] += line[1..];
            else
                output.Add(line);
        }
        return output;
    }

    private static int FindUnescaped(string value, char delimiter)
    {
        var escaped = false;
        for (var i = 0; i < value.Length; i++)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }
            if (value[i] == '\\')
            {
                escaped = true;
                continue;
            }
            if (value[i] == delimiter) return i;
        }
        return -1;
    }

    private static IReadOnlyList<string> SplitEscaped(string value, char delimiter)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (ch == '\\' && i + 1 < value.Length)
            {
                current.Append(ch);
                current.Append(value[++i]);
            }
            else if (ch == delimiter)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        result.Add(current.ToString());
        return result;
    }

    private static string Esc(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r\n", "\\n", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\r", "\\n", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal);

    private static string Unesc(string value)
    {
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                result.Append(value[i]);
                continue;
            }

            var next = value[++i];
            result.Append(next switch
            {
                'n' or 'N' => '\n',
                '\\' => '\\',
                ',' => ',',
                ';' => ';',
                _ => next
            });
        }
        return result.ToString();
    }

    private static string ParameterToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "other";
        var token = new string(value
            .Where(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
            .Take(32)
            .ToArray());
        return token.Length == 0 ? "other" : token.ToLowerInvariant();
    }

    private static string ReadTypeLabel(string key, string fallback)
    {
        foreach (var part in key.Split(';').Skip(1))
        {
            if (part.StartsWith("TYPE=", StringComparison.OrdinalIgnoreCase))
            {
                var value = part[5..].Split(',')[0].Trim();
                if (value.Length > 0) return value;
            }
        }
        return fallback;
    }

    private static ContactFieldKind ReadFieldKind(string key, ContactFieldKind fallback)
    {
        var label = ReadTypeLabel(key, string.Empty);
        return Enum.TryParse<ContactFieldKind>(label, ignoreCase: true, out var kind) ? kind : fallback;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
