using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteContactRepository(SqliteConnectionFactory factory, DatabaseMigrator migrator) : IContactRepository
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => migrator.ApplyAsync(cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM contacts;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var contacts = await LoadContactsAsync(connection, "WHERE c.id = $id", [new("$id", id.ToString())], cancellationToken).ConfigureAwait(false);
        return contacts.SingleOrDefault();
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var clauses = new List<string>();
        var parameters = new List<SqliteParameter>();
        if (!query.IncludeArchived) clauses.Add("c.is_archived = 0");
        if (query.FavoritesOnly) clauses.Add("c.is_favorite = 1");
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            clauses.Add("(c.given_name LIKE $search ESCAPE '\\' OR c.family_name LIKE $search ESCAPE '\\' OR c.nickname LIKE $search ESCAPE '\\' OR EXISTS(SELECT 1 FROM phones p WHERE p.contact_id=c.id AND p.number LIKE $search ESCAPE '\\') OR EXISTS(SELECT 1 FROM emails e WHERE e.contact_id=c.id AND e.address LIKE $search ESCAPE '\\'))");
            parameters.Add(new("$search", "%" + EscapeLike(query.Search.Trim()) + "%"));
        }
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            clauses.Add("EXISTS(SELECT 1 FROM contact_tags ct JOIN tags t ON t.id=ct.tag_id WHERE ct.contact_id=c.id AND t.name=$tag COLLATE NOCASE)");
            parameters.Add(new("$tag", query.Tag.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(query.Group))
        {
            clauses.Add("EXISTS(SELECT 1 FROM contact_groups cg JOIN groups g ON g.id=cg.group_id WHERE cg.contact_id=c.id AND g.name=$group COLLATE NOCASE)");
            parameters.Add(new("$group", query.Group.Trim()));
        }
        if (query.StartsWith is { } startsWith)
        {
            clauses.Add("COALESCE(NULLIF(c.family_name,''), NULLIF(c.given_name,''), c.nickname) LIKE $starts ESCAPE '\\'");
            parameters.Add(new("$starts", EscapeLike(startsWith.ToString()) + "%"));
        }
        var where = clauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", clauses);
        return await LoadContactsAsync(connection, where + " ORDER BY c.family_name COLLATE NOCASE, c.given_name COLLATE NOCASE, c.nickname COLLATE NOCASE", parameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ExecAsync(connection, tx, """
                INSERT INTO contacts(id,given_name,family_name,nickname,birthday,notes,is_favorite,is_archived,created_at,updated_at)
                VALUES($id,$given,$family,$nickname,$birthday,$notes,$favorite,$archived,$created,$updated)
                ON CONFLICT(id) DO UPDATE SET given_name=excluded.given_name,family_name=excluded.family_name,nickname=excluded.nickname,birthday=excluded.birthday,notes=excluded.notes,is_favorite=excluded.is_favorite,is_archived=excluded.is_archived,updated_at=excluded.updated_at;
                """, cancellationToken,
                ("$id", contact.Id.ToString()), ("$given", contact.GivenName), ("$family", contact.FamilyName), ("$nickname", contact.Nickname),
                ("$birthday", contact.Birthday?.ToString("yyyy-MM-dd")), ("$notes", contact.Notes), ("$favorite", contact.IsFavorite ? 1 : 0),
                ("$archived", contact.IsArchived ? 1 : 0), ("$created", contact.CreatedAt.ToString("O")), ("$updated", contact.UpdatedAt.ToString("O"))).ConfigureAwait(false);

            foreach (var table in new[] { "phones", "emails", "addresses", "organizations", "contact_groups", "contact_tags" })
                await ExecAsync(connection, tx, $"DELETE FROM {table} WHERE contact_id=$id;", cancellationToken, ("$id", contact.Id.ToString())).ConfigureAwait(false);

            foreach (var p in contact.Phones)
                await ExecAsync(connection, tx, "INSERT INTO phones(id,contact_id,label,number,kind) VALUES($id,$contact,$label,$number,$kind);", cancellationToken, ("$id", p.Id.ToString()), ("$contact", contact.Id.ToString()), ("$label", p.Label), ("$number", p.Number), ("$kind", (int)p.Kind)).ConfigureAwait(false);
            foreach (var e in contact.Emails)
                await ExecAsync(connection, tx, "INSERT INTO emails(id,contact_id,label,address,kind) VALUES($id,$contact,$label,$address,$kind);", cancellationToken, ("$id", e.Id.ToString()), ("$contact", contact.Id.ToString()), ("$label", e.Label), ("$address", e.Address), ("$kind", (int)e.Kind)).ConfigureAwait(false);
            foreach (var a in contact.Addresses)
                await ExecAsync(connection, tx, "INSERT INTO addresses(id,contact_id,label,street,city,region,postal_code,country) VALUES($id,$contact,$label,$street,$city,$region,$postal,$country);", cancellationToken, ("$id", a.Id.ToString()), ("$contact", contact.Id.ToString()), ("$label", a.Label), ("$street", a.Street), ("$city", a.City), ("$region", a.Region), ("$postal", a.PostalCode), ("$country", a.Country)).ConfigureAwait(false);
            foreach (var o in contact.Organizations)
                await ExecAsync(connection, tx, "INSERT INTO organizations(id,contact_id,name,title,department) VALUES($id,$contact,$name,$title,$department);", cancellationToken, ("$id", o.Id.ToString()), ("$contact", contact.Id.ToString()), ("$name", o.Name), ("$title", o.Title), ("$department", o.Department)).ConfigureAwait(false);
            foreach (var group in contact.Groups)
            {
                await ExecAsync(connection, tx, "INSERT INTO groups(id,name) VALUES($id,$name) ON CONFLICT(name) DO NOTHING;", cancellationToken, ("$id", group.Id.ToString()), ("$name", group.Name)).ConfigureAwait(false);
                await ExecAsync(connection, tx, "INSERT OR IGNORE INTO contact_groups(contact_id,group_id) SELECT $contact,id FROM groups WHERE name=$name COLLATE NOCASE;", cancellationToken, ("$contact", contact.Id.ToString()), ("$name", group.Name)).ConfigureAwait(false);
            }
            foreach (var tag in contact.Tags)
            {
                await ExecAsync(connection, tx, "INSERT INTO tags(id,name) VALUES($id,$name) ON CONFLICT(name) DO NOTHING;", cancellationToken, ("$id", tag.Id.ToString()), ("$name", tag.Name)).ConfigureAwait(false);
                await ExecAsync(connection, tx, "INSERT OR IGNORE INTO contact_tags(contact_id,tag_id) SELECT $contact,id FROM tags WHERE name=$name COLLATE NOCASE;", cancellationToken, ("$contact", contact.Id.ToString()), ("$name", tag.Name)).ConfigureAwait(false);
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM contacts WHERE id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<Contact>> LoadContactsAsync(SqliteConnection connection, string suffix, IReadOnlyList<SqliteParameter> parameters, CancellationToken cancellationToken)
    {
        var contacts = new List<Contact>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT c.id,c.given_name,c.family_name,c.nickname,c.birthday,c.notes,c.is_favorite,c.is_archived,c.created_at,c.updated_at FROM contacts c " + suffix;
            foreach (var p in parameters) cmd.Parameters.Add(new SqliteParameter(p.ParameterName, p.Value));
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                contacts.Add(new Contact
                {
                    Id = Guid.Parse(reader.GetString(0)), GivenName = reader.GetString(1), FamilyName = reader.GetString(2), Nickname = reader.GetString(3),
                    Birthday = reader.IsDBNull(4) ? null : DateOnly.ParseExact(reader.GetString(4), "yyyy-MM-dd"), Notes = reader.GetString(5),
                    IsFavorite = reader.GetInt32(6) != 0, IsArchived = reader.GetInt32(7) != 0,
                    CreatedAt = DateTimeOffset.Parse(reader.GetString(8), System.Globalization.CultureInfo.InvariantCulture),
                    UpdatedAt = DateTimeOffset.Parse(reader.GetString(9), System.Globalization.CultureInfo.InvariantCulture)
                });
            }
        }
        foreach (var contact in contacts) await LoadChildrenAsync(connection, contact, cancellationToken).ConfigureAwait(false);
        return contacts;
    }

    private static async Task LoadChildrenAsync(SqliteConnection connection, Contact contact, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Parameters.AddWithValue("$id", contact.Id.ToString());
        cmd.CommandText = "SELECT id,label,number,kind FROM phones WHERE contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Phones.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), (ContactFieldKind)reader.GetInt32(3)));
        cmd.CommandText = "SELECT id,label,address,kind FROM emails WHERE contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Emails.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), (ContactFieldKind)reader.GetInt32(3)));
        cmd.CommandText = "SELECT id,label,street,city,region,postal_code,country FROM addresses WHERE contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Addresses.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6)));
        cmd.CommandText = "SELECT id,name,title,department FROM organizations WHERE contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Organizations.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        cmd.CommandText = "SELECT g.id,g.name FROM groups g JOIN contact_groups cg ON cg.group_id=g.id WHERE cg.contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Groups.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1)));
        cmd.CommandText = "SELECT t.id,t.name FROM tags t JOIN contact_tags ct ON ct.tag_id=t.id WHERE ct.contact_id=$id;";
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) contact.Tags.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1)));
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private static async Task ExecAsync(SqliteConnection connection, System.Data.Common.DbTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] values)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = sql;
        foreach (var (name, value) in values) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
