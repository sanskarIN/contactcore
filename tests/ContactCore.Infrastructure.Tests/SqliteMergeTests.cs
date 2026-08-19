using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class SqliteMergeTests
{
    private string _dir = null!;
    private SqliteContactRepository _repo = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-merge-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var factory = new SqliteConnectionFactory(Path.Combine(_dir, "test.db"));
        _repo = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
        await _repo.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        try { Directory.Delete(_dir, true); }
        catch (IOException) { }
    }

    [TestMethod]
    public async Task Merge_updates_primary_and_deletes_secondary_in_one_transaction()
    {
        var primary = new Contact { GivenName = "Primary", Notes = "before" };
        var secondary = new Contact { GivenName = "Secondary" };
        secondary.Emails.Add(new(Guid.NewGuid(), "Work", "secondary@example.test", ContactFieldKind.Work));
        await _repo.UpsertManyAsync([primary, secondary]);

        var merged = primary.DeepCopy();
        merged.Notes = "merged";
        merged.Emails.Add(new(Guid.NewGuid(), "Work", "secondary@example.test", ContactFieldKind.Work));

        await _repo.MergeAsync(merged, secondary.Id);

        var loaded = await _repo.GetAsync(primary.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("merged", loaded.Notes);
        Assert.AreEqual("secondary@example.test", loaded.Emails.Single().Address);
        Assert.IsNull(await _repo.GetAsync(secondary.Id));
        Assert.AreEqual(1, await _repo.CountAsync());
    }

    [TestMethod]
    public async Task Merge_rolls_back_primary_update_when_secondary_disappeared()
    {
        var primary = new Contact { GivenName = "Primary", Notes = "original" };
        await _repo.UpsertAsync(primary);
        var attempted = primary.DeepCopy();
        attempted.Notes = "must not persist";

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => _repo.MergeAsync(attempted, Guid.NewGuid()));

        var loaded = await _repo.GetAsync(primary.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual("original", loaded.Notes);
        Assert.AreEqual(1, await _repo.CountAsync());
    }

    [TestMethod]
    public async Task Merge_does_not_recreate_primary_when_primary_disappeared()
    {
        var primary = new Contact { GivenName = "Primary", Notes = "stale copy" };
        var secondary = new Contact { GivenName = "Secondary", Notes = "must remain" };
        await _repo.UpsertManyAsync([primary, secondary]);

        var attempted = primary.DeepCopy();
        attempted.Notes = "must not be recreated";
        await _repo.DeleteAsync(primary.Id);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => _repo.MergeAsync(attempted, secondary.Id));

        Assert.IsNull(await _repo.GetAsync(primary.Id));
        var survivingSecondary = await _repo.GetAsync(secondary.Id);
        Assert.IsNotNull(survivingSecondary);
        Assert.AreEqual("must remain", survivingSecondary.Notes);
        Assert.AreEqual(1, await _repo.CountAsync());
    }
}
