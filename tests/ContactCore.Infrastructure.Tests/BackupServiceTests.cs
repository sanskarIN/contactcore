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
    private DatabaseMigrator _migrator = null!;
    private BackupService _backup = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-backup-tests", Guid.NewGuid().ToString("N"));
        _paths = new AppPaths(_dir);
        _factory = new SqliteConnectionFactory(_paths.DatabasePath);
        _migrator = new DatabaseMigrator(_factory);
        _backup = new BackupService(_paths, _factory, _migrator);
        await _migrator.ApplyAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [TestMethod]
    public async Task Restore_failure_preserves_existing_database()
    {
        await using (var connection = await _factory.OpenAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE restore_marker(value TEXT NOT NULL); INSERT INTO restore_marker(value) VALUES ('original');";
            await command.ExecuteNonQueryAsync();
        }

        var invalidBackup = Path.Combine(_dir, "invalid-schema.db");
        var invalidBuilder = new SqliteConnectionStringBuilder { DataSource = invalidBackup, Mode = SqliteOpenMode.ReadWriteCreate };
        await using (var connection = new SqliteConnection(invalidBuilder.ToString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL); CREATE TABLE contacts(id TEXT PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsExceptionAsync<SqliteException>(() => _backup.RestoreBackupAsync(invalidBackup));

        await using var restored = await _factory.OpenAsync();
        await using var verify = restored.CreateCommand();
        verify.CommandText = "SELECT value FROM restore_marker LIMIT 1;";
        Assert.AreEqual("original", await verify.ExecuteScalarAsync());
        Assert.IsFalse(File.Exists(_paths.DatabasePath + ".restore"));
        Assert.IsFalse(File.Exists(_paths.DatabasePath + ".pre-restore"));
    }

    [TestMethod]
    public async Task Restore_valid_backup_replaces_live_database()
    {
        await using (var connection = await _factory.OpenAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE restore_marker(value TEXT NOT NULL); INSERT INTO restore_marker(value) VALUES ('from-backup');";
            await command.ExecuteNonQueryAsync();
        }

        var backupDirectory = Path.Combine(_dir, "backups");
        var backupFile = await _backup.CreateBackupAsync(backupDirectory);

        await using (var connection = await _factory.OpenAsync())
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE restore_marker SET value = 'changed-live';";
            await command.ExecuteNonQueryAsync();
        }

        await _backup.RestoreBackupAsync(backupFile);

        await using var restored = await _factory.OpenAsync();
        await using var verify = restored.CreateCommand();
        verify.CommandText = "SELECT value FROM restore_marker LIMIT 1;";
        Assert.AreEqual("from-backup", await verify.ExecuteScalarAsync());
    }
}
