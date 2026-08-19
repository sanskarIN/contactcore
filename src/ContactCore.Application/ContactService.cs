using ContactCore.Domain;

namespace ContactCore.Application;

public sealed class ContactValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception("The contact contains invalid data.")
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
}

public sealed class ContactService(IContactRepository repository)
{
    private readonly IContactRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<IReadOnlyList<Contact>> SearchAsync(ContactQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var safeQuery = query with
        {
            SearchText = query.SearchText?.Trim() ?? string.Empty,
            Offset = Math.Max(0, query.Offset),
            Limit = Math.Clamp(query.Limit, 1, 1000)
        };
        return _repository.SearchAsync(safeQuery, cancellationToken);
    }

    public Task<Contact?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetAsync(id, cancellationToken);

    public async Task<Contact> SaveAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);
        var normalized = Normalize(contact);
        var issues = ContactValidation.Validate(normalized);
        if (issues.Count > 0)
            throw new ContactValidationException(issues);

        normalized.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpsertAsync(normalized, cancellationToken).ConfigureAwait(false);
        return normalized;
    }

    public async Task<bool> SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var contact = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (contact is null) return false;
        contact.IsFavorite = isFavorite;
        contact.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpsertAsync(contact, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetArchivedAsync(Guid id, bool isArchived, CancellationToken cancellationToken = default)
    {
        var contact = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (contact is null) return false;
        contact.IsArchived = isArchived;
        contact.UpdatedAt = DateTimeOffset.UtcNow;
        await _repository.UpsertAsync(contact, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    private static Contact Normalize(Contact source)
    {
        var copy = source.DeepCopy();
        copy.GivenName = source.GivenName.Trim();
        copy.FamilyName = source.FamilyName.Trim();
        copy.Nickname = source.Nickname.Trim();
        copy.Notes = source.Notes.Trim();

        for (var i = 0; i < copy.Phones.Count; i++)
            copy.Phones[i] = copy.Phones[i] with { Label = copy.Phones[i].Label.Trim(), Number = copy.Phones[i].Number.Trim() };
        for (var i = 0; i < copy.Emails.Count; i++)
            copy.Emails[i] = copy.Emails[i] with { Label = copy.Emails[i].Label.Trim(), Address = copy.Emails[i].Address.Trim() };
        for (var i = 0; i < copy.Addresses.Count; i++)
        {
            var address = copy.Addresses[i];
            copy.Addresses[i] = address with
            {
                Label = address.Label.Trim(), Street = address.Street.Trim(), City = address.City.Trim(),
                Region = address.Region.Trim(), PostalCode = address.PostalCode.Trim(), Country = address.Country.Trim()
            };
        }
        return copy;
    }
}
