using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed record PickedTextFile(string Name, string Content);

public sealed partial class ContactListItemViewModel(Contact contact) : ObservableObject
{
    public Contact Model { get; } = contact;
    public string DisplayName => contact.DisplayName;
    public string Subtitle => contact.Emails.FirstOrDefault()?.Address ?? contact.Phones.FirstOrDefault()?.Number ?? "No contact details";
    public bool IsFavorite => contact.IsFavorite;
    public bool IsArchived => contact.IsArchived;
    public string Initials
    {
        get
        {
            var parts = contact.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }
    }
}

public sealed partial class ContactDraftViewModel : ObservableObject
{
    private Contact? _loadedContact;

    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsPersisted { get; private set; }
    public IReadOnlyList<ContactFieldKind> FieldKinds { get; } = Enum.GetValues<ContactFieldKind>();
    public ObservableCollection<PhoneDraftViewModel> Phones { get; } = [];
    public ObservableCollection<EmailDraftViewModel> Emails { get; } = [];
    public ObservableCollection<AddressDraftViewModel> Addresses { get; } = [];
    public ObservableCollection<OrganizationDraftViewModel> Organizations { get; } = [];

    [ObservableProperty] private string givenName = "";
    [ObservableProperty] private string familyName = "";
    [ObservableProperty] private string nickname = "";
    [ObservableProperty] private string birthdayText = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isArchived;
    [ObservableProperty] private string groupsText = "";
    [ObservableProperty] private string tagsText = "";

    public void Load(Contact contact, bool isPersisted = true)
    {
        ArgumentNullException.ThrowIfNull(contact);
        _loadedContact = contact.DeepCopy();
        Id = contact.Id;
        CreatedAt = contact.CreatedAt;
        IsPersisted = isPersisted;
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
    }

