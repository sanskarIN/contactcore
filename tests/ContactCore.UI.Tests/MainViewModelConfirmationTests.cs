using ContactCore.Domain;
using ContactCore.UI;

namespace ContactCore.UI.Tests;

[TestClass]
public sealed class MainViewModelConfirmationTests
{
    [TestMethod]
    public async Task Permanent_delete_waits_for_confirmation_when_preference_is_enabled()
    {
        var contact = new Contact { GivenName = "Delete", FamilyName = "Candidate" };
        var repository = new RecordingContactRepository();
        repository.Seed(contact);
        var preferences = new TestPreferences { ConfirmPermanentDelete = true };
        var viewModel = new MainViewModel(UiTestServices.Create(repository, preferences: preferences));
        viewModel.Draft.Load(contact, isPersisted: true);

        await viewModel.RequestDeleteCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.IsTrue(viewModel.IsConfirmationVisible);
        Assert.AreEqual(0, repository.DeleteCalls);
        StringAssert.Contains(viewModel.ConfirmationMessage, "Permanently delete");

        await viewModel.ConfirmPendingCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.IsFalse(viewModel.IsConfirmationVisible);
        Assert.AreEqual(1, repository.DeleteCalls);
    }

    [TestMethod]
    public async Task Cancelling_pending_delete_preserves_contact()
    {
        var contact = new Contact { GivenName = "Keep", FamilyName = "Candidate" };
        var repository = new RecordingContactRepository();
        repository.Seed(contact);
        var viewModel = new MainViewModel(UiTestServices.Create(repository));
        viewModel.Draft.Load(contact, isPersisted: true);

        await viewModel.RequestDeleteCommand.ExecuteAsync(null).ConfigureAwait(false);
        viewModel.CancelPendingCommand.Execute(null);

        Assert.IsFalse(viewModel.IsConfirmationVisible);
        Assert.AreEqual(0, repository.DeleteCalls);
        Assert.IsNotNull(await repository.GetAsync(contact.Id).ConfigureAwait(false));
    }

    [TestMethod]
    public async Task Permanent_delete_can_proceed_directly_when_confirmation_preference_is_disabled()
    {
        var contact = new Contact { GivenName = "Direct", FamilyName = "Delete" };
        var repository = new RecordingContactRepository();
        repository.Seed(contact);
        var preferences = new TestPreferences { ConfirmPermanentDelete = false };
        var viewModel = new MainViewModel(UiTestServices.Create(repository, preferences: preferences));
        viewModel.Draft.Load(contact, isPersisted: true);

        await viewModel.RequestDeleteCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.IsFalse(viewModel.IsConfirmationVisible);
        Assert.AreEqual(1, repository.DeleteCalls);
    }

    [TestMethod]
    public async Task Restore_waits_for_confirmation_before_backup_service_is_called()
    {
        const string backupPath = "/virtual/contactcore-backup.db";
        var repository = new RecordingContactRepository();
        var backup = new RecordingBackupService();
        var viewModel = new MainViewModel(UiTestServices.Create(repository, backup: backup, supportsBackups: true))
        {
            PickBackupFileRequested = () => Task.FromResult<PickedBackupFile?>(new PickedBackupFile(backupPath))
        };

        await viewModel.RestoreBackupCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.IsTrue(viewModel.IsConfirmationVisible);
        Assert.AreEqual(0, backup.RestoreCalls);
        StringAssert.Contains(viewModel.ConfirmationMessage, "Restore this ContactCore backup");

        await viewModel.ConfirmPendingCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.IsFalse(viewModel.IsConfirmationVisible);
        Assert.AreEqual(1, backup.RestoreCalls);
        Assert.AreEqual(backupPath, backup.LastRestorePath);
        Assert.AreEqual("Backup restored successfully.", viewModel.StatusMessage);
    }
}
