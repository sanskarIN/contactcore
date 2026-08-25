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
    public string AboutSummary => $"ContactCore {ProductVersion} • {_capabilities.PlatformName} • MIT License • Made by the Sanskar";
    public string SupportSummary => "sanskarin@outlook.in • supportramsandesh@gmail.com";
    public string ProjectSummary => "github.com/sanskarIN/contactcore • buymeacoffee.com/sanskarIN";
    public bool IsListVisible => !IsEditorVisible && !IsSettingsVisible && !IsDataToolsVisible && !IsDuplicatesVisible;

    private static string ProductVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "unknown";

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
        HideDetailViews();
        IsDuplicatesVisible = true;
        DuplicatePairs.Clear();
        SelectedDuplicate = null;
        DuplicateMessage = "Scanning local contacts…";

        try
        {
            var all = await _service.ListAsync(new ContactQuery());
            var pairs = DuplicateDetector.Find(all);
            foreach (var pair in pairs)
                DuplicatePairs.Add(new DuplicatePairViewModel(pair));

            SelectedDuplicate = DuplicatePairs.FirstOrDefault();
            DuplicateMessage = DuplicatePairs.Count == 0
                ? "No likely duplicates found."
                : $"{DuplicatePairs.Count} likely duplicate pair{(DuplicatePairs.Count == 1 ? "" : "s")} found. Review the evidence before merging.";
        }
        catch (Exception ex)
        {
            DuplicateMessage = SafeMessage(ex);
        }
    }

    [RelayCommand] private Task MergeSelectedDuplicateAsync() => RequestMergeAsync(keepPrimary: true);
    [RelayCommand] private Task MergeSelectedDuplicateIntoSecondaryAsync() => RequestMergeAsync(keepPrimary: false);

    private Task RequestMergeAsync(bool keepPrimary)
    {
        if (SelectedDuplicate is null)
        {
            DuplicateMessage = "Select a duplicate pair first.";
            return Task.CompletedTask;
        }

        var pair = SelectedDuplicate.Candidate;
        var primary = keepPrimary ? pair.Left : pair.Right;
        var secondary = keepPrimary ? pair.Right : pair.Left;
        QueueConfirmation(
            $"Merge the selected duplicate pair and keep {primary.DisplayName}? The other local record will be permanently removed after its unique data is merged.",
            () => MergeConfirmedAsync(primary.Id, secondary.Id));
        return Task.CompletedTask;
    }

    private async Task MergeConfirmedAsync(Guid primaryId, Guid secondaryId)
    {
        try
        {
            await _service.MergeAsync(primaryId, secondaryId);
            DuplicateMessage = "Duplicate pair merged locally.";
            await FindDuplicatesAsync();
        }
        catch (Exception ex)
        {
            DuplicateMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private void ShowDataTools()
    {
        HideDetailViews();
        IsDataToolsVisible = true;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task ImportContactsAsync()
    {
        if (PickImportTextRequested is null)
        {
            StatusMessage = "Import picker is unavailable on this platform.";
            return;
        }

        var file = await PickImportTextRequested();
        if (file is null) return;
        try
        {
            var format = Path.GetExtension(file.Name).Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? ImportFormat.Csv
                : ImportFormat.VCard;
            var result = await _service.ImportAsync(file.Content, format);
            StatusMessage = result.Warnings.Count == 0
                ? $"Imported {result.ImportedCount} contact{(result.ImportedCount == 1 ? "" : "s")}."
                : $"Imported {result.ImportedCount} contact{(result.ImportedCount == 1 ? "" : "s")}. {string.Join(" ", result.Warnings)}";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (SaveTextRequested is null)
        {
            StatusMessage = "Export is unavailable on this platform.";
            return;
        }

        try
        {
            var text = await _service.ExportAsync(ExportFormat.Csv);
            if (await SaveTextRequested("contactcore-contacts.csv", "text/csv", text))
                StatusMessage = "CSV exported. Treat formula-like text carefully before opening it in spreadsheet software.";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task ExportVCardAsync()
    {
        if (SaveTextRequested is null)
        {
            StatusMessage = "Export is unavailable on this platform.";
            return;
        }

        try
        {
            var text = await _service.ExportAsync(ExportFormat.VCard);
            if (await SaveTextRequested("contactcore-contacts.vcf", "text/vcard", text))
                StatusMessage = "vCard exported.";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        if (!CanUseDatabaseBackups)
        {
            StatusMessage = "Native database backups are unavailable on this platform. Use CSV or vCard export for a portable copy.";
            return;
        }

        try
        {
            StatusMessage = $"Backup created: {await _backup.CreateAsync()}";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (!CanUseDatabaseBackups)
        {
            StatusMessage = "Native database restore is unavailable on this platform.";
            return;
        }
        if (PickBackupFileRequested is null)
        {
            StatusMessage = "Backup picker is unavailable on this platform.";
            return;
        }

        var selected = await PickBackupFileRequested();
        if (selected is null) return;
        QueueConfirmation(
            "Restore this verified database backup? Current local data will be replaced only after verification, with a recovery snapshot retained by the restore workflow.",
            () => RestoreConfirmedAsync(selected));
    }

    private async Task RestoreConfirmedAsync(PickedBackupFile selected)
    {
        try
        {
            await _backup.RestoreAsync(selected.Path);
            StatusMessage = "Backup restored and verified.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
        finally
        {
            if (selected.DeleteAfterUse)
                TryDeleteTemporary(selected.Path);
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        HideDetailViews();
        IsSettingsVisible = true;
        SelectedTheme = NormalizeTheme(_preferences.Theme);
        ReducedMotion = _preferences.ReducedMotion;
        ConfirmPermanentDelete = _preferences.ConfirmPermanentDelete;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        _preferences.Theme = NormalizeTheme(SelectedTheme);
        _preferences.ReducedMotion = ReducedMotion;
        _preferences.ConfirmPermanentDelete = ConfirmPermanentDelete;
        try
        {
            await _preferences.SaveAsync();
            ThemeChangeRequested?.Invoke(_preferences.Theme);
            StatusMessage = "Settings saved locally.";
        }
        catch (Exception ex)
        {
            StatusMessage = SafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task ConfirmPendingAsync()
    {
        var action = _pendingConfirmedAction;
        ClearConfirmation();
        if (action is not null)
            await action();
    }

    [RelayCommand]
    private void CancelPending() => ClearConfirmation();

    private void QueueConfirmation(string message, Func<Task> action)
    {
        _pendingConfirmedAction = action;
        ConfirmationMessage = message;
        IsConfirmationVisible = true;
    }

    private void ClearConfirmation()
    {
        IsConfirmationVisible = false;
        ConfirmationMessage = "";
        _pendingConfirmedAction = null;
    }

    private void HideDetailViews()
    {
        IsEditorVisible = false;
        IsSettingsVisible = false;
        IsDataToolsVisible = false;
        IsDuplicatesVisible = false;
        ClearConfirmation();
    }

    private async Task DebouncedRefreshAsync()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _searchCts, current);
        previous?.Cancel();
        previous?.Dispose();
        try
        {
            await Task.Delay(180, current.Token);
            await RefreshAsync(current.Token);
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref _searchCts, null, current), current))
                current.Dispose();
        }
    }

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        var query = new ContactQuery(
            Search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
            Letter: _letter,
            FavoritesOnly: FavoritesOnly,
            ArchivedOnly: ArchivedOnly,
            ShowAll: ShowAll);
        var rows = await _service.ListAsync(query, ct);
        ct.ThrowIfCancellationRequested();
        var items = rows.Select(c => new ContactListItemViewModel(c)).ToArray();
        ct.ThrowIfCancellationRequested();
        Contacts.Clear();
        foreach (var item in items) Contacts.Add(item);
        ResultCountText = $"{Contacts.Count} contact{(Contacts.Count == 1 ? "" : "s")}";
    }

    private static string NormalizeTheme(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "light" => "Light",
        "dark" => "Dark",
        _ => "System"
    };

    private static string SafeMessage(Exception ex)
    {
        var text = ex.Message;
        if (text.Length > 500) text = text[..500] + "…";
        return text;
    }

    private static void TryDeleteTemporary(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }
}