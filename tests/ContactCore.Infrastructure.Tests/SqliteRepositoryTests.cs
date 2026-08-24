using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class SqliteRepositoryTests
{
    private string _dir = null!;
    private SqliteContactRepository _repo = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var factory = new SqliteConnectionFactory(Path.Combine(_dir, "test.db"));
        _repo = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
        await _repo.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup() { try { Directory.Delete(_dir, true); } catch (IOException) { } }

    [TestMethod]
    public async Task Upsert_and_search_round_trip_children()
    {
        var c = new Contact { GivenName = "Test", FamilyName = "Person", IsFavorite = true };
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 99999 00000"));
        c.Emails.Add(new(Guid.NewGuid(), "Work", "test@example.test"));
        c.Tags.Add(new(Guid.NewGuid(), "Friends"));
        await _repo.UpsertAsync(c);
        var loaded = await _repo.GetAsync(c.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.Phones.Count);
        Assert.AreEqual(1, loaded.Emails.Count);
        Assert.AreEqual(1, loaded.Tags.Count);
        Assert.AreEqual(1, (await _repo.SearchAsync(new ContactQuery("Person", FavoritesOnly: true))).Count);
    }

    [TestMethod]
    public async Task Rich_aggregate_round_trip_preserves_all_child_types_and_replaces_stale_rows()
    {
        var c = new Contact { GivenName = "Rich", FamilyName = "Contact" };
        var firstPhone = new ContactPhone(Guid.NewGuid(), "Mobile", "1111111", ContactFieldKind.Mobile);
        var secondPhone = new ContactPhone(Guid.NewGuid(), "Work", "2222222", ContactFieldKind.Work);
        var email = new ContactEmail(Guid.NewGuid(), "Work", "rich@example.test", ContactFieldKind.Work);
        var address = new ContactAddress(Guid.NewGuid(), "Home", "1 Fictional Street", "Example City", "Example Region", "100001", "Exampleland");
        var organization = new ContactOrganization(Guid.NewGuid(), "Example Org", "Engineer", "Research");
        var group = new ContactGroup(Guid.NewGuid(), "Project Team");
        var tag = new ContactTag(Guid.NewGuid(), "Priority");
        c.Phones.Add(firstPhone);
        c.Phones.Add(secondPhone);
        c.Emails.Add(email);
        c.Addresses.Add(address);
        c.Organizations.Add(organization);
        c.Groups.Add(group);
        c.Tags.Add(tag);

        await _repo.UpsertAsync(c);
        var firstLoad = await _repo.GetAsync(c.Id);

        Assert.IsNotNull(firstLoad);
        CollectionAssert.AreEquivalent(new[] { firstPhone, secondPhone }, firstLoad.Phones.ToArray());
        Assert.AreEqual(email, firstLoad.Emails.Single());
        Assert.AreEqual(address, firstLoad.Addresses.Single());
        Assert.AreEqual(organization, firstLoad.Organizations.Single());
        Assert.AreEqual(group.Name, firstLoad.Groups.Single().Name);
        Assert.AreEqual(tag.Name, firstLoad.Tags.Single().Name);

        c.Phones.RemoveAt(0);
        c.Addresses.Clear();
        c.Groups.Clear();
        c.Notes = "Updated";
        await _repo.UpsertAsync(c);
        var secondLoad = await _repo.GetAsync(c.Id);

        Assert.IsNotNull(secondLoad);
        Assert.AreEqual("Updated", secondLoad.Notes);
        Assert.AreEqual(1, secondLoad.Phones.Count);
        Assert.AreEqual(secondPhone, secondLoad.Phones.Single());
        Assert.AreEqual(0, secondLoad.Addresses.Count, "Removed address rows must not remain stale in SQLite.");
        Assert.AreEqual(0, secondLoad.Groups.Count, "Removed contact-group links must not remain stale in SQLite.");
        Assert.AreEqual(email, secondLoad.Emails.Single());
        Assert.AreEqual(organization, secondLoad.Organizations.Single());
        Assert.AreEqual(tag.Name, secondLoad.Tags.Single().Name);
    }

    [TestMethod]
    public async Task Shared_group_and_tag_reassignment_persists_new_dictionary_identities()
    {
        var contact = new Contact { GivenName = "Dictionary" };
        var oldGroup = new ContactGroup(Guid.NewGuid(), "Friends");
        var oldTag = new ContactTag(Guid.NewGuid(), "Important");
        contact.Groups.Add(oldGroup);
        contact.Tags.Add(oldTag);
        await _repo.UpsertAsync(contact);

        var newGroup = new ContactGroup(Guid.NewGuid(), "Family");
        var newTag = new ContactTag(Guid.NewGuid(), "Client");
        contact.Groups.Clear();
        contact.Tags.Clear();
        contact.Groups.Add(newGroup);
        contact.Tags.Add(newTag);

        await _repo.UpsertAsync(contact);
        var loaded = await _repo.GetAsync(contact.Id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(newGroup, loaded.Groups.Single());
        Assert.AreEqual(newTag, loaded.Tags.Single());
        Assert.AreNotEqual(oldGroup.Id, loaded.Groups.Single().Id);
        Assert.AreNotEqual(oldTag.Id, loaded.Tags.Single().Id);
    }

    [TestMethod]
    public async Task Initialize_rejects_unrelated_existing_sqlite_before_contactcore_mutation()
    {
        var databasePath = Path.Combine(_dir, "unrelated.db");
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE unrelated_data(id INTEGER PRIMARY KEY, value TEXT NOT NULL); INSERT INTO unrelated_data(value) VALUES ('keep');";
            await create.ExecuteNonQueryAsync();
        }

        var factory = new SqliteConnectionFactory(databasePath);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new DatabaseMigrator(factory).ApplyAsync());

        await using var verify = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await verify.OpenAsync();
        await using var tableCheck = verify.CreateCommand();
        tableCheck.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='schema_migrations';";
        Assert.AreEqual(0L, Convert.ToInt64(await tableCheck.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
        tableCheck.CommandText = "SELECT COUNT(*) FROM unrelated_data;";
        Assert.AreEqual(1L, Convert.ToInt64(await tableCheck.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task Initialize_rejects_incomplete_database_that_claims_current_schema_version()
    {
        var databasePath = Path.Combine(_dir, "incomplete.db");
        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE TABLE schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);
                INSERT INTO schema_migrations(version, applied_at) VALUES (2, '2026-08-19T00:00:00.0000000+00:00');
                CREATE TABLE contacts(id TEXT PRIMARY KEY);
                CREATE TABLE app_metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO app_metadata(key, value) VALUES ('schema_family', 'contactcore');
                """;
            await create.ExecuteNonQueryAsync();
        }

        var factory = new SqliteConnectionFactory(databasePath);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new DatabaseMigrator(factory).ApplyAsync());

        await using var verify = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await verify.OpenAsync();
        await using var tableCheck = verify.CreateCommand();
        tableCheck.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='phones';";
        Assert.AreEqual(0L, Convert.ToInt64(await tableCheck.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task Initialize_rejects_conflicting_legacy_identity_without_recording_v2_migration()
    {
        var databasePath = Path.Combine(_dir, "legacy-conflict.db");
        var factory = new SqliteConnectionFactory(databasePath);
        var migrator = new DatabaseMigrator(factory);
        await migrator.ApplyAsync();

        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath}"))
        {
            await connection.OpenAsync();
            await using var tamper = connection.CreateCommand();
            tamper.CommandText = """
                DELETE FROM schema_migrations WHERE version=2;
                UPDATE app_metadata SET value='different-application' WHERE key='schema_family';
                """;
            await tamper.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => migrator.ApplyAsync());

        await using var verify = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await verify.OpenAsync();
        await using var versionCheck = verify.CreateCommand();
        versionCheck.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
        Assert.AreEqual(1L, Convert.ToInt64(await versionCheck.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
        versionCheck.CommandText = "SELECT value FROM app_metadata WHERE key='schema_family';";
        Assert.AreEqual("different-application", Convert.ToString(await versionCheck.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task Search_treats_percent_underscore_and_backslash_as_literal_text()
    {
        var percent = new Contact { GivenName = "Percent%Literal" };
        var percentControl = new Contact { GivenName = "PercentXLiteral" };
        var underscore = new Contact { GivenName = "Under_Score" };
        var underscoreControl = new Contact { GivenName = "UnderXScore" };
        var slash = new Contact { GivenName = "Back\\Slash" };
        var slashControl = new Contact { GivenName = "BackXSlash" };
        await _repo.UpsertManyAsync([percent, percentControl, underscore, underscoreControl, slash, slashControl]);

        var percentMatches = await _repo.SearchAsync(new ContactQuery("%"));
        var underscoreMatches = await _repo.SearchAsync(new ContactQuery("_"));
        var slashMatches = await _repo.SearchAsync(new ContactQuery("\\"));

        Assert.AreEqual(1, percentMatches.Count);
        Assert.AreEqual(percent.Id, percentMatches[0].Id);
        Assert.AreEqual(1, underscoreMatches.Count);
        Assert.AreEqual(underscore.Id, underscoreMatches[0].Id);
        Assert.AreEqual(1, slashMatches.Count);
        Assert.AreEqual(slash.Id, slashMatches[0].Id);
    }

    [TestMethod]
    public async Task Search_filters_by_tag_group_and_family_first_letter_case_insensitively()
    {
        var matching = new Contact { GivenName = "Zelda", FamilyName = "Baker" };
        matching.Groups.Add(new(Guid.NewGuid(), "Project Team"));
        matching.Tags.Add(new(Guid.NewGuid(), "Priority"));
        var control = new Contact { GivenName = "Baker", FamilyName = "Carter" };
        control.Groups.Add(new(Guid.NewGuid(), "Other Group"));
        control.Tags.Add(new(Guid.NewGuid(), "Other Tag"));
        await _repo.UpsertManyAsync([matching, control]);

        var byTag = await _repo.SearchAsync(new ContactQuery(Tag: "priority"));
        var byGroup = await _repo.SearchAsync(new ContactQuery(Group: "project team"));
        var byLetter = await _repo.SearchAsync(new ContactQuery(StartsWith: 'B'));

        Assert.AreEqual(1, byTag.Count);
        Assert.AreEqual(matching.Id, byTag[0].Id);
        Assert.AreEqual(1, byGroup.Count);
        Assert.AreEqual(matching.Id, byGroup[0].Id);
        Assert.AreEqual(1, byLetter.Count);
        Assert.AreEqual(matching.Id, byLetter[0].Id, "StartsWith should prefer family name when it is present.");
    }

    [TestMethod]
    public async Task Delete_cascades_related_rows()
    {
        var c = new Contact { GivenName = "Disposable" };
        c.Phones.Add(new(Guid.NewGuid(), "Mobile", "1234567"));
        await _repo.UpsertAsync(c);
        await _repo.DeleteAsync(c.Id);
        Assert.IsNull(await _repo.GetAsync(c.Id));
        Assert.AreEqual(0, await _repo.CountAsync());
    }

    [TestMethod]
    public async Task Bulk_upsert_rolls_back_every_contact_when_one_write_fails()
    {
        var sharedPhoneId = Guid.NewGuid();
        var first = new Contact { GivenName = "First" };
        first.Phones.Add(new(sharedPhoneId, "Mobile", "1111111"));
        var second = new Contact { GivenName = "Second" };
        second.Phones.Add(new(sharedPhoneId, "Mobile", "2222222"));

        var failed = false;
        try
        {
            await _repo.UpsertManyAsync([first, second]);
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            failed = true;
        }

        Assert.IsTrue(failed, "The duplicate child primary key should make the batch fail.");
        Assert.AreEqual(0, await _repo.CountAsync(), "The successful prefix must be rolled back with the failing contact.");
    }
}
