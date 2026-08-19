using ContactCore.Application;
using ContactCore.Domain;
using ContactCore.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class BackupServiceTests
{
    private string _dir = null!;
    private AppPaths _paths = null!;
    private SqliteConnectionFactory _factory = null!;
    private SqliteContactRepository _repository = null!;
    private BackupService _backup = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-backup-tests", Guid.NewGuid().ToString("N"));
        _paths = new AppPaths(_dir);
        _factory = new SqliteConnectionFactory(_paths.DatabasePath);
        _repository = new SqliteContactRepository(_factory, new DatabaseMigrator(_factory));
        _backup = new BackupService(_paths, _factory);
        await _repository.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [TestMethod]
    public async Task Restore_reverts_database_to_verified_snapshot()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "First" });
        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await _repository.UpsertAsync(new Contact { GivenName = "Second" });
        Assert.AreEqual(2, await _repository.CountAsync());

        await _backup.RestoreBackupAsync(backupPath);

        Assert.AreEqual(1, await _repository.CountAsync());
        Assert.AreEqual("First", (await _repository.SearchAsync(new ContactQuery())).Single().GivenName);
    }

    [TestMethod]
    public async Task Invalid_backup_never_replaces_active_database()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep me" });
        var invalid = Path.Combine(_dir, "not-a-database.db");
        await File.WriteAllTextAsync(invalid, "this is not sqlite");

        var threw = false;
        try
        {
            await _backup.RestoreBackupAsync(invalid);
        }
        catch (Exception ex) when (ex is InvalidDataException or Microsoft.Data.Sqlite.SqliteException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Invalid backup should be rejected.");
        Assert.AreEqual(1, await _repository.CountAsync());
        Assert.AreEqual("Keep me", (await _repository.SearchAsync(new ContactQuery())).Single().GivenName);
    }

    [TestMethod]
    public async Task Consecutive_backups_use_unique_file_names()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Backup" });

        var first = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        var second = await _backup.CreateBackupAsync(_paths.BackupDirectory);

        Assert.AreNotEqual(first, second);
        Assert.IsTrue(File.Exists(first));
        Assert.IsTrue(File.Exists(second));
    }
}
