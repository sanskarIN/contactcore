using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;
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
