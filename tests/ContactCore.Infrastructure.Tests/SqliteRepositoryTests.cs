using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;
using Microsoft.Data.Sqlite;
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
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public async Task Upsert_and_search_round_trip_complete_aggregate()
    {
        var contact = new Contact
        {
            GivenName = "Test",
            FamilyName = "Person",
            Birthday = new DateOnly(2001, 2, 3),
            Notes = "Primary note",
            IsFavorite = true
        };
        contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 99999 00000"));
        contact.Emails.Add(new(Guid.NewGuid(), "Work", "test@example.test"));
        contact.Addresses.Add(new(Guid.NewGuid(), "Home", "1 Test Road", "Pune", "MH", "411001", "India"));
        contact.Organizations.Add(new(Guid.NewGuid(), "Example Org", "Engineer", "R&D"));
        contact.Dates.Add(new(Guid.NewGuid(), "Anniversary", new DateOnly(2024, 4, 5)));
        contact.NoteEntries.Add(new(Guid.NewGuid(), "Meeting", "Follow up next week."));
        contact.Groups.Add(new(Guid.NewGuid(), "Friends"));
        contact.Tags.Add(new(Guid.NewGuid(), "Priority"));

        await _repo.UpsertAsync(contact);
        var loaded = await _repo.GetAsync(contact.Id);

        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.Phones.Count);
        Assert.AreEqual(1, loaded.Emails.Count);
        Assert.AreEqual(1, loaded.Addresses.Count);
        Assert.AreEqual(1, loaded.Organizations.Count);
        Assert.AreEqual(new DateOnly(2024, 4, 5), loaded.Dates.Single().Date);
        Assert.AreEqual("Follow up next week.", loaded.NoteEntries.Single().Content);
        Assert.AreEqual("Friends", loaded.Groups.Single().Name);
        Assert.AreEqual("Priority", loaded.Tags.Single().Name);
        Assert.AreEqual(1, (await _repo.SearchAsync(new ContactQuery("Person", FavoritesOnly: true))).Count);
        Assert.AreEqual(1, (await _repo.SearchAsync(new ContactQuery("Follow up"))).Count);
    }

    [TestMethod]
    public async Task Repeated_children_are_replaced_transactionally_on_update()
    {
        var contact = new Contact { GivenName = "Mutable" };
        contact.Dates.Add(new(Guid.NewGuid(), "Old", new DateOnly(2020, 1, 1)));
        contact.NoteEntries.Add(new(Guid.NewGuid(), "Old", "Old content"));
        await _repo.UpsertAsync(contact);

        contact.Dates.Clear();
        contact.Dates.Add(new(Guid.NewGuid(), "New", new DateOnly(2026, 8, 19)));
        contact.NoteEntries.Clear();
        contact.NoteEntries.Add(new(Guid.NewGuid(), "New", "New content"));
        await _repo.UpsertAsync(contact);

        var loaded = await _repo.GetAsync(contact.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded.Dates.Count);
        Assert.AreEqual("New", loaded.Dates.Single().Label);
        Assert.AreEqual(1, loaded.NoteEntries.Count);
        Assert.AreEqual("New content", loaded.NoteEntries.Single().Content);
    }

    [TestMethod]
    public async Task Delete_cascades_related_rows()
    {
        var contact = new Contact { GivenName = "Disposable" };
        contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "1234567"));
        contact.Dates.Add(new(Guid.NewGuid(), "Anniversary", new DateOnly(2020, 1, 1)));
        contact.NoteEntries.Add(new(Guid.NewGuid(), "Private", "Delete me"));

        await _repo.UpsertAsync(contact);
        await _repo.DeleteAsync(contact.Id);

        Assert.IsNull(await _repo.GetAsync(contact.Id));
        Assert.AreEqual(0, await _repo.CountAsync());
    }
}
