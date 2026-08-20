using ContactCore.Application;
using ContactCore.Domain;
using Microsoft.Data.Sqlite;

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
    public async Task Restore_retains_verified_pre_restore_snapshot_of_previous_active_state()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Backup state" });
        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await _repository.UpsertAsync(new Contact { GivenName = "Current before restore" });

        await _backup.RestoreBackupAsync(backupPath);

        var recoveryFiles = Directory.GetFiles(_paths.BackupDirectory, "pre-restore-*.db");
        Assert.AreEqual(1, recoveryFiles.Length, "One pre-restore snapshot should be retained for this restore attempt.");
        var recoveryFactory = new SqliteConnectionFactory(recoveryFiles[0]);
        var recoveryRepository = new SqliteContactRepository(recoveryFactory, new DatabaseMigrator(recoveryFactory));
        await recoveryRepository.InitializeAsync();
        var recoveryNames = (await recoveryRepository.SearchAsync(new ContactQuery())).Select(x => x.GivenName).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Backup state", "Current before restore" }, recoveryNames);
    }

    [TestMethod]
    public async Task Missing_backup_is_rejected_before_active_database_changes()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep active" });
        var missing = Path.Combine(_dir, "missing.db");

        await Assert.ThrowsAsync<FileNotFoundException>(() => _backup.RestoreBackupAsync(missing));

        Assert.AreEqual("Keep active", (await _repository.SearchAsync(new ContactQuery())).Single().GivenName);
    }

    [TestMethod]
    public async Task Active_database_cannot_be_selected_as_its_own_restore_source()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep active" });

        await Assert.ThrowsAsync<ArgumentException>(() => _backup.RestoreBackupAsync(_paths.DatabasePath));

        Assert.AreEqual("Keep active", (await _repository.SearchAsync(new ContactQuery())).Single().GivenName);
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
    public async Task Valid_unrelated_sqlite_database_is_rejected_without_replacing_active_database()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep active" });
        var unrelated = Path.Combine(_dir, "unrelated.db");
        await using (var connection = new SqliteConnection($"Data Source={unrelated};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE unrelated(id INTEGER PRIMARY KEY, value TEXT NOT NULL); INSERT INTO unrelated(value) VALUES ('fictional');";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => _backup.RestoreBackupAsync(unrelated));

        var contacts = await _repository.SearchAsync(new ContactQuery());
        Assert.AreEqual(1, contacts.Count);
        Assert.AreEqual("Keep active", contacts.Single().GivenName);
    }

    [TestMethod]
    public async Task Tampered_schema_identity_is_rejected_without_replacing_active_database()
    {
        await _repository.UpsertAsync(new Contact { GivenName = "Keep active" });
        var backupPath = await _backup.CreateBackupAsync(_paths.BackupDirectory);
        await TamperSchemaIdentityAsync(backupPath);

        await Assert.ThrowsAsync<InvalidDataException>(() => _backup.RestoreBackupAsync(backupPath));

        var contacts = await _repository.SearchAsync(new ContactQuery());
        Assert.AreEqual(1, contacts.Count);
        Assert.AreEqual("Keep active", contacts.Single().GivenName);
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
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task TamperSchemaIdentityAsync(string path)
    {
        SqliteConnection.ClearAllPools();
        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE app_metadata SET value='not-contactcore' WHERE key='schema_family';";
        await command.ExecuteNonQueryAsync();
    }
}
