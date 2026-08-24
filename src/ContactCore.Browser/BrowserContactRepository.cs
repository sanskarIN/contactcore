using System.Text.Json;
using ContactCore.Application;
using ContactCore.Domain;

namespace ContactCore.Browser;

public sealed class BrowserContactRepository : IContactRepository, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<Guid, Contact> _contacts = [];
    private bool _initialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;
            var json = await BrowserStorageInterop.LoadContactsAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                BrowserContactDocument[]? documents;
                try
                {
                    documents = JsonSerializer.Deserialize(json, BrowserJsonContext.Default.BrowserContactDocumentArray);
                }
                catch (JsonException ex)
                {
                    throw new InvalidDataException("The browser contact store could not be read safely.", ex);
                }

                _contacts.Clear();
                foreach (var document in documents ?? [])
                {
                    var contact = document.ToDomain();
                    if (!_contacts.TryAdd(contact.Id, contact))
                        throw new InvalidDataException("The browser contact store contains duplicate contact identities.");
                }
            }

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IEnumerable<Contact> result = _contacts.Values;
            if (!query.IncludeArchived) result = result.Where(contact => !contact.IsArchived);
            if (query.FavoritesOnly) result = result.Where(contact => contact.IsFavorite);
            if (!string.IsNullOrWhiteSpace(query.Tag))
            {
                var tag = TextNormalizer.SearchKey(query.Tag);
                result = result.Where(contact => contact.Tags.Any(value => TextNormalizer.SearchKey(value.Name) == tag));
            }
            if (!string.IsNullOrWhiteSpace(query.Group))
            {
                var group = TextNormalizer.SearchKey(query.Group);
                result = result.Where(contact => contact.Groups.Any(value => TextNormalizer.SearchKey(value.Name) == group));
            }
            if (query.StartsWith is { } startsWith)
            {
                var prefix = TextNormalizer.SearchKey(startsWith.ToString());
                result = result.Where(contact => TextNormalizer.SearchKey(contact.DisplayName).StartsWith(prefix, StringComparison.Ordinal));
            }
            if (!string.IsNullOrWhiteSpace(query.Search))
                result = result.Where(contact => MatchesSearch(contact, query.Search));

            return result
                .OrderBy(contact => contact.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(contact => contact.Id)
                .Select(contact => contact.DeepCopy())
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _contacts.TryGetValue(id, out var contact) ? contact.DeepCopy() : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        return WriteAsync(() => _contacts[contact.Id] = contact.DeepCopy(), cancellationToken);
    }

    public Task UpsertManyAsync(IReadOnlyList<Contact> contacts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        return WriteAsync(() =>
        {
            foreach (var contact in contacts)
            {
                ArgumentNullException.ThrowIfNull(contact);
                _contacts[contact.Id] = contact.DeepCopy();
            }
        }, cancellationToken);
    }

    public Task MergeAsync(Contact mergedContact, Guid secondaryId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mergedContact);
        if (mergedContact.Id == secondaryId)
            throw new ArgumentException("The surviving and secondary contacts must be different.", nameof(secondaryId));

        return WriteAsync(() =>
        {
            if (!_contacts.ContainsKey(mergedContact.Id) || !_contacts.ContainsKey(secondaryId))
                throw new KeyNotFoundException("One of the reviewed contacts no longer exists.");

            _contacts[mergedContact.Id] = mergedContact.DeepCopy();
            _contacts.Remove(secondaryId);
        }, cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        WriteAsync(() => _contacts.Remove(id), cancellationToken);

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _contacts.Count;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task WriteAsync(Action mutation, CancellationToken cancellationToken)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = _contacts.ToDictionary(pair => pair.Key, pair => pair.Value.DeepCopy());
            try
            {
                mutation();
                var documents = _contacts.Values
                    .OrderBy(contact => contact.Id)
                    .Select(BrowserContactDocument.FromDomain)
                    .ToArray();
                var json = JsonSerializer.Serialize(documents, BrowserJsonContext.Default.BrowserContactDocumentArray);
                await BrowserStorageInterop.SaveContactsAsync(json).ConfigureAwait(false);
            }
            catch
            {
                _contacts.Clear();
                foreach (var pair in before) _contacts.Add(pair.Key, pair.Value);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool MatchesSearch(Contact contact, string query)
    {
        var text = TextNormalizer.SearchKey(query);
        var phone = TextNormalizer.PhoneKey(query);

        if (text.Length > 0)
        {
            if (TextNormalizer.SearchKey(contact.DisplayName).Contains(text, StringComparison.Ordinal) ||
                TextNormalizer.SearchKey(contact.GivenName).Contains(text, StringComparison.Ordinal) ||
                TextNormalizer.SearchKey(contact.FamilyName).Contains(text, StringComparison.Ordinal) ||
                TextNormalizer.SearchKey(contact.Nickname).Contains(text, StringComparison.Ordinal) ||
                contact.Emails.Any(value => TextNormalizer.SearchKey(value.Address).Contains(text, StringComparison.Ordinal)))
                return true;
        }

        return phone.Length > 0 && contact.Phones.Any(value => TextNormalizer.PhoneKey(value.Number).Contains(phone, StringComparison.Ordinal));
    }
}
