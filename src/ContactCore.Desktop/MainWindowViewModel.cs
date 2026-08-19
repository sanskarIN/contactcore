using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactCore.Application;
using ContactCore.Domain;

namespace ContactCore.Desktop;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ContactService _contacts;
    private readonly DuplicateService _duplicates;
    private readonly IBackupService _backups;
    private Contact? _editingContact;

    public MainWindowViewModel(ContactService contacts, DuplicateService duplicates, IBackupService backups)
    {
        _contacts = contacts ?? throw new ArgumentNullException(nameof(contacts));
        _duplicates = duplicates ?? throw new ArgumentNullException(nameof(duplicates));
        _backups = backups ?? throw new ArgumentNullException(nameof(backups));
        _ = RefreshAsync();
    }

    public ObservableCollection<Contact> Items { get; } = [];

    [ObservableProperty] private Contact? selectedContact;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool favoritesOnly;
    [ObservableProperty] private bool includeArchived;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = "Ready";
    [ObservableProperty] private string givenName = string.Empty;
    [ObservableProperty] private string familyName = string.Empty;
    [ObservableProperty] private string nickname = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string validationMessage = string.Empty;

    public string ProductCredit => "Made by the Sanskar";
    public string AboutText => "ContactCore · Local-first contact management · MIT License";

    partial void OnSelectedContactChanged(Contact? value) => LoadEditor(value);
    partial void OnSearchTextChanged(string value) => _ = RefreshAsync();
    partial void OnFavoritesOnlyChanged(bool value) => _ = RefreshAsync();
    partial void OnIncludeArchivedChanged(bool value) => _ = RefreshAsync();

    [RelayCommand]
    private void NewContact()
    {
        SelectedContact = null;
        _editingContact = new Contact();
        ClearEditor();
        StatusMessage = "Creating a new contact";
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var selectedId = SelectedContact?.Id;
            var results = await _contacts.SearchAsync(new ContactQuery(SearchText, FavoritesOnly, IncludeArchived));
            Items.Clear();
            foreach (var contact in results) Items.Add(contact);
            SelectedContact = selectedId is null ? Items.FirstOrDefault() : Items.FirstOrDefault(item => item.Id == selectedId) ?? Items.FirstOrDefault();
            StatusMessage = $"{Items.Count} contact{(Items.Count == 1 ? string.Empty : "s")}";
        }
        catch (Exception ex)
        {
            StatusMessage = UserSafeMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            ValidationMessage = string.Empty;
            IsBusy = true;
            var contact = (_editingContact ?? SelectedContact ?? new Contact()).DeepCopy();
            contact.GivenName = GivenName;
            contact.FamilyName = FamilyName;
            contact.Nickname = Nickname;
            contact.Notes = Notes;
            ReplacePrimaryEmail(contact, Email);
            ReplacePrimaryPhone(contact, Phone);

            var duplicateMatches = await _duplicates.FindPotentialDuplicatesAsync(contact, 0.70);
            if (duplicateMatches.Count > 0 && SelectedContact is null)
                StatusMessage = $"Saved with {duplicateMatches.Count} possible duplicate match{(duplicateMatches.Count == 1 ? string.Empty : "es")}.";

            var saved = await _contacts.SaveAsync(contact);
            _editingContact = saved;
            await RefreshAsync();
            SelectedContact = Items.FirstOrDefault(item => item.Id == saved.Id);
            if (duplicateMatches.Count == 0) StatusMessage = "Contact saved";
        }
        catch (ContactValidationException ex)
        {
            ValidationMessage = string.Join(Environment.NewLine, ex.Issues.Select(issue => $"{issue.Field}: {issue.Message}"));
            StatusMessage = "Please fix the highlighted contact data";
        }
        catch (Exception ex)
        {
            StatusMessage = UserSafeMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (SelectedContact is null) return;
        try
        {
            IsBusy = true;
            var id = SelectedContact.Id;
            await _contacts.DeleteAsync(id);
            _editingContact = null;
            await RefreshAsync();
            StatusMessage = "Contact deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = UserSafeMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (SelectedContact is null) return;
        await _contacts.SetFavoriteAsync(SelectedContact.Id, !SelectedContact.IsFavorite);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (SelectedContact is null) return;
        await _contacts.SetArchivedAsync(SelectedContact.Id, !SelectedContact.IsArchived);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"contactcore-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
            var contacts = await _contacts.SearchAsync(new ContactQuery(IncludeArchived: true, Limit: 1000));
            await File.WriteAllTextAsync(path, ContactCsvCodec.Export(contacts));
            StatusMessage = $"CSV exported to {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = UserSafeMessage(ex);
        }
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ContactCore Backups");
            var result = await _backups.CreateBackupAsync(Path.Combine(folder, $"contactcore-{DateTime.UtcNow:yyyyMMdd-HHmmss}.db"));
            StatusMessage = $"Backup created: {result.Path}";
        }
        catch (Exception ex)
        {
            StatusMessage = UserSafeMessage(ex);
        }
    }

    private void LoadEditor(Contact? contact)
    {
        _editingContact = contact?.DeepCopy();
        if (contact is null) return;
        GivenName = contact.GivenName;
        FamilyName = contact.FamilyName;
        Nickname = contact.Nickname;
        Email = contact.Emails.FirstOrDefault()?.Address ?? string.Empty;
        Phone = contact.Phones.FirstOrDefault()?.Number ?? string.Empty;
        Notes = contact.Notes;
        ValidationMessage = string.Empty;
    }

    private void ClearEditor()
    {
        GivenName = string.Empty;
        FamilyName = string.Empty;
        Nickname = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Notes = string.Empty;
        ValidationMessage = string.Empty;
    }

    private static void ReplacePrimaryEmail(Contact contact, string value)
    {
        var trimmed = value.Trim();
        if (contact.Emails.Count > 0) contact.Emails.RemoveAt(0);
        if (trimmed.Length > 0) contact.Emails.Insert(0, new ContactEmail(Guid.NewGuid(), "Primary", trimmed));
    }

    private static void ReplacePrimaryPhone(Contact contact, string value)
    {
        var trimmed = value.Trim();
        if (contact.Phones.Count > 0) contact.Phones.RemoveAt(0);
        if (trimmed.Length > 0) contact.Phones.Insert(0, new ContactPhone(Guid.NewGuid(), "Primary", trimmed));
    }

    private static string UserSafeMessage(Exception exception) => exception switch
    {
        UnauthorizedAccessException => "ContactCore does not have permission to access that file or folder.",
        IOException => "A local file operation failed. Check available storage and file permissions.",
        _ => "The operation could not be completed. No contact data was sent anywhere."
    };
}
