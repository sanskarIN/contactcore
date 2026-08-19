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
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    [ObservableProperty] private string givenName = "";
    [ObservableProperty] private string familyName = "";
    [ObservableProperty] private string nickname = "";
    [ObservableProperty] private string birthdayText = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private bool isFavorite;

    public void Load(Contact contact)
    {
        Id = contact.Id;
        CreatedAt = contact.CreatedAt;
        GivenName = contact.GivenName;
        FamilyName = contact.FamilyName;
        Nickname = contact.Nickname;
        BirthdayText = contact.Birthday?.ToString("yyyy-MM-dd") ?? "";
        Phone = contact.Phones.FirstOrDefault()?.Number ?? "";
        Email = contact.Emails.FirstOrDefault()?.Address ?? "";
        Notes = contact.Notes;
        IsFavorite = contact.IsFavorite;
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
            UpdatedAt = DateTimeOffset.UtcNow
        };
        if (!string.IsNullOrWhiteSpace(Phone)) contact.Phones.Add(new(Guid.NewGuid(), "Mobile", Phone.Trim()));
        if (!string.IsNullOrWhiteSpace(Email)) contact.Emails.Add(new(Guid.NewGuid(), "Email", Email.Trim()));
        return contact;
    }
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
    [ObservableProperty] private string editorTitle = "Contact details";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string listHeading = "All contacts";
    [ObservableProperty] private string resultCountText = "0 contacts";
    [ObservableProperty] private string footerText = "Ready";
    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private bool reducedMotion;
    private char? _letter;

    partial void OnSearchTextChanged(string value) => _ = DebouncedRefreshAsync();

    partial void OnSelectedContactChanged(ContactListItemViewModel? value)
    {
        if (value is null) return;
        IsSettingsVisible = false;
        IsDataToolsVisible = false;
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
        IsSettingsVisible = false;
        IsDataToolsVisible = false;
        Draft.Load(new Contact());
        IsEditorVisible = true;
        EditorTitle = "New contact";
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await _service.SaveAsync(Draft.ToContact());
            StatusMessage = "Saved locally.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Draft.Id == Guid.Empty)
        {
            CancelEdit();
            return;
        }

        try
        {
            await _service.DeleteAsync(Draft.Id);
            IsEditorVisible = false;
            SelectedContact = null;
            StatusMessage = "Contact permanently deleted.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (IsSettingsVisible || IsDataToolsVisible)
        {
            IsSettingsVisible = false;
            IsDataToolsVisible = false;
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
        var all = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
        var duplicates = new DuplicateDetector().Find(all);
        StatusMessage = duplicates.Count == 0
            ? "No likely duplicates found."
            : $"Found {duplicates.Count} likely duplicate pair(s). Highest score: {duplicates[0].Score:P0}.";
    }

    [RelayCommand]
    private void ShowDataTools()
    {
        SelectedContact = null;
        IsEditorVisible = false;
        IsSettingsVisible = false;
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
        IsEditorVisible = false;
        IsDataToolsVisible = false;
        SelectedTheme = NormalizeTheme(_preferences.Theme);
        ReducedMotion = _preferences.ReducedMotion;
        IsSettingsVisible = true;
        StatusMessage = "";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SelectedTheme = NormalizeTheme(SelectedTheme);
        _preferences.Theme = SelectedTheme;
        _preferences.ReducedMotion = ReducedMotion;
        _preferences.Save();
        ThemeChangeRequested?.Invoke(SelectedTheme);
        IsSettingsVisible = false;
        StatusMessage = "Settings saved locally.";
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
