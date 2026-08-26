using ContactCore.Domain;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class SqliteOrderingTests
{
    private string _dir = null!;
    private string _databasePath = null!;
    private SqliteConnectionFactory _factory = null!;
    private SqliteContactRepository _repository = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-ordering-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _databasePath = Path.Combine(_dir, "ordering.db");
        _factory = new SqliteConnectionFactory(_databasePath);
        _repository = new SqliteContactRepository(_factory, new DatabaseMigrator(_factory));
        await _repository.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_dir, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public async Task Repeated_field_order_round_trips_and_survives_reordering()
    {
        var contact = new Contact { GivenName = "Ordered", FamilyName = "Contact" };
        var phone1 = new ContactPhone(Guid.NewGuid(), "First", "1111111111", ContactFieldKind.Mobile);
        var phone2 = new ContactPhone(Guid.NewGuid(), "Second", "2222222222", ContactFieldKind.Work);
        var email1 = new ContactEmail(Guid.NewGuid(), "First", "first@example.test", ContactFieldKind.Home);
        var email2 = new ContactEmail(Guid.NewGuid(), "Second", "second@example.test", ContactFieldKind.Work);
        var address1 = new ContactAddress(Guid.NewGuid(), "First", "1 First Street", "First City", "R1", "100001", "Exampleland");
        var address2 = new ContactAddress(Guid.NewGuid(), "Second", "2 Second Street", "Second City", "R2", "200002", "Exampleland");
        var organization1 = new ContactOrganization(Guid.NewGuid(), "First Org", "Engineer", "One");
        var organization2 = new ContactOrganization(Guid.NewGuid(), "Second Org", "Manager", "Two");
        var group1 = new ContactGroup(Guid.NewGuid(), "First Group");
        var group2 = new ContactGroup(Guid.NewGuid(), "Second Group");
        var tag1 = new ContactTag(Guid.NewGuid(), "First Tag");
        var tag2 = new ContactTag(Guid.NewGuid(), "Second Tag");

        contact.Phones.AddRange([phone1, phone2]);
        contact.Emails.AddRange([email1, email2]);
        contact.Addresses.AddRange([address1, address2]);
        contact.Organizations.AddRange([organization1, organization2]);
        contact.Groups.AddRange([group1, group2]);
        contact.Tags.AddRange([tag1, tag2]);

        await _repository.UpsertAsync(contact);
        var firstLoad = await _repository.GetAsync(contact.Id);

        Assert.IsNotNull(firstLoad);
        CollectionAssert.AreEqual(new[] { phone1, phone2 }, firstLoad.Phones.ToArray());
        CollectionAssert.AreEqual(new[] { email1, email2 }, firstLoad.Emails.ToArray());
        CollectionAssert.AreEqual(new[] { address1, address2 }, firstLoad.Addresses.ToArray());
        CollectionAssert.AreEqual(new[] { organization1, organization2 }, firstLoad.Organizations.ToArray());
        CollectionAssert.AreEqual(new[] { group1.Name, group2.Name }, firstLoad.Groups.Select(x => x.Name).ToArray());
        CollectionAssert.AreEqual(new[] { tag1.Name, tag2.Name }, firstLoad.Tags.Select(x => x.Name).ToArray());

        Reverse(contact.Phones);
        Reverse(contact.Emails);
        Reverse(contact.Addresses);
        Reverse(contact.Organizations);
        Reverse(contact.Groups);
        Reverse(contact.Tags);

        await _repository.UpsertAsync(contact);
        var reorderedLoad = await _repository.GetAsync(contact.Id);

        Assert.IsNotNull(reorderedLoad);
        CollectionAssert.AreEqual(new[] { phone2, phone1 }, reorderedLoad.Phones.ToArray());
        CollectionAssert.AreEqual(new[] { email2, email1 }, reorderedLoad.Emails.ToArray());
        CollectionAssert.AreEqual(new[] { address2, address1 }, reorderedLoad.Addresses.ToArray());
        CollectionAssert.AreEqual(new[] { organization2, organization1 }, reorderedLoad.Organizations.ToArray());
        CollectionAssert.AreEqual(new[] { group2.Name, group1.Name }, reorderedLoad.Groups.Select(x => x.Name).ToArray());
        CollectionAssert.AreEqual(new[] { tag2.Name, tag1.Name }, reorderedLoad.Tags.Select(x => x.Name).ToArray());
        Assert.AreEqual(phone2.Id, reorderedLoad.Phones[0].Id, "Reordering must preserve child identities.");
        Assert.AreEqual(phone1.Id, reorderedLoad.Phones[1].Id, "Reordering must preserve child identities.");
    }

    [TestMethod]
    public async Task Version_three_migration_adds_positions_and_backfills_existing_row_order()
    {
        var legacyPath = Path.Combine(_dir, "legacy-v2.db");
        var contactId = Guid.NewGuid().ToString();
        var firstPhoneId = Guid.NewGuid().ToString();
        var secondPhoneId = Guid.NewGuid().ToString();

        await using (var connection = new SqliteConnection($"Data Source={legacyPath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE TABLE schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);
                CREATE TABLE contacts (
                  id TEXT PRIMARY KEY,
                  given_name TEXT NOT NULL DEFAULT '', family_name TEXT NOT NULL DEFAULT '', nickname TEXT NOT NULL DEFAULT '',
                  birthday TEXT NULL, notes TEXT NOT NULL DEFAULT '', is_favorite INTEGER NOT NULL DEFAULT 0,
                  is_archived INTEGER NOT NULL DEFAULT 0, created_at TEXT NOT NULL, updated_at TEXT NOT NULL
                );
                CREATE TABLE phones (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, number TEXT NOT NULL, kind INTEGER NOT NULL);
                CREATE TABLE emails (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, address TEXT NOT NULL, kind INTEGER NOT NULL);
                CREATE TABLE addresses (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, street TEXT NOT NULL, city TEXT NOT NULL, region TEXT NOT NULL, postal_code TEXT NOT NULL, country TEXT NOT NULL);
                CREATE TABLE organizations (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, name TEXT NOT NULL, title TEXT NULL, department TEXT NULL);
                CREATE TABLE groups (id TEXT PRIMARY KEY, name TEXT NOT NULL COLLATE NOCASE UNIQUE);
                CREATE TABLE tags (id TEXT PRIMARY KEY, name TEXT NOT NULL COLLATE NOCASE UNIQUE);
                CREATE TABLE contact_groups (contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, group_id TEXT NOT NULL REFERENCES groups(id) ON DELETE CASCADE, PRIMARY KEY(contact_id, group_id));
                CREATE TABLE contact_tags (contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE, PRIMARY KEY(contact_id, tag_id));
                CREATE TABLE app_metadata (key TEXT PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO app_metadata(key,value) VALUES ('schema_family','contactcore');
                INSERT INTO schema_migrations(version,applied_at) VALUES (1,'2026-08-19T00:00:00Z'),(2,'2026-08-20T00:00:00Z');
                """;
            await create.ExecuteNonQueryAsync();

            await using var seed = connection.CreateCommand();
            seed.CommandText = """
                INSERT INTO contacts(id,given_name,family_name,nickname,birthday,notes,is_favorite,is_archived,created_at,updated_at)
                VALUES($contact,'Legacy','Order','',NULL,'',0,0,'2026-08-19T00:00:00Z','2026-08-19T00:00:00Z');
                INSERT INTO phones(id,contact_id,label,number,kind) VALUES($first,$contact,'First','1111111111',0);
                INSERT INTO phones(id,contact_id,label,number,kind) VALUES($second,$contact,'Second','2222222222',0);
                """;
            seed.Parameters.AddWithValue("$contact", contactId);
            seed.Parameters.AddWithValue("$first", firstPhoneId);
            seed.Parameters.AddWithValue("$second", secondPhoneId);
            await seed.ExecuteNonQueryAsync();
        }

        var legacyFactory = new SqliteConnectionFactory(legacyPath);
        await new DatabaseMigrator(legacyFactory).ApplyAsync();

        await using var verify = new SqliteConnection($"Data Source={legacyPath};Mode=ReadOnly");
        await verify.OpenAsync();
        await using var version = verify.CreateCommand();
        version.CommandText = "SELECT MAX(version) FROM schema_migrations;";
        Assert.AreEqual(3L, Convert.ToInt64(await version.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));

        foreach (var table in new[] { "phones", "emails", "addresses", "organizations", "contact_groups", "contact_tags" })
            Assert.IsTrue(await ColumnExistsAsync(verify, table, "position"), $"Expected {table}.position after schema v3 migration.");

        await using var positions = verify.CreateCommand();
        positions.CommandText = "SELECT id,position FROM phones WHERE contact_id=$contact ORDER BY position;";
        positions.Parameters.AddWithValue("$contact", contactId);
        await using var reader = await positions.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(firstPhoneId, reader.GetString(0));
        Assert.AreEqual(0L, reader.GetInt64(1));
        Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual(secondPhoneId, reader.GetString(0));
        Assert.AreEqual(1L, reader.GetInt64(1));
        Assert.IsFalse(await reader.ReadAsync());
    }

    private static void Reverse<T>(IList<T> values)
    {
        for (var left = 0; left < values.Count / 2; left++)
        {
            var right = values.Count - left - 1;
            (values[left], values[right]) = (values[right], values[left]);
        }
    }

    private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string table, string column)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}