using CommunityToolkit.Mvvm.Input;
using ContactCore.Infrastructure;

namespace ContactCore.Desktop;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private async Task MergeSelectedDuplicateIntoSecondaryAsync()
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
            $"Merge {pair.PrimaryName} into {pair.SecondaryName}? The second contact is kept, unique details are combined, and the first contact is permanently removed from the active database.");
        if (!confirmed) return;

        try
        {
            FooterText = "Merging duplicate contacts…";
            var merged = await _service.MergeAsync(pair.Candidate.Right.Id, pair.Candidate.Left.Id);
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
}
