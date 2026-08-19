using ContactCore.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Application.Tests;

[TestClass]
public sealed class ApplicationTests
{
    [TestMethod]
    public async Task Save_NormalizesAndPersistsContact()
    {
        var repository = new FakeRepository();
        var service = new ContactService(repository);
        var contact = new Contact { GivenName = "  Ada ", FamilyName = " Lovelace  " };
        contact.Emails.Add(new(Guid.NewGuid(), " Home ", " ada@example.test "));

        var saved = await service.SaveAsync(contact);
        var persisted = await repository.GetAsync(saved.Id);

        Assert.IsNotNull(persisted);
        Assert.AreEqual("Ada", persisted.GivenName);
        Assert.AreEqual("Lovelace", persisted.FamilyName);
        Assert.AreEqual("ada@example.test", persisted.Emails[0].Address);
    }

    [TestMethod]
    public async Task Save_RejectsInvalidContact()
    {
        var service = new ContactService(new FakeRepository());
        var contact = new Contact { GivenName = "Test" };
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "invalid"));

        try
        {
            await service.SaveAsync(contact);
            Assert.Fail("Expected validation exception.");
        }
        catch (ContactValidationException ex)
        {
            Assert.AreEqual("Email", ex.Issues.Single().Field);
        }
    }

    [TestMethod]
    public void DuplicateScore_UsesIndependentSignals()
    {
        var left = new Contact { GivenName = "Ada", FamilyName = "Lovelace" };
        left.Emails.Add(new(Guid.NewGuid(), "Home", "ada@example.test"));
        left.Phones.Add(new(Guid.NewGuid(), "Mobile", "+91 98765 43210"));
        var right = new Contact { GivenName = "ADA", FamilyName = "LOVELACE" };
        right.Emails.Add(new(Guid.NewGuid(), "Other", "ADA@example.test"));
        right.Phones.Add(new(Guid.NewGuid(), "Other", "9876543210"));

        var result = DuplicateService.Score(left, right);

        Assert.AreEqual(1d, result.Score, 0.0001);
        Assert.AreEqual(3, result.Reasons.Count);
    }

    [TestMethod]
    public void Merge_KeepsUniqueCommunicationFields()
    {
        var primary = new Contact { GivenName = "Ada" };
        primary.Emails.Add(new(Guid.NewGuid(), "Home", "ada@example.test"));
        var secondary = new Contact { FamilyName = "Lovelace" };
        secondary.Emails.Add(new(Guid.NewGuid(), "Work", "ada@example.test"));
        secondary.Phones.Add(new(Guid.NewGuid(), "Mobile", "+44 123 456"));

        var merged = DuplicateService.Merge(primary, secondary);

        Assert.AreEqual("Ada", merged.GivenName);
        Assert.AreEqual("Lovelace", merged.FamilyName);
        Assert.AreEqual(1, merged.Emails.Count);
        Assert.AreEqual(1, merged.Phones.Count);
    }

    [TestMethod]
    public void Csv_RoundTripsQuotedNotes()
    {
        var contact = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Notes = "Comma, quote \" and\nnewline" };
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "ada@example.test"));

        var csv = ContactCsvCodec.Export([contact]);
        var imported = ContactCsvCodec.Import(csv).Single();

        Assert.AreEqual(contact.GivenName, imported.GivenName);
        Assert.AreEqual(contact.Notes, imported.Notes);
        Assert.AreEqual("ada@example.test", imported.Emails.Single().Address);
    }

    [TestMethod]
    public void VCard_RoundTripsCoreFields()
    {
        var contact = new Contact { GivenName = "Ada", FamilyName = "Lovelace", Nickname = "Enchantress" };
        contact.Emails.Add(new(Guid.NewGuid(), "Home", "ada@example.test", ContactFieldKind.Home));

        var text = VCardCodec.Export([contact]);
        var imported = VCardCodec.Import(text).Single();

        Assert.AreEqual(contact.Id, imported.Id);
        Assert.AreEqual("Ada", imported.GivenName);
        Assert.AreEqual("Lovelace", imported.FamilyName);
        Assert.AreEqual("ada@example.test", imported.Emails.Single().Address);
    }

    private sealed class FakeRepository : IContactRepository
    {
        private readonly Dictionary<Guid, Contact> _items = [];

        public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(id, out var value) ? value.DeepCopy() : null);

        public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
        {
            IEnumerable<Contact> values = _items.Values;
            if (!query.IncludeArchived) values = values.Where(item => !item.IsArchived);
            if (query.FavoritesOnly) values = values.Where(item => item.IsFavorite);
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var needle = TextNormalizer.SearchKey(query.SearchText);
                values = values.Where(item => TextNormalizer.SearchKey(item.DisplayName).Contains(needle, StringComparison.Ordinal));
            }
            return Task.FromResult<IReadOnlyList<Contact>>(values.Skip(query.Offset).Take(query.Limit).Select(item => item.DeepCopy()).ToArray());
        }

        public Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Contact>>(_items.Values.Select(item => item.DeepCopy()).ToArray());

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

        public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(_items.Count);
    }
}
