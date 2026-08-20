using ContactCore.Domain;
using ContactCore.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class BackupServiceTests
{
    private string _dir = null!;
    private AppPaths _paths = null!;
    private SqliteConnectionFactory _factory = null!;
    private SqliteContactRepository _repo = null!;
    private BackupService _backup = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-backup-tests", Guid.NewGuid().ToString("N"));
        _paths = new AppPaths(_dir);
        _factory = new SqliteConnectionFactory(_paths.DatabasePath);
        _repo = new SqliteContactRepository(_factory, new DatabaseMigrator(_factory));
        _backup = new BackupService(_paths, _factory);
        await _repo.InitializeAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [TestMethod]
    public async Task Restore_round_trips_existing_contact()
    {
        var contact = new Contact { GivenName = "Backup", FamilyName = "Candidate" };
        await _repo.UpsertAsync(contact);

        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await _repo.DeleteAsync(contact.Id);
        Assert.IsNull(await _repo.GetAsync(contact.Id));

        await _backup.RestoreBackupAsync(backupPath);

        var restored = await _repo.GetAsync(contact.Id);
        Assert.IsNotNull(restored);
        Assert.AreEqual("Backup", restored.GivenName);
        Assert.AreEqual("Candidate", restored.FamilyName);
    }

    [TestMethod]
    public async Task Invalid_backup_does_not_replace_live_database()
    {
        var contact = new Contact { GivenName = "Keep", FamilyName = "Me" };
        await _repo.UpsertAsync(contact);
        var invalidBackup = Path.Combine(_dir, "not-a-database.db");
        await File.WriteAllTextAsync(invalidBackup, "not sqlite");

        await Assert.ThrowsExceptionAsync<SqliteException>(() => _backup.RestoreBackupAsync(invalidBackup));

        Assert.IsNotNull(await _repo.GetAsync(contact.Id));
    }

    [TestMethod]
    public async Task Failed_post_restore_migration_rolls_back_live_database()
    {
        var contact = new Contact { GivenName = "Original", FamilyName = "Data" };
        await _repo.UpsertAsync(contact);

        var malformedBackup = Path.Combine(_dir, "malformed-schema.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = malformedBackup, Mode = SqliteOpenMode.ReadWriteCreate };
        await using (var connection = new SqliteConnection(builder.ToString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE schema_migrations(foo TEXT);";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsExceptionAsync<SqliteException>(() => _backup.RestoreBackupAsync(malformedBackup));

        var preserved = await _repo.GetAsync(contact.Id);
        Assert.IsNotNull(preserved);
        Assert.AreEqual("Original", preserved.GivenName);
        Assert.IsFalse(File.Exists(_paths.DatabasePath + ".restore"));
        Assert.IsFalse(File.Exists(_paths.DatabasePath + ".pre-restore"));
    }
}
