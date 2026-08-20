using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Domain;

namespace ContactCore.UI;

public sealed partial class ContactDraftViewModel : ObservableObject
{
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsPersisted { get; private set; }
    public IReadOnlyList<ContactFieldKind> FieldKinds { get; } = Enum.GetValues<ContactFieldKind>();
    public ObservableCollection<PhoneDraftViewModel> Phones { get; } = [];
    public ObservableCollection<EmailDraftViewModel> Emails { get; } = [];
    public ObservableCollection<AddressDraftViewModel> Addresses { get; } = [];
    public ObservableCollection<OrganizationDraftViewModel> Organizations { get; } = [];
    public ObservableCollection<GroupDraftViewModel> Groups { get; } = [];
    public ObservableCollection<TagDraftViewModel> Tags { get; } = [];

    [ObservableProperty] private string givenName = "";
    [ObservableProperty] private string familyName = "";
    [ObservableProperty] private string nickname = "";
    [ObservableProperty] private string birthdayText = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isArchived;

    public void Load(Contact contact, bool isPersisted = true)
    {
        ArgumentNullException.ThrowIfNull(contact);
        Id = contact.Id;
        CreatedAt = contact.CreatedAt;
        IsPersisted = isPersisted;
        GivenName = contact.GivenName;
        FamilyName = contact.FamilyName;
        Nickname = contact.Nickname;
        BirthdayText = contact.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        Notes = contact.Notes;
        IsFavorite = contact.IsFavorite;
        IsArchived = contact.IsArchived;

        Phones.Clear();
        foreach (var phone in contact.Phones)
            Phones.Add(new PhoneDraftViewModel { Id = phone.Id, Label = phone.Label, Number = phone.Number, Kind = phone.Kind });

        Emails.Clear();
        foreach (var email in contact.Emails)
            Emails.Add(new EmailDraftViewModel { Id = email.Id, Label = email.Label, Address = email.Address, Kind = email.Kind });

        Addresses.Clear();
        foreach (var address in contact.Addresses)
        {
            Addresses.Add(new AddressDraftViewModel
            {
                Id = address.Id,
                Label = address.Label,
                Street = address.Street,
                City = address.City,
                Region = address.Region,
                PostalCode = address.PostalCode,
                Country = address.Country
            });
        }

        Organizations.Clear();
        foreach (var organization in contact.Organizations)
        {
            Organizations.Add(new OrganizationDraftViewModel
            {
                Id = organization.Id,
                Name = organization.Name,
                Title = organization.Title ?? "",
                Department = organization.Department ?? ""
            });
        }

        Groups.Clear();
        foreach (var group in contact.Groups)
            Groups.Add(new GroupDraftViewModel { Id = group.Id, OriginalName = group.Name, Name = group.Name });

        Tags.Clear();
        foreach (var tag in contact.Tags)
            Tags.Add(new TagDraftViewModel { Id = tag.Id, OriginalName = tag.Name, Name = tag.Name });
    }

