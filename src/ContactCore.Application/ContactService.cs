using ContactCore.Domain;

namespace ContactCore.Application;

public sealed class ContactService(IContactRepository repository)
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => repository.InitializeAsync(cancellationToken);
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => repository.CountAsync(cancellationToken);

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
        ValidateOrThrow(contact);
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

    public async Task<Contact> MergeAsync(Guid primaryId, Guid secondaryId, CancellationToken cancellationToken = default)
    {
        if (primaryId == secondaryId)
            throw new ArgumentException("A contact cannot be merged with itself.", nameof(secondaryId));

        var primary = await RequireAsync(primaryId, cancellationToken).ConfigureAwait(false);
        var secondary = await RequireAsync(secondaryId, cancellationToken).ConfigureAwait(false);
        var merged = new ContactMerger().Merge(primary, secondary);
        NormalizeInPlace(merged);
        merged.UpdatedAt = DateTimeOffset.UtcNow;
        ValidateOrThrow(merged);
        await repository.MergeAsync(merged, secondaryId, cancellationToken).ConfigureAwait(false);
        return merged;
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

    private static void ValidateOrThrow(Contact contact)
    {
        var issues = ContactValidation.Validate(contact);
        if (issues.Count > 0) throw new ContactValidationException(issues);
    }

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
        for (var i = 0; i < contact.Addresses.Count; i++)
        {
            var address = contact.Addresses[i];
            contact.Addresses[i] = address with
            {
                Label = address.Label.Trim(),
                Street = address.Street.Trim(),
                City = address.City.Trim(),
                Region = address.Region.Trim(),
                PostalCode = address.PostalCode.Trim(),
                Country = address.Country.Trim()
            };
        }
        for (var i = 0; i < contact.Organizations.Count; i++)
        {
            var organization = contact.Organizations[i];
            contact.Organizations[i] = organization with
            {
                Name = organization.Name.Trim(),
                Title = string.IsNullOrWhiteSpace(organization.Title) ? null : organization.Title.Trim(),
                Department = string.IsNullOrWhiteSpace(organization.Department) ? null : organization.Department.Trim()
            };
        }
        for (var i = 0; i < contact.Groups.Count; i++)
            contact.Groups[i] = contact.Groups[i] with { Name = contact.Groups[i].Name.Trim() };
        for (var i = 0; i < contact.Tags.Count; i++)
            contact.Tags[i] = contact.Tags[i] with { Name = contact.Tags[i].Name.Trim() };
    }
}

public sealed class ContactValidationException(IReadOnlyList<ValidationIssue> issues)
    : Exception(string.Join(" ", issues.Select(x => x.Message)))
{
    public IReadOnlyList<ValidationIssue> Issues { get; } = issues;
}
