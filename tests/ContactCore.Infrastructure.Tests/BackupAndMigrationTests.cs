using ContactCore.Domain;
using ContactCore.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ContactCore.Infrastructure.Tests;

[TestClass]
public sealed class BackupAndMigrationTests
{
    private string _dir = null!;

    [TestInitialize]
    public void Initialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "contactcore-backup-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
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
    public async Task Backup_restore_round_trip_recovers_committed_contact()
    {
        var paths = new AppPaths(_dir);
        var factory = new SqliteConnectionFactory(paths.DatabasePath);
        var repository = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
        await repository.InitializeAsync();
        var contact = new Contact { GivenName = "Backup", FamilyName = "Test" };
        contact.Emails.Add(new(Guid.NewGuid(), "Work", "backup@example.test"));
        await repository.UpsertAsync(contact);

        var service = new BackupService(paths, factory);
        var externalBackups = Path.Combine(_dir, "external");
        var backup = await service.CreateBackupAsync(externalBackups);
        await repository.DeleteAsync(contact.Id);
        Assert.IsNull(await repository.GetAsync(contact.Id));

        await service.RestoreBackupAsync(backup);
        await repository.InitializeAsync();
        var restored = await repository.GetAsync(contact.Id);

        Assert.IsNotNull(restored);
        Assert.AreEqual("backup@example.test", restored.Emails.Single().Address);
    }

    [TestMethod]
    public async Task Encryption_request_fails_closed_with_plain_sqlite_provider()
    {
        var path = Path.Combine(_dir, "encrypted.db");
        var factory = new SqliteConnectionFactory(path, () => "test-secret-not-for-production");

        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await using var _ = await factory.OpenAsync();
        });

        StringAssert.Contains(ex.Message, "SQLCipher-compatible");
    }

    [TestMethod]
    public async Task Migrator_refuses_database_from_newer_schema()
    {
        var path = Path.Combine(_dir, "future.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = path };
        await using (var connection = new SqliteConnection(builder.ToString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL); INSERT INTO schema_migrations VALUES(999, 'future');";
            await command.ExecuteNonQueryAsync();
        }

        var factory = new SqliteConnectionFactory(path);
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => new DatabaseMigrator(factory).ApplyAsync());

        StringAssert.Contains(ex.Message, "schema version 999");
    }

    [TestMethod]
    public async Task Restore_rejects_non_contactcore_sqlite_database()
    {
        var paths = new AppPaths(Path.Combine(_dir, "app"));
        var factory = new SqliteConnectionFactory(paths.DatabasePath);
        var repository = new SqliteContactRepository(factory, new DatabaseMigrator(factory));
        await repository.InitializeAsync();

        var unrelated = Path.Combine(_dir, "unrelated.db");
        await using (var connection = new SqliteConnection($"Data Source={unrelated}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE something_else(id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsExceptionAsync<InvalidDataException>(
            () => new BackupService(paths, factory).RestoreBackupAsync(unrelated));
    }
}
