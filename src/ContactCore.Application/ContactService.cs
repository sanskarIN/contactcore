using ContactCore.Domain;

namespace ContactCore.Application;

public sealed class ContactService(IContactRepository repository)
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => repository.InitializeAsync(cancellationToken);

    public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return repository.SearchAsync(query with { Search = query.Search.Trim() }, cancellationToken);
    }

    public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default) => repository.GetAsync(id, cancellationToken);

    public async Task SaveAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        NormalizeInPlace(contact);
        contact.UpdatedAt = DateTimeOffset.UtcNow;
        var issues = ContactValidation.Validate(contact);
        if (issues.Count > 0) throw new ContactValidationException(issues);
        await repository.UpsertAsync(contact, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> ImportAsync(IEnumerable<Contact> contacts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var normalized = contacts.Select(contact =>
        {
            ArgumentNullException.ThrowIfNull(contact);
            var copy = contact.DeepCopy();
            NormalizeInPlace(copy);
            return copy;
        }).ToArray();

        if (normalized.Length == 0) return 0;

        var issues = new List<ValidationIssue>();
        for (var index = 0; index < normalized.Length; index++)
        {
            foreach (var issue in ContactValidation.Validate(normalized[index]))
                issues.Add(issue with { Field = $"Contact[{index + 1}].{issue.Field}" });
        }
        if (issues.Count > 0) throw new ContactValidationException(issues);

        var now = DateTimeOffset.UtcNow;
        foreach (var contact in normalized) contact.UpdatedAt = now;
        await repository.UpsertManyAsync(normalized, cancellationToken).ConfigureAwait(false);
        return normalized.Length;
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

    private static void NormalizeInPlace(Contact contact)
    {
        contact.GivenName = contact.GivenName.Trim();
        contact.FamilyName = contact.FamilyName.Trim();
        contact.Nickname = contact.Nickname.Trim();
        contact.Notes = contact.Notes.Trim();
        for (var i = 0; i < contact.Phones.Count; i++)
            contact.Phones[i] = contact.Phones[i] with { Label = contact.Phones[i].Label.Trim(), Number = contact.Phones[i].Number.Trim() };
        for (var i = 0; i < contact.Emails.Count; i++)
            contact.Emails[i] = contact.Emails[i] with { Label = contact.Emails[i].Label.Trim(), Address = contact.Emails[i].Address.Trim() };
    }
}

public sealed class ContactValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception(string.Join(" ", issues.Select(x => x.Message)))
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
}
