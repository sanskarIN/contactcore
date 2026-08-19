namespace ContactCore.Domain;

public enum ContactFieldKind
{
    Home,
    Work,
    Mobile,
    Other
}

public sealed record ContactPhone(Guid Id, string Label, string Number, ContactFieldKind Kind = ContactFieldKind.Mobile);
public sealed record ContactEmail(Guid Id, string Label, string Address, ContactFieldKind Kind = ContactFieldKind.Home);
public sealed record ContactAddress(Guid Id, string Label, string Street, string City, string Region, string PostalCode, string Country);
public sealed record ContactOrganization(Guid Id, string Name, string? Title, string? Department);
public sealed record ContactGroup(Guid Id, string Name);
public sealed record ContactTag(Guid Id, string Name);

public sealed class Contact
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public DateOnly? Birthday { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ContactPhone> Phones { get; } = [];
    public List<ContactEmail> Emails { get; } = [];
    public List<ContactAddress> Addresses { get; } = [];
    public List<ContactOrganization> Organizations { get; } = [];
    public List<ContactGroup> Groups { get; } = [];
    public List<ContactTag> Tags { get; } = [];

    public string DisplayName
    {
        get
        {
            var full = $"{GivenName} {FamilyName}".Trim();
            return full.Length > 0 ? full : Nickname.Length > 0 ? Nickname : "Unnamed contact";
        }
    }

    public Contact DeepCopy()
    {
        var copy = new Contact
        {
            Id = Id,
            GivenName = GivenName,
            FamilyName = FamilyName,
            Nickname = Nickname,
            Birthday = Birthday,
            Notes = Notes,
            IsFavorite = IsFavorite,
            IsArchived = IsArchived,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
        copy.Phones.AddRange(Phones);
        copy.Emails.AddRange(Emails);
        copy.Addresses.AddRange(Addresses);
        copy.Organizations.AddRange(Organizations);
        copy.Groups.AddRange(Groups);
        copy.Tags.AddRange(Tags);
        return copy;
    }
}
