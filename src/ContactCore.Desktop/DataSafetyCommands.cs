using CommunityToolkit.Mvvm.Input;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed record PickedBackupFile(string Path, bool DeleteAfterUse = false);

public sealed partial class MainWindowViewModel
{
    public Func<Task<PickedBackupFile?>>? PickBackupFileRequested { get; set; }
    public Func<string, Task<bool>>? ConfirmActionRequested { get; set; }

    [RelayCommand]
    private async Task RequestDeleteAsync()
    {
        if (Draft.Id == Guid.Empty)
        {
            CancelEdit();
            return;
        }

        if (_preferences.ConfirmPermanentDelete)
        {
            if (ConfirmActionRequested is null)
            {
                StatusMessage = "Permanent deletion is blocked because confirmation is unavailable.";
                return;
            }

            var confirmed = await ConfirmActionRequested(
                "Permanently delete this contact? This removes it from the active database. Existing backups and exports are separate copies.");
            if (!confirmed) return;
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
    private async Task RestoreBackupAsync()
    {
        if (PickBackupFileRequested is null)
        {
            StatusMessage = "Backup picker is unavailable on this platform.";
            return;
        }

        PickedBackupFile? picked = null;
        try
        {
            picked = await PickBackupFileRequested();
            if (picked is null) return;

            if (ConfirmActionRequested is null)
            {
                StatusMessage = "Restore is blocked because confirmation is unavailable.";
                return;
            }

            var confirmed = await ConfirmActionRequested(
                "Restore this ContactCore backup? A verified snapshot of the current database will be retained in the backup directory before replacement.");
            if (!confirmed) return;

            FooterText = "Restoring verified backup…";
            await _backup.RestoreBackupAsync(picked.Path);
            await _service.InitializeAsync();
            SelectedContact = null;
            IsEditorVisible = false;
            await RefreshAsync();
            StatusMessage = "Backup restored successfully. A pre-restore recovery snapshot was retained.";
        }
        catch (Exception ex)
        {
            StatusMessage = RedactingLog.Sanitize(ex.Message);
        }
        finally
        {
            FooterText = "Ready";
            if (picked?.DeleteAfterUse == true)
            {
                try { File.Delete(picked.Path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
