using System.Globalization;
using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteContactRepository(SqliteDatabase database) : IContactRepository
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public async Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(connection, id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT c.id
            FROM contacts c
            WHERE ($includeArchived = 1 OR c.is_archived = 0)
              AND ($favoritesOnly = 0 OR c.is_favorite = 1)
              AND (
                    $search = ''
                 OR c.given_name LIKE $pattern ESCAPE '\' COLLATE NOCASE
                 OR c.family_name LIKE $pattern ESCAPE '\' COLLATE NOCASE
                 OR c.nickname LIKE $pattern ESCAPE '\' COLLATE NOCASE
                 OR EXISTS (SELECT 1 FROM emails e WHERE e.contact_id = c.id AND e.address LIKE $pattern ESCAPE '\' COLLATE NOCASE)
                 OR EXISTS (SELECT 1 FROM phones p WHERE p.contact_id = c.id AND p.number LIKE $pattern ESCAPE '\' COLLATE NOCASE)
                 OR EXISTS (SELECT 1 FROM organizations o WHERE o.contact_id = c.id AND (o.name LIKE $pattern ESCAPE '\' COLLATE NOCASE OR o.title LIKE $pattern ESCAPE '\' COLLATE NOCASE))
                 OR EXISTS (SELECT 1 FROM contact_tags ct JOIN tags t ON t.id = ct.tag_id WHERE ct.contact_id = c.id AND t.name LIKE $pattern ESCAPE '\' COLLATE NOCASE)
                 OR EXISTS (SELECT 1 FROM contact_groups cg JOIN groups g ON g.id = cg.group_id WHERE cg.contact_id = c.id AND g.name LIKE $pattern ESCAPE '\' COLLATE NOCASE)
              )
            ORDER BY c.family_name COLLATE NOCASE, c.given_name COLLATE NOCASE, c.id
            LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$includeArchived", query.IncludeArchived ? 1 : 0);
        command.Parameters.AddWithValue("$favoritesOnly", query.FavoritesOnly ? 1 : 0);
        command.Parameters.AddWithValue("$search", query.SearchText);
        command.Parameters.AddWithValue("$pattern", $"%{EscapeLike(query.SearchText)}%");
        command.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 1, 1000));
        command.Parameters.AddWithValue("$offset", Math.Max(0, query.Offset));

        var ids = new List<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                ids.Add(Guid.Parse(reader.GetString(0)));
        }

        var contacts = new List<Contact>(ids.Count);
        foreach (var id in ids)
        {
            var contact = await GetAsync(connection, id, cancellationToken).ConfigureAwait(false);
            if (contact is not null) contacts.Add(contact);
        }
        return contacts;
    }

    public async Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM contacts ORDER BY family_name COLLATE NOCASE, given_name COLLATE NOCASE, id;";
        var ids = new List<Guid>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) ids.Add(Guid.Parse(reader.GetString(0)));
        }

        var contacts = new List<Contact>(ids.Count);
        foreach (var id in ids)
        {
            var contact = await GetAsync(connection, id, cancellationToken).ConfigureAwait(false);
            if (contact is not null) contacts.Add(contact);
        }
        return contacts;
    }

    public async Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO contacts(id, given_name, family_name, nickname, birthday, notes, is_favorite, is_archived, created_utc, updated_utc)
                VALUES($id, $given, $family, $nickname, $birthday, $notes, $favorite, $archived, $created, $updated)
                ON CONFLICT(id) DO UPDATE SET
                    given_name = excluded.given_name,
                    family_name = excluded.family_name,
                    nickname = excluded.nickname,
                    birthday = excluded.birthday,
                    notes = excluded.notes,
                    is_favorite = excluded.is_favorite,
                    is_archived = excluded.is_archived,
                    updated_utc = excluded.updated_utc;
                """;
            command.Parameters.AddWithValue("$id", contact.Id.ToString("D"));
            command.Parameters.AddWithValue("$given", contact.GivenName);
            command.Parameters.AddWithValue("$family", contact.FamilyName);
            command.Parameters.AddWithValue("$nickname", contact.Nickname);
            command.Parameters.AddWithValue("$birthday", (object?)contact.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$notes", contact.Notes);
            command.Parameters.AddWithValue("$favorite", contact.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("$archived", contact.IsArchived ? 1 : 0);
            command.Parameters.AddWithValue("$created", contact.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$updated", contact.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await ClearChildrenAsync(connection, transaction, contact.Id, cancellationToken).ConfigureAwait(false);
        await InsertPhonesAsync(connection, transaction, contact, cancellationToken).ConfigureAwait(false);
        await InsertEmailsAsync(connection, transaction, contact, cancellationToken).ConfigureAwait(false);
        await InsertAddressesAsync(connection, transaction, contact, cancellationToken).ConfigureAwait(false);
        await InsertOrganizationsAsync(connection, transaction, contact, cancellationToken).ConfigureAwait(false);
        await InsertNamedLinksAsync(connection, transaction, contact.Id, "groups", "contact_groups", "group_id", contact.Groups.Select(g => (g.Id, g.Name)), cancellationToken).ConfigureAwait(false);
        await InsertNamedLinksAsync(connection, transaction, contact.Id, "tags", "contact_tags", "tag_id", contact.Tags.Select(t => (t.Id, t.Name)), cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM contacts WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM contacts;";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task<Contact?> GetAsync(SqliteConnection connection, Guid id, CancellationToken cancellationToken)
    {
        Contact? contact;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, given_name, family_name, nickname, birthday, notes, is_favorite, is_archived, created_utc, updated_utc
                FROM contacts WHERE id = $id;
                """;
            command.Parameters.AddWithValue("$id", id.ToString("D"));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            contact = new Contact
            {
                Id = Guid.Parse(reader.GetString(0)),
                GivenName = reader.GetString(1),
                FamilyName = reader.GetString(2),
                Nickname = reader.GetString(3),
                Birthday = reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Notes = reader.GetString(5),
                IsFavorite = reader.GetInt32(6) != 0,
                IsArchived = reader.GetInt32(7) != 0,
                CreatedAt = DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };
        }

        await LoadPhonesAsync(connection, contact, cancellationToken).ConfigureAwait(false);
        await LoadEmailsAsync(connection, contact, cancellationToken).ConfigureAwait(false);
        await LoadAddressesAsync(connection, contact, cancellationToken).ConfigureAwait(false);
        await LoadOrganizationsAsync(connection, contact, cancellationToken).ConfigureAwait(false);
        await LoadNamedLinksAsync(connection, contact.Id, "groups", "contact_groups", "group_id", (idValue, name) => contact.Groups.Add(new(idValue, name)), cancellationToken).ConfigureAwait(false);
        await LoadNamedLinksAsync(connection, contact.Id, "tags", "contact_tags", "tag_id", (idValue, name) => contact.Tags.Add(new(idValue, name)), cancellationToken).ConfigureAwait(false);
        return contact;
    }

    private static async Task ClearChildrenAsync(SqliteConnection connection, SqliteTransaction transaction, Guid contactId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM phones WHERE contact_id = $id;
            DELETE FROM emails WHERE contact_id = $id;
            DELETE FROM addresses WHERE contact_id = $id;
            DELETE FROM organizations WHERE contact_id = $id;
            DELETE FROM contact_groups WHERE contact_id = $id;
            DELETE FROM contact_tags WHERE contact_id = $id;
            """;
        command.Parameters.AddWithValue("$id", contactId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task InsertPhonesAsync(SqliteConnection connection, SqliteTransaction transaction, Contact contact, CancellationToken cancellationToken)
    {
        foreach (var item in contact.Phones)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO phones(id, contact_id, label, number, kind) VALUES($id,$contact,$label,$number,$kind);";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D")); command.Parameters.AddWithValue("$contact", contact.Id.ToString("D"));
            command.Parameters.AddWithValue("$label", item.Label); command.Parameters.AddWithValue("$number", item.Number); command.Parameters.AddWithValue("$kind", (int)item.Kind);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InsertEmailsAsync(SqliteConnection connection, SqliteTransaction transaction, Contact contact, CancellationToken cancellationToken)
    {
        foreach (var item in contact.Emails)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO emails(id, contact_id, label, address, kind) VALUES($id,$contact,$label,$address,$kind);";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D")); command.Parameters.AddWithValue("$contact", contact.Id.ToString("D"));
            command.Parameters.AddWithValue("$label", item.Label); command.Parameters.AddWithValue("$address", item.Address); command.Parameters.AddWithValue("$kind", (int)item.Kind);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InsertAddressesAsync(SqliteConnection connection, SqliteTransaction transaction, Contact contact, CancellationToken cancellationToken)
    {
        foreach (var item in contact.Addresses)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO addresses(id, contact_id, label, street, city, region, postal_code, country) VALUES($id,$contact,$label,$street,$city,$region,$postal,$country);";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D")); command.Parameters.AddWithValue("$contact", contact.Id.ToString("D")); command.Parameters.AddWithValue("$label", item.Label);
            command.Parameters.AddWithValue("$street", item.Street); command.Parameters.AddWithValue("$city", item.City); command.Parameters.AddWithValue("$region", item.Region); command.Parameters.AddWithValue("$postal", item.PostalCode); command.Parameters.AddWithValue("$country", item.Country);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InsertOrganizationsAsync(SqliteConnection connection, SqliteTransaction transaction, Contact contact, CancellationToken cancellationToken)
    {
        foreach (var item in contact.Organizations)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO organizations(id, contact_id, name, title, department) VALUES($id,$contact,$name,$title,$department);";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D")); command.Parameters.AddWithValue("$contact", contact.Id.ToString("D")); command.Parameters.AddWithValue("$name", item.Name);
            command.Parameters.AddWithValue("$title", (object?)item.Title ?? DBNull.Value); command.Parameters.AddWithValue("$department", (object?)item.Department ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InsertNamedLinksAsync(SqliteConnection connection, SqliteTransaction transaction, Guid contactId, string itemTable, string linkTable, string itemIdColumn, IEnumerable<(Guid Id, string Name)> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            await using var insert = connection.CreateCommand(); insert.Transaction = transaction;
            insert.CommandText = $"INSERT OR IGNORE INTO {itemTable}(id, name) VALUES($id, $name);";
            insert.Parameters.AddWithValue("$id", item.Id.ToString("D")); insert.Parameters.AddWithValue("$name", item.Name);
            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var find = connection.CreateCommand(); find.Transaction = transaction;
            find.CommandText = $"SELECT id FROM {itemTable} WHERE name = $name COLLATE NOCASE LIMIT 1;";
            find.Parameters.AddWithValue("$name", item.Name);
            var storedId = Convert.ToString(await find.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException($"Unable to resolve {itemTable} value.");

            await using var link = connection.CreateCommand(); link.Transaction = transaction;
            link.CommandText = $"INSERT OR IGNORE INTO {linkTable}(contact_id, {itemIdColumn}) VALUES($contact, $item);";
            link.Parameters.AddWithValue("$contact", contactId.ToString("D")); link.Parameters.AddWithValue("$item", storedId);
            await link.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task LoadPhonesAsync(SqliteConnection connection, Contact contact, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT id,label,number,kind FROM phones WHERE contact_id=$id ORDER BY rowid;"; command.Parameters.AddWithValue("$id", contact.Id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Phones.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), (ContactFieldKind)reader.GetInt32(3)));
    }

    private static async Task LoadEmailsAsync(SqliteConnection connection, Contact contact, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT id,label,address,kind FROM emails WHERE contact_id=$id ORDER BY rowid;"; command.Parameters.AddWithValue("$id", contact.Id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Emails.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), (ContactFieldKind)reader.GetInt32(3)));
    }

    private static async Task LoadAddressesAsync(SqliteConnection connection, Contact contact, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT id,label,street,city,region,postal_code,country FROM addresses WHERE contact_id=$id ORDER BY rowid;"; command.Parameters.AddWithValue("$id", contact.Id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Addresses.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6)));
    }

    private static async Task LoadOrganizationsAsync(SqliteConnection connection, Contact contact, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT id,name,title,department FROM organizations WHERE contact_id=$id ORDER BY rowid;"; command.Parameters.AddWithValue("$id", contact.Id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Organizations.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
    }

    private static async Task LoadNamedLinksAsync(SqliteConnection connection, Guid contactId, string itemTable, string linkTable, string itemIdColumn, Action<Guid, string> add, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT i.id, i.name FROM {linkTable} l JOIN {itemTable} i ON i.id=l.{itemIdColumn} WHERE l.contact_id=$id ORDER BY i.name COLLATE NOCASE;";
        command.Parameters.AddWithValue("$id", contactId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) add(Guid.Parse(reader.GetString(0)), reader.GetString(1));
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
