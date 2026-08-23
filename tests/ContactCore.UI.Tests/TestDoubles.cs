using System.Collections.Concurrent;
using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.UI;

namespace ContactCore.UI.Tests;

internal sealed class RecordingContactRepository : IContactRepository
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, Contact> _contacts = [];
    private int _cancelledSearches;

    public ConcurrentQueue<ContactQuery> SearchQueries { get; } = new();
    public TaskCompletionSource<bool> SlowSearchStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int CancelledSearches => Volatile.Read(ref _cancelledSearches);
    public int DeleteCalls { get; private set; }

    public void Seed(params Contact[] contacts)
    {
        lock (_sync)
        {
            foreach (var contact in contacts) _contacts[contact.Id] = contact.DeepCopy();
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        SearchQueries.Enqueue(query);
        if (string.Equals(query.Search, "slow", StringComparison.Ordinal))
        {
            SlowSearchStarted.TrySetResult(true);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref _cancelledSearches);
                throw;
            }
        }

        lock (_sync)
        {
            IEnumerable<Contact> contacts = _contacts.Values;
            if (!query.IncludeArchived) contacts = contacts.Where(contact => !contact.IsArchived);
            if (query.FavoritesOnly) contacts = contacts.Where(contact => contact.IsFavorite);
            if (query.StartsWith is { } startsWith)
                contacts = contacts.Where(contact => contact.DisplayName.StartsWith(startsWith.ToString(), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.Search))
                contacts = contacts.Where(contact => contact.DisplayName.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            return contacts.Select(contact => contact.DeepCopy()).ToArray();
        }
    }

    public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(_contacts.TryGetValue(id, out var contact) ? contact.DeepCopy() : null);
        }
    }

    public Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync) _contacts[contact.Id] = contact.DeepCopy();
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(IReadOnlyList<Contact> contacts, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            foreach (var contact in contacts) _contacts[contact.Id] = contact.DeepCopy();
        }
        return Task.CompletedTask;
    }

    public Task MergeAsync(Contact mergedContact, Guid secondaryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _contacts[mergedContact.Id] = mergedContact.DeepCopy();
            _contacts.Remove(secondaryId);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            DeleteCalls++;
            _contacts.Remove(id);
        }
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync) return Task.FromResult(_contacts.Count);
    }
}

internal sealed class RecordingBackupService : IBackupService
{
    public int RestoreCalls { get; private set; }
    public string? LastRestorePath { get; private set; }

    public Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Path.Combine(destinationDirectory, "contactcore-test.db"));
    }

    public Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RestoreCalls++;
        LastRestorePath = backupFile;
        return Task.CompletedTask;
    }
}

internal sealed class TestPreferences : IAppPreferences
{
    public string Theme { get; set; } = "System";
    public bool ReducedMotion { get; set; }
    public bool ConfirmPermanentDelete { get; set; } = true;
    public string? DatabaseKey { get; set; }
    public void Save() { }
}

internal static class UiTestServices
{
    public static AppServices Create(
        RecordingContactRepository repository,
        RecordingBackupService? backup = null,
        TestPreferences? preferences = null,
        bool supportsBackups = false) =>
        new(
            new ContactService(repository),
            backup ?? new RecordingBackupService(),
            preferences ?? new TestPreferences(),
            new AppPlatformCapabilities(
                "Test",
                "In-memory test store",
                "Test backups",
                supportsBackups,
                SupportsDatabaseEncryption: false));
}
