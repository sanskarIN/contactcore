using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

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

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ContactService _service;
    private readonly IBackupService _backup;
    private readonly IAppPreferences _preferences;
    private readonly AppPaths _paths;
    private CancellationTokenSource? _searchCts;
    private char? _letter;

    public MainWindowViewModel(ContactService service, IBackupService backup, IAppPreferences preferences, AppPaths paths)
    {
        _service = service;
        _backup = backup;
        _preferences = preferences;
        _paths = paths;
        Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()).ToArray();
        Draft = new ContactDraftViewModel();
        SelectedTheme = preferences.Theme;
        ReducedMotion = preferences.ReducedMotion;
        ConfirmPermanentDelete = preferences.ConfirmPermanentDelete;
    }

    public Action? FocusSearchRequested { get; set; }
    public Action<string>? ThemeRequested { get; set; }
    public ObservableCollection<ContactListItemViewModel> Contacts { get; } = [];
    public ObservableCollection<DuplicatePairViewModel> DuplicatePairs { get; } = [];
    public IReadOnlyList<string> Alphabet { get; }
    public IReadOnlyList<string> ThemeChoices { get; } = ["System", "Light", "Dark"];
    public ContactDraftViewModel Draft { get; }
    public string DataDirectory => _paths.DataDirectory;
    public string VersionText => typeof(MainWindowViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private ContactListItemViewModel? selectedContact;
    [ObservableProperty] private bool favoritesOnly;
    [ObservableProperty] private bool archivedOnly;
    [ObservableProperty] private bool showAll = true;
    [ObservableProperty] private bool isEditorVisible;
    [ObservableProperty] private bool isWelcomeVisible;
    [ObservableProperty] private bool isSettingsVisible;
    [ObservableProperty] private bool isDataToolsVisible;
    [ObservableProperty] private bool isDuplicatesVisible;
    [ObservableProperty] private bool isDeleteConfirmVisible;
    [ObservableProperty] private bool hasNoContacts;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string editorTitle = "Contact details";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string listHeading = "All contacts";
    [ObservableProperty] private string resultCountText = "0 contacts";
    [ObservableProperty] private string footerText = "Ready";
    [ObservableProperty] private string dataToolsMessage = "Import or export CSV/vCard files, or create and restore SQLite backups.";
    [ObservableProperty] private string duplicateMessage = "Scan your address book to find likely duplicates.";
    [ObservableProperty] private DuplicatePairViewModel? selectedDuplicate;
    [ObservableProperty] private string selectedTheme = "System";
    [ObservableProperty] private bool reducedMotion;
    [ObservableProperty] private bool confirmPermanentDelete = true;

    partial void OnSearchTextChanged(string value) => _ = DebouncedRefreshAsync();
    partial void OnSelectedContactChanged(ContactListItemViewModel? value)
    {
        if (value is null) return;
        Draft.Load(value.Model.DeepCopy());
        IsEditorVisible = true;
        EditorTitle = value.DisplayName;
        StatusMessage = "";
        CloseOverlays();
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            FooterText = "Opening local database…";
            await _service.InitializeAsync();
            await RefreshAsync();
            IsWelcomeVisible = !_preferences.HasCompletedOnboarding && await _service.CountAsync() == 0;
            ThemeRequested?.Invoke(_preferences.Theme);
            FooterText = "Ready";
        }
        catch (Exception ex)
        {
            FooterText = "Could not initialize ContactCore";
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void FinishOnboarding()
    {
        _preferences.HasCompletedOnboarding = true;
        _preferences.Save();
        IsWelcomeVisible = false;
        NewContact();
    }

    [RelayCommand] private void NewContact()
    {
        SelectedContact = null;
        Draft.Load(new Contact());
        IsEditorVisible = true;
        EditorTitle = "New contact";
        StatusMessage = "";
        CloseOverlays();
    }

    [RelayCommand] private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;
            var saved = Draft.ToContact();
            await _service.SaveAsync(saved);
            StatusMessage = "Saved locally.";
            await RefreshAsync();
            SelectedContact = Contacts.FirstOrDefault(x => x.Model.Id == saved.Id);
        }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private async Task ArchiveAsync()
    {
        if (Draft.Id == Guid.Empty) return;
        try
        {
            Draft.IsArchived = !Draft.IsArchived;
            await SaveAsync();
            StatusMessage = Draft.IsArchived ? "Contact archived." : "Contact restored from archive.";
        }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
    }

    [RelayCommand] private async Task DeleteAsync()
    {
        if (Draft.Id == Guid.Empty) { CancelEdit(); return; }
        if (_preferences.ConfirmPermanentDelete)
        {
            IsDeleteConfirmVisible = true;
            return;
        }
        await DeleteConfirmedAsync();
    }

    [RelayCommand] private async Task DeleteConfirmedAsync()
    {
        if (Draft.Id == Guid.Empty) return;
        try
        {
            IsBusy = true;
            await _service.DeleteAsync(Draft.Id);
            IsDeleteConfirmVisible = false;
            IsEditorVisible = false;
            SelectedContact = null;
            StatusMessage = "Contact permanently deleted.";
            await RefreshAsync();
        }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void CancelDelete() => IsDeleteConfirmVisible = false;
    [RelayCommand] private void CancelEdit() { IsEditorVisible = false; SelectedContact = null; StatusMessage = ""; IsDeleteConfirmVisible = false; }
    [RelayCommand] private async Task ShowAllAsync() { ShowAll = true; FavoritesOnly = false; ArchivedOnly = false; _letter = null; ListHeading = "All contacts"; await RefreshAsync(); }
    [RelayCommand] private async Task FavoritesAsync() { ShowAll = false; FavoritesOnly = true; ArchivedOnly = false; _letter = null; ListHeading = "Favorites"; await RefreshAsync(); }
    [RelayCommand] private async Task ArchivedAsync() { ShowAll = false; FavoritesOnly = false; ArchivedOnly = true; _letter = null; ListHeading = "Archived"; await RefreshAsync(); }
    [RelayCommand] private async Task FilterLetterAsync(string letter) { _letter = string.IsNullOrEmpty(letter) ? null : letter[0]; ListHeading = $"Contacts — {letter}"; await RefreshAsync(); }

    [RelayCommand] private async Task FindDuplicatesAsync()
    {
        CloseOverlays();
        IsDuplicatesVisible = true;
        DuplicatePairs.Clear();
        try
        {
            IsBusy = true;
            var all = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
            foreach (var candidate in new DuplicateDetector().Find(all)) DuplicatePairs.Add(new(candidate));
            SelectedDuplicate = DuplicatePairs.FirstOrDefault();
            DuplicateMessage = DuplicatePairs.Count == 0
                ? "No likely duplicates found."
                : $"Found {DuplicatePairs.Count} likely duplicate pair(s). Select a pair to preview the reasons, then merge if appropriate.";
        }
        catch (Exception ex) { DuplicateMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private async Task MergeSelectedDuplicateAsync()
    {
        if (SelectedDuplicate is null) return;
        try
        {
            IsBusy = true;
            var candidate = SelectedDuplicate.Candidate;
            var merged = await _service.MergeAsync(candidate.Left.Id, candidate.Right.Id);
            DuplicateMessage = $"Merged into {merged.DisplayName}. The secondary duplicate was removed.";
            await RefreshAsync();
            await FindDuplicatesAsync();
        }
        catch (Exception ex) { DuplicateMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void ShowDataTools()
    {
        CloseOverlays();
        IsDataToolsVisible = true;
    }

    public async Task ImportTextAsync(string text, string fileName)
    {
        try
        {
            IsBusy = true;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var result = extension is ".vcf" or ".vcard" ? VCardCodec.Import(text) : ContactCsvCodec.Import(text);
            var imported = await _service.ImportAsync(result.Contacts);
            await RefreshAsync();
            DataToolsMessage = $"Imported {imported} contact(s)." + (result.Warnings.Count > 0 ? $" {result.Warnings.Count} warning(s): {string.Join(" ", result.Warnings.Take(3))}" : "");
        }
        catch (Exception ex) { DataToolsMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    public async Task<string> ExportTextAsync(string format)
    {
        var contacts = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
        return format.Equals("vcard", StringComparison.OrdinalIgnoreCase) ? VCardCodec.Export(contacts) : ContactCsvCodec.Export(contacts);
    }

    public async Task CreateBackupAsync(string destinationDirectory)
    {
        try
        {
            IsBusy = true;
            var path = await _backup.CreateBackupAsync(destinationDirectory);
            DataToolsMessage = $"Backup created: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { DataToolsMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    public async Task RestoreBackupAsync(string backupPath)
    {
        try
        {
            IsBusy = true;
            await _backup.RestoreBackupAsync(backupPath);
            await _service.InitializeAsync();
            await RefreshAsync();
            DataToolsMessage = $"Backup restored: {Path.GetFileName(backupPath)}";
        }
        catch (Exception ex) { DataToolsMessage = RedactingLog.Sanitize(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void ShowSettings()
    {
        CloseOverlays();
        SelectedTheme = _preferences.Theme;
        ReducedMotion = _preferences.ReducedMotion;
        ConfirmPermanentDelete = _preferences.ConfirmPermanentDelete;
        IsSettingsVisible = true;
    }

    [RelayCommand] private void SaveSettings()
    {
        _preferences.Theme = SelectedTheme;
        _preferences.ReducedMotion = ReducedMotion;
        _preferences.ConfirmPermanentDelete = ConfirmPermanentDelete;
        _preferences.Save();
        ThemeRequested?.Invoke(SelectedTheme);
        StatusMessage = "Settings saved.";
        IsSettingsVisible = false;
    }

    [RelayCommand] private void CloseOverlay() => CloseOverlays();

    private void CloseOverlays()
    {
        IsSettingsVisible = false;
        IsDataToolsVisible = false;
        IsDuplicatesVisible = false;
    }

    private async Task DebouncedRefreshAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(180, _searchCts.Token);
            await RefreshAsync(_searchCts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var query = new ContactQuery(SearchText, FavoritesOnly, IncludeArchived: ArchivedOnly, StartsWith: _letter);
        var contacts = await _service.SearchAsync(query, cancellationToken);
        if (ArchivedOnly) contacts = contacts.Where(x => x.IsArchived).ToArray();
        Contacts.Clear();
        foreach (var contact in contacts) Contacts.Add(new(contact));
        ResultCountText = $"{Contacts.Count} {(Contacts.Count == 1 ? "contact" : "contacts")}";
        HasNoContacts = Contacts.Count == 0;
    }
}
