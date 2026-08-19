using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record ContactQuery(string Search = "", bool FavoritesOnly = false, bool IncludeArchived = false, string? Tag = null, string? Group = null, char? StartsWith = null);

public interface IContactRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default);
    Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default);
}

public interface IAppPreferences
{
    string Theme { get; set; }
    bool ReducedMotion { get; set; }
    bool ConfirmPermanentDelete { get; set; }
    bool HasCompletedOnboarding { get; set; }
    string? DatabaseKey { get; set; }
    void Save();
}
