using ContactCore.Application;
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
        SqliteConnection.ClearAllPools();
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
        catch (Exception ex) when (ex is InvalidDataException or SqliteException)
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Invalid backup should be rejected.");
        Assert.AreEqual(1, await _repository.CountAsync());
        Assert.AreEqual("Keep me", (await _repository.SearchAsync(new ContactQuery())).Single().GivenName);
    }

    [TestMethod]
    public async Task Legacy_schema_backup_is_migrated_before_replacement()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Legacy" });
        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await DowngradeBackupToVersionOneAsync(backupPath);
        await _repository.UpsertAsync(new Contact { GivenName = "Current" });

        await _backup.RestoreBackupAsync(backupPath);

        var contacts = await _repository.SearchAsync(new ContactQuery());
        Assert.AreEqual(1, contacts.Count);
        Assert.AreEqual("Legacy", contacts.Single().GivenName);
        await using var active = await _factory.OpenAsync();
        Assert.AreEqual(DatabaseMigrator.LatestSchemaVersion, await DatabaseMigrator.CurrentVersionAsync(active, CancellationToken.None));
    }

    [TestMethod]
    public async Task Future_schema_backup_is_rejected_without_replacing_active_database()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep active" });
        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await MarkBackupAsFutureVersionAsync(backupPath);

        await Assert.ThrowsAsync<NotSupportedException>(() => _backup.RestoreBackupAsync(backupPath));

        var contacts = await _repository.SearchAsync(new ContactQuery());
        Assert.AreEqual(1, contacts.Count);
        Assert.AreEqual("Keep active", contacts.Single().GivenName);
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

    private static async Task DowngradeBackupToVersionOneAsync(string path)
    {
        SqliteConnection.ClearAllPools();
        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM schema_migrations WHERE version >= 2; DROP TABLE IF EXISTS app_metadata;";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task MarkBackupAsFutureVersionAsync(string path)
    {
        SqliteConnection.ClearAllPools();
        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO schema_migrations(version, applied_at) VALUES ($version, $at);";
        command.Parameters.AddWithValue("$version", DatabaseMigrator.LatestSchemaVersion + 100);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }
}
