using ContactCore.Domain;

namespace ContactCore.Application;

public sealed class ContactService(IContactRepository repository)
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => repository.InitializeAsync(cancellationToken);

    public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default) =>
        repository.SearchAsync(query, cancellationToken);

    public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default) => repository.GetAsync(id, cancellationToken);

    public async Task SaveAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        contact.GivenName = contact.GivenName.Trim();
        contact.FamilyName = contact.FamilyName.Trim();
        contact.Nickname = contact.Nickname.Trim();
        contact.UpdatedAt = DateTimeOffset.UtcNow;
        var issues = ContactValidation.Validate(contact);
        if (issues.Count > 0) throw new ContactValidationException(issues);
        await repository.UpsertAsync(contact, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFavoriteAsync(Guid id, bool value, CancellationToken cancellationToken = default)
    {
        var contact = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        contact.IsFavorite = value;
        await SaveAsync(contact, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetArchivedAsync(Guid id, bool value, CancellationToken cancellationToken = default)
    {
        var contact = await RequireAsync(id, cancellationToken).ConfigureAwait(false);
        contact.IsArchived = value;
        await SaveAsync(contact, cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => repository.DeleteAsync(id, cancellationToken);

    private async Task<Contact> RequireAsync(Guid id, CancellationToken cancellationToken)
        => await repository.GetAsync(id, cancellationToken).ConfigureAwait(false)
           ?? throw new KeyNotFoundException("The contact no longer exists.");
}

public sealed class ContactValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception(string.Join(" ", issues.Select(x => x.Message)))
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
}