    public Contact ToContact()
    {
        DateOnly? birthday = null;
        if (!string.IsNullOrWhiteSpace(BirthdayText))
        {
            if (!DateOnly.TryParseExact(BirthdayText.Trim(), "yyyy-MM-dd", out var parsed))
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
        {
            contact.Phones.Add(new(
                phone.Id == Guid.Empty ? Guid.NewGuid() : phone.Id,
                LabelOrDefault(phone.Label, phone.Kind.ToString()),
                phone.Number.Trim(),
                phone.Kind));
        }

        foreach (var email in Emails.Where(x => !string.IsNullOrWhiteSpace(x.Address)))
        {
            contact.Emails.Add(new(
                email.Id == Guid.Empty ? Guid.NewGuid() : email.Id,
                LabelOrDefault(email.Label, email.Kind.ToString()),
                email.Address.Trim(),
                email.Kind));
        }

        foreach (var address in Addresses.Where(HasAddressValue))
        {
            contact.Addresses.Add(new(
                address.Id == Guid.Empty ? Guid.NewGuid() : address.Id,
                LabelOrDefault(address.Label, "Address"),
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

        foreach (var groupName in SplitLabels(GroupsText))
            contact.Groups.Add(FindExistingGroup(groupName) ?? new ContactGroup(Guid.NewGuid(), groupName));
        foreach (var tagName in SplitLabels(TagsText))
            contact.Tags.Add(FindExistingTag(tagName) ?? new ContactTag(Guid.NewGuid(), tagName));

        return contact;
    }

    [RelayCommand]
    private void AddPhone() => Phones.Add(new PhoneDraftViewModel());

    [RelayCommand]
    private void RemovePhone(PhoneDraftViewModel? phone)
    {
        if (phone is not null) Phones.Remove(phone);
    }

    [RelayCommand]
    private void AddEmail() => Emails.Add(new EmailDraftViewModel());

    [RelayCommand]
    private void RemoveEmail(EmailDraftViewModel? email)
    {
        if (email is not null) Emails.Remove(email);
    }

    [RelayCommand]
    private void AddAddress() => Addresses.Add(new AddressDraftViewModel());

    [RelayCommand]
    private void RemoveAddress(AddressDraftViewModel? address)
    {
        if (address is not null) Addresses.Remove(address);
    }

    [RelayCommand]
    private void AddOrganization() => Organizations.Add(new OrganizationDraftViewModel());

    [RelayCommand]
    private void RemoveOrganization(OrganizationDraftViewModel? organization)
    {
        if (organization is not null) Organizations.Remove(organization);
    }

    private ContactGroup? FindExistingGroup(string name) => _loadedContact?.Groups.FirstOrDefault(x =>
        TextNormalizer.SearchKey(x.Name) == TextNormalizer.SearchKey(name));

    private ContactTag? FindExistingTag(string name) => _loadedContact?.Tags.FirstOrDefault(x =>
        TextNormalizer.SearchKey(x.Name) == TextNormalizer.SearchKey(name));

    private static bool HasAddressValue(AddressDraftViewModel address) =>
        !string.IsNullOrWhiteSpace(address.Street) ||
        !string.IsNullOrWhiteSpace(address.City) ||
        !string.IsNullOrWhiteSpace(address.Region) ||
        !string.IsNullOrWhiteSpace(address.PostalCode) ||
        !string.IsNullOrWhiteSpace(address.Country);

    private static IEnumerable<string> SplitLabels(string value) => value
        .Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string LabelOrDefault(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NullIfBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ContactService _service;
    private readonly IBackupService _backup;
    private readonly IAppPreferences _preferences;
    private readonly AppPaths _paths;
    private CancellationTokenSource? _searchCts;

    public MainWindowViewModel(ContactService service, IBackupService backup, IAppPreferences preferences, AppPaths paths)
    {
        _service = service;
        _backup = backup;
        _preferences = preferences;
        _paths = paths;
        Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()).ToArray();
        ThemeOptions = ["System", "Light", "Dark"];
        Draft = new ContactDraftViewModel();
    }

    public Action? FocusSearchRequested { get; set; }
    public Action<string>? ThemeChangeRequested { get; set; }
    public Func<Task<PickedTextFile?>>? PickImportTextRequested { get; set; }
    public Func<string, string, Task<bool>>? SaveTextRequested { get; set; }
    public ObservableCollection<ContactListItemViewModel> Contacts { get; } = [];
    public ObservableCollection<DuplicatePairViewModel> DuplicatePairs { get; } = [];
    public IReadOnlyList<string> Alphabet { get; }
    public IReadOnlyList<string> ThemeOptions { get; }
    public ContactDraftViewModel Draft { get; }
    public string DataDirectory => _paths.DataDirectory;
    public string BackupDirectory => _paths.BackupDirectory;
    public string AboutSummary => "ContactCore • MIT License • Made by the Sanskar";
    public string SupportSummary => "sanskarin@outlook.in • supportramsandesh@gmail.com";
    public string ProjectSummary => "github.com/sanskarIN/contactcore • buymeacoffee.com/sanskarIN";

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private ContactListItemViewModel? selectedContact;
    [ObservableProperty] private bool favoritesOnly;
    [ObservableProperty] private bool archivedOnly;
    [ObservableProperty] private bool showAll = true;
    [ObservableProperty] private bool isEditorVisible;
    [ObservableProperty] private bool isSettingsVisible;
    [ObservableProperty] private bool isDataToolsVisible;
    [ObservableProperty] private bool isDuplicatesVisible;
    [ObservableProperty] private string editorTitle = "Contact details";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string listHeading = "All contacts";
    [ObservableProperty] private string resultCountText = "0 contacts";
    [ObservableProperty] private string footerText = "Ready";
    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private bool reducedMotion;
    [ObservableProperty] private bool confirmPermanentDelete = true;
    [ObservableProperty] private DuplicatePairViewModel? selectedDuplicate;
    [ObservableProperty] private string duplicateMessage = "Scan all contacts to review likely duplicate pairs.";
    private char? _letter;

    partial void OnSearchTextChanged(string value) => _ = DebouncedRefreshAsync();

    partial void OnSelectedContactChanged(ContactListItemViewModel? value)
    {
        if (value is null) return;
        HideDetailViews();
        Draft.Load(value.Model.DeepCopy());
        IsEditorVisible = true;
        EditorTitle = value.DisplayName;
        StatusMessage = "";
    }

    public async Task InitializeAsync()
    {
        try
        {
            FooterText = "Opening local database…";
            await _service.InitializeAsync();
            await RefreshAsync();
            FooterText = "Ready";
        }
        catch (Exception ex)
        {
            FooterText = "Could not initialize ContactCore";
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private void NewContact()
    {
        SelectedContact = null;
        HideDetailViews();
        Draft.Load(new Contact(), isPersisted: false);
        IsEditorVisible = true;
        EditorTitle = "New contact";
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var saved = Draft.ToContact();
            await _service.SaveAsync(saved);
            Draft.Load(saved, isPersisted: true);
            StatusMessage = "Saved locally.";
            await RefreshAsync();
            SelectedContact = Contacts.FirstOrDefault(x => x.Model.Id == saved.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (IsSettingsVisible || IsDataToolsVisible || IsDuplicatesVisible)
        {
            HideDetailViews();
            StatusMessage = "";
            return;
        }

        IsEditorVisible = false;
        SelectedContact = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task ShowAllAsync()
    {
        ShowAll = true;
        FavoritesOnly = false;
        ArchivedOnly = false;
        _letter = null;
        ListHeading = "All contacts";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task FavoritesAsync()
    {
        ShowAll = false;
        FavoritesOnly = true;
        ArchivedOnly = false;
        _letter = null;
        ListHeading = "Favorites";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ArchivedAsync()
    {
        ShowAll = false;
        FavoritesOnly = false;
        ArchivedOnly = true;
        _letter = null;
        ListHeading = "Archived";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task FilterLetterAsync(string letter)
    {
        _letter = string.IsNullOrEmpty(letter) ? null : letter[0];
        ListHeading = $"Contacts — {letter}";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        SelectedContact = null;
        HideDetailViews();
        IsDuplicatesVisible = true;
        await RefreshDuplicatesAsync();
    }

    [RelayCommand]
    private async Task MergeSelectedDuplicateAsync()
    {
        if (SelectedDuplicate is null)
        {
            DuplicateMessage = "Select a duplicate pair first.";
            return;
        }
        if (ConfirmActionRequested is null)
        {
            DuplicateMessage = "Duplicate merge is blocked because confirmation is unavailable.";
            return;
        }

        var pair = SelectedDuplicate;
        var confirmed = await ConfirmActionRequested(
            $"Merge {pair.SecondaryName} into {pair.PrimaryName}? The primary contact is kept, unique details are combined, and the secondary contact is permanently removed from the active database.");
        if (!confirmed) return;

        try
        {
            FooterText = "Merging duplicate contacts…";
            var merged = await _service.MergeAsync(pair.Candidate.Left.Id, pair.Candidate.Right.Id);
            await RefreshAsync();
            await RefreshDuplicatesAsync();
            DuplicateMessage = $"Merged duplicate into {merged.DisplayName}. The operation was committed atomically.";
        }
        catch (Exception ex)
        {
            DuplicateMessage = RedactingLog.Sanitize(ex.Message);
        }
        finally
        {
            FooterText = "Ready";
        }
    }

    [RelayCommand]
    private void ShowDataTools()
    {
        SelectedContact = null;
        HideDetailViews();
        IsDataToolsVisible = true;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task ImportContactsAsync()
    {
        if (PickImportTextRequested is null)
        {
            StatusMessage = "File picker is unavailable on this platform.";
            return;
        }

        try
        {
            var picked = await PickImportTextRequested();
            if (picked is null) return;
            var extension = Path.GetExtension(picked.Name);
            var parsed = extension.Equals(".vcf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".vcard", StringComparison.OrdinalIgnoreCase)
                ? VCardCodec.Import(picked.Content)
                : ContactCsvCodec.Import(picked.Content);
            var count = await _service.ImportAsync(parsed.Contacts);
            StatusMessage = parsed.Warnings.Count == 0
                ? $"Imported {count} {(count == 1 ? "contact" : "contacts")} atomically."
                : $"Imported {count} {(count == 1 ? "contact" : "contacts")} with {parsed.Warnings.Count} warning(s).";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        await ExportTextAsync("contactcore-contacts.csv", contacts => ContactCsvCodec.Export(contacts));
    }

    [RelayCommand]
    private async Task ExportVCardAsync()
    {
        await ExportTextAsync("contactcore-contacts.vcf", contacts => VCardCodec.Export(contacts));
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        Directory.CreateDirectory(_paths.BackupDirectory);
        try
        {
            var path = await _backup.CreateBackupAsync(_paths.BackupDirectory);
            StatusMessage = $"Verified backup created: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SelectedContact = null;
        HideDetailViews();
        SelectedTheme = NormalizeTheme(_preferences.Theme);
        ReducedMotion = _preferences.ReducedMotion;
        ConfirmPermanentDelete = _preferences.ConfirmPermanentDelete;
        IsSettingsVisible = true;
        StatusMessage = "";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SelectedTheme = NormalizeTheme(SelectedTheme);
        _preferences.Theme = SelectedTheme;
        _preferences.ReducedMotion = ReducedMotion;
        _preferences.ConfirmPermanentDelete = ConfirmPermanentDelete;
        _preferences.Save();
        ThemeChangeRequested?.Invoke(SelectedTheme);
        IsSettingsVisible = false;
        StatusMessage = "Settings saved locally.";
    }

    private async Task RefreshDuplicatesAsync()
    {
        try
        {
            var all = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
            var candidates = new DuplicateDetector().Find(all);
            DuplicatePairs.Clear();
            foreach (var candidate in candidates) DuplicatePairs.Add(new DuplicatePairViewModel(candidate));
            SelectedDuplicate = DuplicatePairs.FirstOrDefault();
            DuplicateMessage = DuplicatePairs.Count == 0
                ? "No likely duplicates found."
                : $"Found {DuplicatePairs.Count} likely duplicate pair(s). Review the evidence before merging.";
        }
        catch (Exception ex)
        {
            DuplicatePairs.Clear();
            SelectedDuplicate = null;
            DuplicateMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private async Task ExportTextAsync(string suggestedName, Func<IReadOnlyList<Contact>, string> encode)
    {
        if (SaveTextRequested is null)
        {
            StatusMessage = "File picker is unavailable on this platform.";
            return;
        }

        try
        {
            var contacts = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
            var saved = await SaveTextRequested(suggestedName, encode(contacts));
            if (saved) StatusMessage = $"Exported {contacts.Count} {(contacts.Count == 1 ? "contact" : "contacts")}.";
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    private void HideDetailViews()
    {
        IsEditorVisible = false;
        IsSettingsVisible = false;
        IsDataToolsVisible = false;
        IsDuplicatesVisible = false;
    }

    private static string NormalizeTheme(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "light" => "Light",
        "dark" => "Dark",
        _ => "System"
    };

    private async Task DebouncedRefreshAsync()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _searchCts, current);
        if (previous is not null)
        {
            previous.Cancel();
            previous.Dispose();
        }

        try
        {
            await Task.Delay(180, current.Token);
            await RefreshAsync(current.Token);
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested) { }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _searchCts, null, current), current))
                current.Dispose();
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var query = new ContactQuery(SearchText, FavoritesOnly, IncludeArchived: ArchivedOnly, StartsWith: _letter);
        var contacts = await _service.SearchAsync(query, cancellationToken);
        if (ArchivedOnly) contacts = contacts.Where(x => x.IsArchived).ToArray();
        Contacts.Clear();
        foreach (var contact in contacts) Contacts.Add(new(contact));
        ResultCountText = $"{Contacts.Count} {(Contacts.Count == 1 ? "contact" : "contacts")}";
    }
}
