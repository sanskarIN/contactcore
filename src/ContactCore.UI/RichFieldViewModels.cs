using CommunityToolkit.Mvvm.ComponentModel;
using ContactCore.Application;
using ContactCore.Domain;

namespace ContactCore.UI;

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
    [ObservableProperty] private string label = "";
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

public sealed partial class GroupDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? OriginalName { get; init; }
    [ObservableProperty] private string name = "";
}

public sealed partial class TagDraftViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? OriginalName { get; init; }
    [ObservableProperty] private string name = "";
}

public sealed partial class ContactListItemViewModel(Contact contact) : ObservableObject
{
    public Contact Model { get; } = contact;
    public string DisplayName => Model.DisplayName;
    public string Subtitle => Model.Emails.FirstOrDefault()?.Address ?? Model.Phones.FirstOrDefault()?.Number ?? "No contact details";
    public bool IsFavorite => Model.IsFavorite;
    public bool IsArchived => Model.IsArchived;
    public string Initials
    {
        get
        {
            var parts = Model.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }
    }
}

public sealed class DuplicatePairViewModel(DuplicateCandidate candidate)
{
    public DuplicateCandidate Candidate { get; } = candidate;
    public string PrimaryName => Candidate.Left.DisplayName;
    public string SecondaryName => Candidate.Right.DisplayName;
    public string PrimaryDetails => Describe(Candidate.Left);
    public string SecondaryDetails => Describe(Candidate.Right);
    public string ScoreText => $"{Candidate.Score:P0}";
    public string ReasonsText => Candidate.Reasons.Count == 0 ? "No matching signals" : string.Join(" • ", Candidate.Reasons);
    public string Summary => $"{PrimaryName} ↔ {SecondaryName} — {ScoreText}";
    public string MergePreview =>
        $"Match score {ScoreText}. The kept contact retains its identity. Unique phones, emails, addresses, organizations, groups and tags are combined, notes are combined, and favorite state is preserved if either record is favorite.";

    private static string Describe(Contact contact)
    {
        var lines = new List<string>();
        if (contact.Phones.Count > 0) lines.Add("Phones: " + string.Join(", ", contact.Phones.Select(x => x.Number)));
        if (contact.Emails.Count > 0) lines.Add("Emails: " + string.Join(", ", contact.Emails.Select(x => x.Address)));
        if (contact.Birthday is not null) lines.Add($"Birthday: {contact.Birthday:yyyy-MM-dd}");
        if (contact.Addresses.Count > 0) lines.Add($"Addresses: {contact.Addresses.Count}");
        if (contact.Organizations.Count > 0) lines.Add("Organizations: " + string.Join(", ", contact.Organizations.Select(x => x.Name)));
        if (contact.Groups.Count > 0) lines.Add("Groups: " + string.Join(", ", contact.Groups.Select(x => x.Name)));
        if (contact.Tags.Count > 0) lines.Add("Tags: " + string.Join(", ", contact.Tags.Select(x => x.Name)));
        if (!string.IsNullOrWhiteSpace(contact.Notes)) lines.Add("Notes: present");
        if (contact.IsFavorite) lines.Add("Favorite: yes");
        if (contact.IsArchived) lines.Add("Archived: yes");
        return lines.Count == 0 ? "No additional details" : string.Join(Environment.NewLine, lines);
    }
}
