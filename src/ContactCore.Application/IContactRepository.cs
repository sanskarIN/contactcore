using ContactCore.Domain;

namespace ContactCore.Application;

public sealed record ContactQuery(
    string SearchText = "",
    bool FavoritesOnly = false,
    bool IncludeArchived = false,
    int Offset = 0,
    int Limit = 250);

public interface IContactRepository
{
    Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contact>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(Contact contact, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
