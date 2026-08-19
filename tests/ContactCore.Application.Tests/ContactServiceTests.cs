using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class ContactServiceTests
{
    [TestMethod]
    public async Task Merge_keeps_primary_id_combines_data_and_deletes_secondary()
    {
        var repository = new MemoryRepository();
        var primary = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Notes = "Primary" };
        primary.Phones.Add(new(Guid.NewGuid(), "Mobile", "+44 12345"));
        var secondary = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Notes = "Secondary", IsFavorite = true };
        secondary.Emails.Add(new(Guid.NewGuid(), "Work", "ada@example.test"));
        await repository.UpsertAsync(primary);
        await repository.UpsertAsync(secondary);

        var merged = await new ContactService(repository).MergeAsync(primary.Id, secondary.Id);

        Assert.AreEqual(primary.Id, merged.Id);
        Assert.IsTrue(merged.IsFavorite);
        Assert.AreEqual(1, merged.Phones.Count);
        Assert.AreEqual(1, merged.Emails.Count);
        StringAssert.Contains(merged.Notes, "Primary");
        StringAssert.Contains(merged.Notes, "Secondary");
        Assert.IsNull(await repository.GetAsync(secondary.Id));
    }

    [TestMethod]
    public async Task Import_validates_each_contact()
    {
        var repository = new MemoryRepository();
        var valid = new Contact { GivenName = "Valid" };
        var invalid = new Contact { GivenName = new string('x', 121) };

        await Assert.ThrowsExceptionAsync<ContactValidationException>(
            () => new ContactService(repository).ImportAsync([valid, invalid]));

        Assert.AreEqual(1, await repository.CountAsync());
    }

    private sealed class MemoryRepository : IContactRepository
    {
        private readonly Dictionary<Guid, Contact> _items = [];

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Contact>>(_items.Values.Select(x => x.DeepCopy()).ToArray());

        public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(id, out var value) ? value.DeepCopy() : null);

        public Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            _items[contact.Id] = contact.DeepCopy();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count);
    }
}
