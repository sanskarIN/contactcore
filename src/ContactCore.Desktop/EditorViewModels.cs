using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Application;
using ContactCore.Domain;

namespace ContactCore.Desktop;

public sealed partial class PhoneDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [ObservableProperty] private string label = "Mobile";
    [ObservableProperty] private string number = "";
    [ObservableProperty] private ContactFieldKind kind = ContactFieldKind.Mobile;
}

public sealed partial class EmailDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [ObservableProperty] private string label = "Email";
    [ObservableProperty] private string address = "";
    [ObservableProperty] private ContactFieldKind kind = ContactFieldKind.Home;
}

public sealed partial class AddressDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [ObservableProperty] private string label = "Home";
    [ObservableProperty] private string street = "";
    [ObservableProperty] private string city = "";
    [ObservableProperty] private string region = "";
    [ObservableProperty] private string postalCode = "";
    [ObservableProperty] private string country = "";
}

public sealed partial class OrganizationDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string department = "";
}

public sealed partial class ContactDraftViewModel : ObservableObject
{
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    [ObservableProperty] private string givenName = "";
    [ObservableProperty] private string familyName = "";
    [ObservableProperty] private string nickname = "";
    [ObservableProperty] private string birthdayText = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isArchived;
    [ObservableProperty] private string groupsText = "";
    [ObservableProperty] private string tagsText = "";

    public ObservableCollection<PhoneDraftViewModel> Phones { get; } = [];
    public ObservableCollection<EmailDraftViewModel> Emails { get; } = [];
    public ObservableCollection<AddressDraftViewModel> Addresses { get; } = [];
    public ObservableCollection<OrganizationDraftViewModel> Organizations { get; } = [];
    public IReadOnlyList<ContactFieldKind> FieldKinds { get; } = Enum.GetValues<ContactFieldKind>();

    public void Load(Contact contact)
    {
        Id = contact.Id;
        CreatedAt = contact.CreatedAt;
        GivenName = contact.GivenName;
        FamilyName = contact.FamilyName;
        Nickname = contact.Nickname;
        BirthdayText = contact.Birthday?.ToString("yyyy-MM-dd") ?? "";
        Notes = contact.Notes;
        IsFavorite = contact.IsFavorite;
        IsArchived = contact.IsArchived;
        GroupsText = string.Join(", ", contact.Groups.Select(x => x.Name));
        TagsText = string.Join(", ", contact.Tags.Select(x => x.Name));

        Phones.Clear();
        foreach (var item in contact.Phones) Phones.Add(new PhoneDraftViewModel { Id = item.Id, Label = item.Label, Number = item.Number, Kind = item.Kind });
        Emails.Clear();
        foreach (var item in contact.Emails) Emails.Add(new EmailDraftViewModel { Id = item.Id, Label = item.Label, Address = item.Address, Kind = item.Kind });
        Addresses.Clear();
        foreach (var item in contact.Addresses)
            Addresses.Add(new AddressDraftViewModel { Id = item.Id, Label = item.Label, Street = item.Street, City = item.City, Region = item.Region, PostalCode = item.PostalCode, Country = item.Country });
        Organizations.Clear();
        foreach (var item in contact.Organizations)
            Organizations.Add(new OrganizationDraftViewModel { Id = item.Id, Name = item.Name, Title = item.Title ?? "", Department = item.Department ?? "" });
    }

    public Contact ToContact()
    {
        DateOnly? birthday = null;
        if (!string.IsNullOrWhiteSpace(BirthdayText))
        {
            if (!DateOnly.TryParseExact(BirthdayText.Trim(), "yyyy-MM-dd", out var parsed)) throw new FormatException("Birthday must use yyyy-MM-dd.");
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

        foreach (var item in Phones.Where(x => !string.IsNullOrWhiteSpace(x.Number)))
            contact.Phones.Add(new(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Label.Trim(), item.Number.Trim(), item.Kind));
        foreach (var item in Emails.Where(x => !string.IsNullOrWhiteSpace(x.Address)))
            contact.Emails.Add(new(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Label.Trim(), item.Address.Trim(), item.Kind));
        foreach (var item in Addresses.Where(x => !string.IsNullOrWhiteSpace(x.Street) || !string.IsNullOrWhiteSpace(x.City) || !string.IsNullOrWhiteSpace(x.Country)))
            contact.Addresses.Add(new(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Label.Trim(), item.Street.Trim(), item.City.Trim(), item.Region.Trim(), item.PostalCode.Trim(), item.Country.Trim()));
        foreach (var item in Organizations.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            contact.Organizations.Add(new(item.Id == Guid.Empty ? Guid.NewGuid() : item.Id, item.Name.Trim(), NullIfBlank(item.Title), NullIfBlank(item.Department)));

        foreach (var name in SplitLabels(GroupsText)) contact.Groups.Add(new(Guid.NewGuid(), name));
        foreach (var name in SplitLabels(TagsText)) contact.Tags.Add(new(Guid.NewGuid(), name));
        return contact;
    }

    [RelayCommand] private void AddPhone() => Phones.Add(new PhoneDraftViewModel());
    [RelayCommand] private void RemovePhone(PhoneDraftViewModel? item) { if (item is not null) Phones.Remove(item); }
    [RelayCommand] private void AddEmail() => Emails.Add(new EmailDraftViewModel());
    [RelayCommand] private void RemoveEmail(EmailDraftViewModel? item) { if (item is not null) Emails.Remove(item); }
    [RelayCommand] private void AddAddress() => Addresses.Add(new AddressDraftViewModel());
    [RelayCommand] private void RemoveAddress(AddressDraftViewModel? item) { if (item is not null) Addresses.Remove(item); }
    [RelayCommand] private void AddOrganization() => Organizations.Add(new OrganizationDraftViewModel());
    [RelayCommand] private void RemoveOrganization(OrganizationDraftViewModel? item) { if (item is not null) Organizations.Remove(item); }

    private static IEnumerable<string> SplitLabels(string value) => value
        .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class DuplicatePairViewModel(DuplicateCandidate candidate)
{
    public DuplicateCandidate Candidate { get; } = candidate;
    public string PrimaryName => candidate.Left.DisplayName;
    public string SecondaryName => candidate.Right.DisplayName;
    public string ScoreText => $"{candidate.Score:P0}";
    public string ReasonsText => string.Join(" • ", candidate.Reasons);
    public string Summary => $"{PrimaryName} ↔ {SecondaryName} — {ScoreText}";
}
