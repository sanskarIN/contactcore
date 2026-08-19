using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class SqliteRepositoryTests
{
    [TestMethod]
    public async Task Initialize_IsIdempotent()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Database.InitializeAsync();
            await fixture.Database.InitializeAsync();
            Assert.AreEqual(0, await fixture.Repository.CountAsync());
        }
        finally { Cleanup(fixture.Directory); }
    }

    [TestMethod]
    public async Task Repository_RoundTripsAggregate()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Database.InitializeAsync();
            var contact = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Birthday = new DateOnly(1815, 12, 10), Notes = "First programmer" };
            contact.Phones.Add(new(Guid.NewGuid(), "Mobile", "+44 1234 567890"));
            contact.Emails.Add(new(Guid.NewGuid(), "Home", "ada@example.test"));
            contact.Addresses.Add(new(Guid.NewGuid(), "Home", "1 Analytical Way", "London", "London", "N1", "UK"));
            contact.Organizations.Add(new(Guid.NewGuid(), "Analytical Engine", "Programmer", "Research"));
            contact.Groups.Add(new(Guid.NewGuid(), "History"));
            contact.Tags.Add(new(Guid.NewGuid(), "VIP"));

            await fixture.Repository.UpsertAsync(contact);
            var loaded = await fixture.Repository.GetAsync(contact.Id);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Ada Lovelace", loaded.DisplayName);
            Assert.AreEqual(new DateOnly(1815, 12, 10), loaded.Birthday);
            Assert.AreEqual("ada@example.test", loaded.Emails.Single().Address);
            Assert.AreEqual("+44 1234 567890", loaded.Phones.Single().Number);
            Assert.AreEqual("London", loaded.Addresses.Single().City);
            Assert.AreEqual("Analytical Engine", loaded.Organizations.Single().Name);
            Assert.AreEqual("History", loaded.Groups.Single().Name);
            Assert.AreEqual("VIP", loaded.Tags.Single().Name);
        }
        finally { Cleanup(fixture.Directory); }
    }

    [TestMethod]
    public async Task Search_MatchesEmailAndHonorsArchiveFilter()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Database.InitializeAsync();
            var visible = new Contact { GivenName = "Visible" };
            visible.Emails.Add(new(Guid.NewGuid(), "Home", "unique@example.test"));
            var archived = new Contact { GivenName = "Archived", IsArchived = true };
            archived.Emails.Add(new(Guid.NewGuid(), "Home", "unique@example.test"));
            await fixture.Repository.UpsertAsync(visible);
            await fixture.Repository.UpsertAsync(archived);

            var normal = await fixture.Repository.SearchAsync(new ContactQuery("unique@example.test"));
            var withArchived = await fixture.Repository.SearchAsync(new ContactQuery("unique@example.test", IncludeArchived: true));

            Assert.AreEqual(1, normal.Count);
            Assert.AreEqual(2, withArchived.Count);
        }
        finally { Cleanup(fixture.Directory); }
    }

    [TestMethod]
    public async Task BackupRestore_RevertsToVerifiedSnapshot()
    {
        var fixture = CreateFixture();
        try
        {
            await fixture.Database.InitializeAsync();
            await fixture.Repository.UpsertAsync(new Contact { GivenName = "First" });
            var backup = Path.Combine(fixture.Directory, "backup.db");
            var backupService = new SqliteBackupService(fixture.Database);
            var result = await backupService.CreateBackupAsync(backup);
            Assert.IsTrue(result.SizeBytes > 0);

            await fixture.Repository.UpsertAsync(new Contact { GivenName = "Second" });
            Assert.AreEqual(2, await fixture.Repository.CountAsync());

            await backupService.RestoreBackupAsync(backup);
            Assert.AreEqual(1, await fixture.Repository.CountAsync());
        }
        finally { Cleanup(fixture.Directory); }
    }

    private static (string Directory, SqliteDatabase Database, SqliteContactRepository Repository) CreateFixture()
    {
        var directory = Path.Combine(Path.GetTempPath(), "contactcore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var database = new SqliteDatabase(Path.Combine(directory, "test.db"));
        return (directory, database, new SqliteContactRepository(database));
    }

    private static void Cleanup(string directory)
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
