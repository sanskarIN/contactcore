using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
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
        Id = contact.Id; CreatedAt = contact.CreatedAt; GivenName = contact.GivenName; FamilyName = contact.FamilyName; Nickname = contact.Nickname;
        BirthdayText = contact.Birthday?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""; Phone = contact.Phones.FirstOrDefault()?.Number ?? ""; Email = contact.Emails.FirstOrDefault()?.Address ?? "";
        Notes = contact.Notes; IsFavorite = contact.IsFavorite;
    }

    public Contact ToContact()
    {
        DateOnly? birthday = null;
        if (!string.IsNullOrWhiteSpace(BirthdayText))
        {
            if (!DateOnly.TryParseExact(BirthdayText.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) throw new FormatException("Birthday must use yyyy-MM-dd.");
            birthday = parsed;
        }
        var contact = new Contact { Id = Id == Guid.Empty ? Guid.NewGuid() : Id, CreatedAt = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt, GivenName = GivenName, FamilyName = FamilyName, Nickname = Nickname, Birthday = birthday, Notes = Notes, IsFavorite = IsFavorite, UpdatedAt = DateTimeOffset.UtcNow };
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
    private int _refreshVersion;

    public MainWindowViewModel(ContactService service, IBackupService backup, IAppPreferences preferences, AppPaths paths)
    {
        _service = service; _backup = backup; _preferences = preferences; _paths = paths;
        Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(x => x.ToString()).ToArray();
        Draft = new ContactDraftViewModel();
    }

    public Action? FocusSearchRequested { get; set; }
    public ObservableCollection<ContactListItemViewModel> Contacts { get; } = [];
    public IReadOnlyList<string> Alphabet { get; }
    public ContactDraftViewModel Draft { get; }

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private ContactListItemViewModel? selectedContact;
    [ObservableProperty] private bool favoritesOnly;
    [ObservableProperty] private bool archivedOnly;
    [ObservableProperty] private bool showAll = true;
    [ObservableProperty] private bool isEditorVisible;
    [ObservableProperty] private string editorTitle = "Contact details";
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private string listHeading = "All contacts";
    [ObservableProperty] private string resultCountText = "0 contacts";
    [ObservableProperty] private string footerText = "Ready";
    private char? _letter;

    partial void OnSearchTextChanged(string value) => _ = DebouncedRefreshAsync();
    partial void OnSelectedContactChanged(ContactListItemViewModel? value)
    {
        if (value is null) return;
        Draft.Load(value.Model.DeepCopy()); IsEditorVisible = true; EditorTitle = value.DisplayName; StatusMessage = "";
    }

    public async Task InitializeAsync()
    {
        try { FooterText = "Opening local database…"; await _service.InitializeAsync(); await RefreshAsync(); FooterText = "Ready"; }
        catch (Exception ex) { FooterText = "Could not initialize ContactCore"; StatusMessage = RedactingLog.Sanitize(ex.Message); }
    }

    [RelayCommand] private void NewContact()
    {
        SelectedContact = null; Draft.Load(new Contact()); IsEditorVisible = true; EditorTitle = "New contact"; StatusMessage = "";
    }

    [RelayCommand] private async Task SaveAsync()
    {
        try { await _service.SaveAsync(Draft.ToContact()); StatusMessage = "Saved locally."; await RefreshAsync(); }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
    }

    [RelayCommand] private async Task DeleteAsync()
    {
        if (Draft.Id == Guid.Empty) { CancelEdit(); return; }
        try { await _service.DeleteAsync(Draft.Id); IsEditorVisible = false; SelectedContact = null; StatusMessage = "Contact permanently deleted."; await RefreshAsync(); }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
    }

    [RelayCommand] private void CancelEdit() { IsEditorVisible = false; SelectedContact = null; StatusMessage = ""; }
    [RelayCommand] private async Task ShowAllAsync() { ShowAll = true; FavoritesOnly = false; ArchivedOnly = false; _letter = null; ListHeading = "All contacts"; await RefreshAsync(); }
    [RelayCommand] private async Task FavoritesAsync() { ShowAll = false; FavoritesOnly = true; ArchivedOnly = false; _letter = null; ListHeading = "Favorites"; await RefreshAsync(); }
    [RelayCommand] private async Task ArchivedAsync() { ShowAll = false; FavoritesOnly = false; ArchivedOnly = true; _letter = null; ListHeading = "Archived"; await RefreshAsync(); }
    [RelayCommand] private async Task FilterLetterAsync(string letter) { _letter = string.IsNullOrEmpty(letter) ? null : letter[0]; ListHeading = $"Contacts — {letter}"; await RefreshAsync(); }

    [RelayCommand] private async Task FindDuplicatesAsync()
    {
        var all = await _service.SearchAsync(new ContactQuery(IncludeArchived: true));
        var duplicates = new DuplicateDetector().Find(all);
        StatusMessage = duplicates.Count == 0 ? "No likely duplicates found." : $"Found {duplicates.Count} likely duplicate pair(s). Highest score: {duplicates[0].Score:P0}.";
    }

    [RelayCommand] private async Task ShowDataToolsAsync()
    {
        Directory.CreateDirectory(_paths.BackupDirectory);
        try { var path = await _backup.CreateBackupAsync(_paths.BackupDirectory); StatusMessage = $"Backup created: {Path.GetFileName(path)}"; }
        catch (Exception ex) { StatusMessage = RedactingLog.Sanitize(ex.Message); }
    }

    [RelayCommand] private void ShowSettings()
    {
        StatusMessage = $"Theme: {_preferences.Theme}. Data folder: {_paths.DataDirectory}. Privacy: all contact data stays local unless you export it. About: ContactCore • MIT • Made by the Sanskar • github.com/sanskarIN • buymeacoffee.com/sanskarIN";
    }

    private async Task DebouncedRefreshAsync()
    {
        _searchCts?.Cancel(); _searchCts?.Dispose(); _searchCts = new CancellationTokenSource();
        try { await Task.Delay(180, _searchCts.Token); await RefreshAsync(_searchCts.Token); }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var refreshVersion = Interlocked.Increment(ref _refreshVersion);
        var query = new ContactQuery(SearchText, FavoritesOnly, IncludeArchived: ArchivedOnly, StartsWith: _letter);
        var contacts = await _service.SearchAsync(query, cancellationToken);
        if (ArchivedOnly) contacts = contacts.Where(x => x.IsArchived).ToArray();
        if (refreshVersion != Volatile.Read(ref _refreshVersion)) return;

        Contacts.Clear(); foreach (var contact in contacts) Contacts.Add(new(contact));
        ResultCountText = $"{Contacts.Count} {(Contacts.Count == 1 ? "contact" : "contacts")}";
    }
}
