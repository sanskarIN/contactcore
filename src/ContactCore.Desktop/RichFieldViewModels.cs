using CommunityToolkit.Mvvm.ComponentModel;
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

public sealed class DuplicatePairViewModel(DuplicateCandidate candidate)
{
    public DuplicateCandidate Candidate { get; } = candidate;
    public string PrimaryName => candidate.Left.DisplayName;
    public string SecondaryName => candidate.Right.DisplayName;
    public string ScoreText => $"{candidate.Score:P0}";
    public string ReasonsText => candidate.Reasons.Count == 0 ? "No matching signals" : string.Join(" • ", candidate.Reasons);
    public string Summary => $"{PrimaryName} ↔ {SecondaryName} — {ScoreText}";
}
