using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Application;
using ContactCore.Domain;

namespace ContactCore.UI;

public sealed record PickedTextFile(string Name, string Content);
public sealed record PickedBackupFile(string Path, bool DeleteAfterUse = false);

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ContactService _service;
    private readonly IBackupService _backup;
    private readonly IAppPreferences _preferences;
    private readonly AppPlatformCapabilities _capabilities;
    private CancellationTokenSource? _searchCts;
    private Func<Task>? _pendingConfirmedAction;
    private char? _letter;

    public MainViewModel(AppServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _service = services.ContactService;
        _backup = services.BackupService;
        _preferences = services.Preferences;
        _capabilities = services.Capabilities;
        Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()).ToArray();
        ThemeOptions = ["System", "Light", "Dark"];
        Draft = new ContactDraftViewModel();
    }

    public Action? FocusSearchRequested { get; set; }
    public Action<string>? ThemeChangeRequested { get; set; }
    public Func<Task<PickedTextFile?>>? PickImportTextRequested { get; set; }
    public Func<string, string, Task<bool>>? SaveTextRequested { get; set; }
    public Func<Task<PickedBackupFile?>>? PickBackupFileRequested { get; set; }

    public ObservableCollection<ContactListItemViewModel> Contacts { get; } = [];
    public ObservableCollection<DuplicatePairViewModel> DuplicatePairs { get; } = [];
    public IReadOnlyList<string> Alphabet { get; }
    public IReadOnlyList<string> ThemeOptions { get; }
    public ContactDraftViewModel Draft { get; }

    public string PlatformName => _capabilities.PlatformName;
    public string DataLocation => _capabilities.DataLocation;
    public string BackupLocation => _capabilities.BackupLocation;
    public bool CanUseDatabaseBackups => _capabilities.SupportsDatabaseBackups;
    public bool CanUseDatabaseEncryption => _capabilities.SupportsDatabaseEncryption;
    public string AboutSummary => $"ContactCore 2.0.12 • {_capabilities.PlatformName} • MIT License • Made by the Sanskar";
    public string SupportSummary => "sanskarin@outlook.in • supportramsandesh@gmail.com";
    public string ProjectSummary => "github.com/sanskarIN/contactcore • buymeacoffee.com/sanskarIN";
    public bool IsListVisible => !IsEditorVisible && !IsSettingsVisible && !IsDataToolsVisible && !IsDuplicatesVisible;

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
    [ObservableProperty] private bool isConfirmationVisible;
    [ObservableProperty] private string confirmationMessage = "";

    partial void OnSearchTextChanged(string value) => _ = DebouncedRefreshAsync();
    partial void OnIsEditorVisibleChanged(bool value) => OnPropertyChanged(nameof(IsListVisible));
    partial void OnIsSettingsVisibleChanged(bool value) => OnPropertyChanged(nameof(IsListVisible));
    partial void OnIsDataToolsVisibleChanged(bool value) => OnPropertyChanged(nameof(IsListVisible));
    partial void OnIsDuplicatesVisibleChanged(bool value) => OnPropertyChanged(nameof(IsListVisible));

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
            FooterText = "Opening local contact store…";
            await _service.InitializeAsync();
            await RefreshAsync();
            FooterText = "Ready";
        }
        catch (Exception ex)
        {
            FooterText = "Could not initialize ContactCore";
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private void ShowContacts()
    {
        SelectedContact = null;
        HideDetailViews();
        StatusMessage = "";
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
        if (!IsEditorVisible)
        {
            StatusMessage = "Open or create a contact before saving.";
            return;
        }

        try
        {
            var saved = Draft.ToContact();
            await _service.SaveAsync(saved);
            Draft.Load(saved, isPersisted: true);
            await RefreshAsync();
            SelectedContact = Contacts.FirstOrDefault(x => x.Model.Id == saved.Id);
            StatusMessage = "Saved locally.";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        HideDetailViews();
        SelectedContact = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task RequestDeleteAsync()
    {
        if (!Draft.IsPersisted)
        {
            CancelEdit();
            StatusMessage = "Unsaved contact discarded.";
            return;
        }

        if (_preferences.ConfirmPermanentDelete)
        {
            QueueConfirmation(
                "Permanently delete this contact? Existing backups and exports are separate copies.",
                DeleteDraftAsync);
            return;
        }

        await DeleteDraftAsync();
    }

    private async Task DeleteDraftAsync()
    {
        try
        {
            await _service.DeleteAsync(Draft.Id);
            HideDetailViews();
            SelectedContact = null;
            StatusMessage = "Contact permanently deleted.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
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
    private void MergeSelectedDuplicate()
    {
        if (SelectedDuplicate is null)
        {
            DuplicateMessage = "Select a duplicate pair first.";
            return;
        }

        var pair = SelectedDuplicate;
        QueueConfirmation(
            $"Merge {pair.SecondaryName} into {pair.PrimaryName}? The first record is kept and the second is permanently removed.",
            () => MergeDuplicateAsync(pair.Candidate.Left.Id, pair.Candidate.Right.Id));
    }

    [RelayCommand]
    private void MergeSelectedDuplicateIntoSecondary()
    {
        if (SelectedDuplicate is null)
        {
            DuplicateMessage = "Select a duplicate pair first.";
            return;
        }

        var pair = SelectedDuplicate;
        QueueConfirmation(
            $"Merge {pair.PrimaryName} into {pair.SecondaryName}? The second record is kept and the first is permanently removed.",
            () => MergeDuplicateAsync(pair.Candidate.Right.Id, pair.Candidate.Left.Id));
    }

    private async Task MergeDuplicateAsync(Guid primaryId, Guid secondaryId)
    {
        try
        {
            FooterText = "Merging duplicate contacts…";
            var merged = await _service.MergeAsync(primaryId, secondaryId);
            await RefreshAsync();
            await RefreshDuplicatesAsync();
            DuplicateMessage = $"Merged duplicate into {merged.DisplayName}.";
        }
        catch (Exception ex)
        {
            DuplicateMessage = SafeMessage(ex);
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
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private Task ExportCsvAsync() => ExportTextAsync("contactcore-contacts.csv", ContactCsvCodec.Export);

    [RelayCommand]
    private Task ExportVCardAsync() => ExportTextAsync("contactcore-contacts.vcf", VCardCodec.Export);

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!CanUseDatabaseBackups)
        {
            StatusMessage = "Native database backups are not available on this platform. Use CSV or vCard export instead.";
            return;
        }

        try
        {
            Directory.CreateDirectory(BackupLocation);
            var path = await _backup.CreateBackupAsync(BackupLocation);
            StatusMessage = $"Verified backup created: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (!CanUseDatabaseBackups || PickBackupFileRequested is null)
        {
            StatusMessage = "Native database restore is not available on this platform.";
            return;
        }

        var picked = await PickBackupFileRequested();
        if (picked is null) return;
        QueueConfirmation(
            "Restore this ContactCore backup? A snapshot of the current database is retained before replacement.",
            () => RestorePickedBackupAsync(picked));
    }

    private async Task RestorePickedBackupAsync(PickedBackupFile picked)
    {
        try
        {
            FooterText = "Restoring verified backup…";
            await _backup.RestoreBackupAsync(picked.Path);
            await _service.InitializeAsync();
            SelectedContact = null;
            HideDetailViews();
            await RefreshAsync();
            StatusMessage = "Backup restored successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
        finally
        {
            FooterText = "Ready";
            if (picked.DeleteAfterUse)
            {
                try { File.Delete(picked.Path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
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

    [RelayCommand]
    private async Task ConfirmPendingAsync()
    {
        var action = _pendingConfirmedAction;
        _pendingConfirmedAction = null;
        IsConfirmationVisible = false;
        ConfirmationMessage = "";
        if (action is not null) await action();
    }

    [RelayCommand]
    private void CancelPending()
    {
        _pendingConfirmedAction = null;
        IsConfirmationVisible = false;
        ConfirmationMessage = "";
    }

    private void QueueConfirmation(string message, Func<Task> action)
    {
        _pendingConfirmedAction = action;
        ConfirmationMessage = message;
        IsConfirmationVisible = true;
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
            DuplicateMessage = SafeMessage(ex);
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
            StatusMessage = SafeMessage(ex);
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

    private static string SafeMessage(Exception ex)
    {
        var message = string.IsNullOrWhiteSpace(ex.Message) ? "The operation failed." : ex.Message;
        foreach (var path in new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.GetTempPath()
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            message = message.Replace(path, "[local path]", StringComparison.OrdinalIgnoreCase);
        }
        return message.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