    public Contact ToContact()
    {
        DateOnly? birthday = null;
        if (!string.IsNullOrWhiteSpace(BirthdayText))
        {
            if (!DateOnly.TryParseExact(BirthdayText.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                throw new FormatException("Birthday must use yyyy-MM-dd.");
            birthday = parsed;
        }

        var contact = new Contact
        {
            Id = Id == Guid.Empty ? Guid.NewGuid() : Id,
            CreatedAt = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt,
            GivenName = GivenName,
            FamilyName = FamilyName,
            Nickname = Nickname,
            Birthday = birthday,
            Notes = Notes,
            IsFavorite = IsFavorite,
            IsArchived = IsArchived,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var phone in Phones.Where(x => !string.IsNullOrWhiteSpace(x.Number)))
            contact.Phones.Add(new(phone.Id == Guid.Empty ? Guid.NewGuid() : phone.Id, phone.Label.Trim(), phone.Number.Trim(), phone.Kind));

        foreach (var email in Emails.Where(x => !string.IsNullOrWhiteSpace(x.Address)))
            contact.Emails.Add(new(email.Id == Guid.Empty ? Guid.NewGuid() : email.Id, email.Label.Trim(), email.Address.Trim(), email.Kind));

        foreach (var address in Addresses.Where(HasAddressValue))
        {
            contact.Addresses.Add(new(
                address.Id == Guid.Empty ? Guid.NewGuid() : address.Id,
                address.Label.Trim(),
                address.Street.Trim(),
                address.City.Trim(),
                address.Region.Trim(),
                address.PostalCode.Trim(),
                address.Country.Trim()));
        }

        foreach (var organization in Organizations.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            contact.Organizations.Add(new(
                organization.Id == Guid.Empty ? Guid.NewGuid() : organization.Id,
                organization.Name.Trim(),
                NullIfBlank(organization.Title),
                NullIfBlank(organization.Department)));
        }

        AddDistinctGroups(contact);
        AddDistinctTags(contact);
        return contact;
    }

    [RelayCommand] private void AddPhone() => Phones.Add(new PhoneDraftViewModel());
    [RelayCommand] private void RemovePhone(PhoneDraftViewModel? value) { if (value is not null) Phones.Remove(value); }
    [RelayCommand] private void AddEmail() => Emails.Add(new EmailDraftViewModel());
    [RelayCommand] private void RemoveEmail(EmailDraftViewModel? value) { if (value is not null) Emails.Remove(value); }
    [RelayCommand] private void AddAddress() => Addresses.Add(new AddressDraftViewModel());
    [RelayCommand] private void RemoveAddress(AddressDraftViewModel? value) { if (value is not null) Addresses.Remove(value); }
    [RelayCommand] private void AddOrganization() => Organizations.Add(new OrganizationDraftViewModel());
    [RelayCommand] private void RemoveOrganization(OrganizationDraftViewModel? value) { if (value is not null) Organizations.Remove(value); }
    [RelayCommand] private void AddGroup() => Groups.Add(new GroupDraftViewModel());
    [RelayCommand] private void RemoveGroup(GroupDraftViewModel? value) { if (value is not null) Groups.Remove(value); }
    [RelayCommand] private void AddTag() => Tags.Add(new TagDraftViewModel());
    [RelayCommand] private void RemoveTag(TagDraftViewModel? value) { if (value is not null) Tags.Remove(value); }

    private void AddDistinctGroups(Contact contact)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in Groups)
        {
            var name = group.Name.Trim();
            if (name.Length == 0 || !names.Add(name)) continue;
            var (id, persistedName) = ResolveSharedDictionaryIdentity(group.Id, group.OriginalName, name);
            contact.Groups.Add(new ContactGroup(id, persistedName));
        }
    }

    private void AddDistinctTags(Contact contact)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in Tags)
        {
            var name = tag.Name.Trim();
            if (name.Length == 0 || !names.Add(name)) continue;
            var (id, persistedName) = ResolveSharedDictionaryIdentity(tag.Id, tag.OriginalName, name);
            contact.Tags.Add(new ContactTag(id, persistedName));
        }
    }

    private static (Guid Id, string Name) ResolveSharedDictionaryIdentity(Guid id, string? originalName, string editedName)
    {
        if (string.IsNullOrWhiteSpace(originalName))
            return (id == Guid.Empty ? Guid.NewGuid() : id, editedName);

        var original = originalName.Trim();
        if (TextNormalizer.SearchKey(original) == TextNormalizer.SearchKey(editedName))
            return (id == Guid.Empty ? Guid.NewGuid() : id, original);

        return (Guid.NewGuid(), editedName);
    }

    private static bool HasAddressValue(AddressDraftViewModel address) =>
        !string.IsNullOrWhiteSpace(address.Label) ||
        !string.IsNullOrWhiteSpace(address.Street) ||
        !string.IsNullOrWhiteSpace(address.City) ||
        !string.IsNullOrWhiteSpace(address.Region) ||
        !string.IsNullOrWhiteSpace(address.PostalCode) ||
        !string.IsNullOrWhiteSpace(address.Country);

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
