using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class ContactServiceTests
{
    [TestMethod]
    public async Task Save_normalizes_visible_contact_values_before_single_upsert()
    {
        var repository = new RecordingRepository();
        var service = new ContactService(repository);
        var contact = new Contact
        {
            GivenName = "  Ada  ",
            FamilyName = "  Example  ",
            Nickname = "  A  ",
            Notes = "  Local notes  "
        };
        contact.Phones.Add(new(Guid.NewGuid(), "  Mobile  ", "  +91 99999 00000  "));
        contact.Emails.Add(new(Guid.NewGuid(), "  Work  ", "  ada@example.test  "));
        var before = contact.UpdatedAt;

        await service.SaveAsync(contact);

        Assert.AreSame(contact, repository.SingleUpsert);
        Assert.AreEqual("Ada", contact.GivenName);
        Assert.AreEqual("Example", contact.FamilyName);
        Assert.AreEqual("A", contact.Nickname);
        Assert.AreEqual("Local notes", contact.Notes);
        Assert.AreEqual("Mobile", contact.Phones.Single().Label);
        Assert.AreEqual("+91 99999 00000", contact.Phones.Single().Number);
        Assert.AreEqual("Work", contact.Emails.Single().Label);
        Assert.AreEqual("ada@example.test", contact.Emails.Single().Address);
        Assert.IsTrue(contact.UpdatedAt >= before);
    }

    [TestMethod]
    public async Task Save_normalizes_all_rich_contact_collections_before_persistence()
    {
        var repository = new RecordingRepository();
        var service = new ContactService(repository);
        var contact = new Contact { GivenName = "Rich" };
        contact.Addresses.Add(new(
            Guid.NewGuid(),
            "  Home  ",
            "  1 Example Street  ",
            "  Example City  ",
            "  Example Region  ",
            "  12345  ",
            "  Example Country  "));
        contact.Organizations.Add(new(
            Guid.NewGuid(),
            "  Example Org  ",
            "  Engineer  ",
            "   "));
        contact.Groups.Add(new(Guid.NewGuid(), "  Friends  "));
        contact.Tags.Add(new(Guid.NewGuid(), "  Important  "));

        await service.SaveAsync(contact);

        Assert.AreSame(contact, repository.SingleUpsert);
        var address = contact.Addresses.Single();
        Assert.AreEqual("Home", address.Label);
        Assert.AreEqual("1 Example Street", address.Street);
        Assert.AreEqual("Example City", address.City);
        Assert.AreEqual("Example Region", address.Region);
        Assert.AreEqual("12345", address.PostalCode);
        Assert.AreEqual("Example Country", address.Country);

        var organization = contact.Organizations.Single();
        Assert.AreEqual("Example Org", organization.Name);
        Assert.AreEqual("Engineer", organization.Title);
        Assert.IsNull(organization.Department);
        Assert.AreEqual("Friends", contact.Groups.Single().Name);
        Assert.AreEqual("Important", contact.Tags.Single().Name);
    }

    [TestMethod]
    public async Task Import_validates_the_whole_batch_before_any_bulk_upsert()
    {
        var repository = new RecordingRepository();
        var service = new ContactService(repository);
        var first = new Contact { GivenName = "  First  " };
        var second = new Contact { GivenName = "Second" };
        second.Emails.Add(new(Guid.NewGuid(), "Work", "not-an-email"));

        var error = await Assert.ThrowsAsync<ContactValidationException>(() => service.ImportAsync([first, second]));

        Assert.AreEqual(0, repository.BulkUpsertCalls);
        Assert.IsNull(repository.LastBulkUpsert);
        Assert.IsTrue(error.Issues.Any(x => x.Field == "Contact[2].Email"));
        Assert.AreEqual("  First  ", first.GivenName, "Import must normalize a deep copy rather than mutating the source object.");
    }

    [TestMethod]
    public async Task Valid_import_uses_one_normalized_bulk_upsert_and_shared_update_timestamp()
    {
        var repository = new RecordingRepository();
        var service = new ContactService(repository);
        var first = new Contact { GivenName = "  First  ", Notes = "  One  " };
        var second = new Contact { GivenName = "  Second  ", Notes = "  Two  " };

        var count = await service.ImportAsync([first, second]);

        Assert.AreEqual(2, count);
        Assert.AreEqual(1, repository.BulkUpsertCalls);
        Assert.IsNotNull(repository.LastBulkUpsert);
        Assert.AreEqual(2, repository.LastBulkUpsert.Count);
        Assert.AreEqual("First", repository.LastBulkUpsert[0].GivenName);
        Assert.AreEqual("One", repository.LastBulkUpsert[0].Notes);
        Assert.AreEqual("Second", repository.LastBulkUpsert[1].GivenName);
        Assert.AreEqual("Two", repository.LastBulkUpsert[1].Notes);
        Assert.AreEqual(repository.LastBulkUpsert[0].UpdatedAt, repository.LastBulkUpsert[1].UpdatedAt);
        Assert.AreEqual("  First  ", first.GivenName);
        Assert.AreEqual("  Second  ", second.GivenName);
        Assert.AreNotSame(first, repository.LastBulkUpsert[0]);
        Assert.AreNotSame(second, repository.LastBulkUpsert[1]);
    }

    [TestMethod]
    public async Task Search_trims_free_text_without_changing_other_query_filters()
    {
        var repository = new RecordingRepository();
        var service = new ContactService(repository);
        var query = new ContactQuery("  Ada  ", FavoritesOnly: true, IncludeArchived: true, Tag: "Team", Group: "Friends", StartsWith: 'A');

        await service.SearchAsync(query);

        Assert.IsNotNull(repository.LastSearchQuery);
        Assert.AreEqual("Ada", repository.LastSearchQuery.Search);
        Assert.IsTrue(repository.LastSearchQuery.FavoritesOnly);
        Assert.IsTrue(repository.LastSearchQuery.IncludeArchived);
        Assert.AreEqual("Team", repository.LastSearchQuery.Tag);
        Assert.AreEqual("Friends", repository.LastSearchQuery.Group);
        Assert.AreEqual('A', repository.LastSearchQuery.StartsWith);
    }

    private sealed class RecordingRepository : IContactRepository
    {
        public Contact? SingleUpsert { get; private set; }
        public IReadOnlyList<Contact>? LastBulkUpsert { get; private set; }
        public int BulkUpsertCalls { get; private set; }
        public ContactQuery? LastSearchQuery { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
        {
            LastSearchQuery = query;
            return Task.FromResult<IReadOnlyList<Contact>>([]);
        }

        public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Contact?>(null);

        public Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
        {
            SingleUpsert = contact;
            return Task.CompletedTask;
        }

        public Task UpsertManyAsync(IReadOnlyList<Contact> contacts, CancellationToken cancellationToken = default)
        {
            BulkUpsertCalls++;
            LastBulkUpsert = contacts;
            return Task.CompletedTask;
        }

        public Task MergeAsync(Contact mergedContact, Guid secondaryId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
