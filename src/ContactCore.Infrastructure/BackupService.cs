using ContactCore.Application;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class BackupService : IBackupService
{
    private readonly AppPaths _paths;
    private readonly SqliteConnectionFactory _factory;
    private readonly Func<CancellationToken, Task>? _postSwitchVerificationProbe;

    public BackupService(AppPaths paths, SqliteConnectionFactory factory)
        : this(paths, factory, postSwitchVerificationProbe: null)
    {
    }

    internal BackupService(
        AppPaths paths,
        SqliteConnectionFactory factory,
        Func<CancellationToken, Task>? postSwitchVerificationProbe)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(factory);
        _paths = paths;
        _factory = factory;
        _postSwitchVerificationProbe = postSwitchVerificationProbe;
    }

    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var destination = Path.Combine(
            destinationDirectory,
            $"contactcore-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");

        await using var source = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var target = await _factory
            .OpenPathAsync(destination, readOnly: false, pooling: false, cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        source.BackupDatabase(target);
        cancellationToken.ThrowIfCancellationRequested();
        await VerifyContactCoreDatabaseAsync(target, requireCurrentIdentity: true, cancellationToken).ConfigureAwait(false);
        return destination;
    }

    public async Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFile);
        var backupPath = Path.GetFullPath(backupFile);
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file does not exist.", backupPath);

        if (string.Equals(backupPath, _factory.DatabasePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The backup source cannot be the active ContactCore database.", nameof(backupFile));

        // Read-only structural verification rejects corrupt/non-ContactCore/future-schema files before
        // any copy of the active database is touched.
        await using (var probe = await _factory
            .OpenPathAsync(backupPath, readOnly: true, pooling: false, cancellationToken)
            .ConfigureAwait(false))
        {
            await VerifyContactCoreDatabaseAsync(probe, requireCurrentIdentity: false, cancellationToken).ConfigureAwait(false);
        }

        Directory.CreateDirectory(_paths.DataDirectory);
        Directory.CreateDirectory(_paths.BackupDirectory);

        var token = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}";
        var recoveryPath = Path.Combine(_paths.BackupDirectory, $"pre-restore-{token}.db");
        var stagingPath = _paths.DatabasePath + $".restore-{Guid.NewGuid():N}.tmp";
        var failedRestorePath = Path.Combine(_paths.BackupDirectory, $"failed-restore-{token}.db");
        var hadActiveDatabase = File.Exists(_paths.DatabasePath);

        try
        {
            if (hadActiveDatabase)
                await SnapshotActiveDatabaseAsync(recoveryPath, cancellationToken).ConfigureAwait(false);

            File.Copy(backupPath, stagingPath, overwrite: false);
            cancellationToken.ThrowIfCancellationRequested();

            // Migrate and fully verify the staging copy before it is allowed to replace the active file.
            // This means an incompatible migration cannot strand the user on a broken restored database.
            var stagingFactory = _factory.ForPath(stagingPath);
            await new DatabaseMigrator(stagingFactory).ApplyAsync(cancellationToken).ConfigureAwait(false);
            await using (var staged = await stagingFactory.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                await VerifyContactCoreDatabaseAsync(staged, requireCurrentIdentity: true, cancellationToken).ConfigureAwait(false);
            }

            SqliteConnection.ClearAllPools();
            DeleteSidecars(_paths.DatabasePath);
            File.Move(stagingPath, _paths.DatabasePath, overwrite: true);

            try
            {
                // Internal deterministic probe used by infrastructure tests to exercise the otherwise
                // timing/filesystem-dependent post-switch rollback path. Production construction leaves it null.
                if (_postSwitchVerificationProbe is not null)
                    await _postSwitchVerificationProbe(cancellationToken).ConfigureAwait(false);

                await using var restored = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
                await VerifyContactCoreDatabaseAsync(restored, requireCurrentIdentity: true, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                SqliteConnection.ClearAllPools();
                DeleteSidecars(_paths.DatabasePath);

                if (File.Exists(_paths.DatabasePath))
                    File.Move(_paths.DatabasePath, failedRestorePath, overwrite: true);

                if (hadActiveDatabase && File.Exists(recoveryPath))
                    File.Copy(recoveryPath, _paths.DatabasePath, overwrite: true);

                throw;
            }
        }
        finally
        {
            if (File.Exists(stagingPath)) File.Delete(stagingPath);
        }
    }

    private async Task SnapshotActiveDatabaseAsync(string destination, CancellationToken cancellationToken)
    {
        await using var source = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var target = await _factory
            .OpenPathAsync(destination, readOnly: false, pooling: false, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        source.BackupDatabase(target);
        await VerifyContactCoreDatabaseAsync(target, requireCurrentIdentity: true, cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyContactCoreDatabaseAsync(
        SqliteConnection connection,
        bool requireCurrentIdentity,
        CancellationToken cancellationToken)
    {
        await using (var integrity = connection.CreateCommand())
        {
            integrity.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(
                await integrity.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Backup failed SQLite integrity_check.");
        }

        await using (var identity = connection.CreateCommand())
        {
            identity.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name IN ('contacts', 'schema_migrations');
                """;
            var requiredTables = Convert.ToInt32(
                await identity.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture);
            if (requiredTables != 2)
                throw new InvalidDataException("The selected database is valid SQLite but is not a ContactCore backup.");
        }

        var version = await DatabaseMigrator.CurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (version <= 0)
            throw new InvalidDataException("The selected database does not contain a valid ContactCore schema version.");
        if (version > DatabaseMigrator.LatestSchemaVersion)
            throw new NotSupportedException($"Backup schema version {version} is newer than this ContactCore build supports ({DatabaseMigrator.LatestSchemaVersion}).");

        if (requireCurrentIdentity || version >= 2)
        {
            await using var familyCommand = connection.CreateCommand();
            familyCommand.CommandText = "SELECT value FROM app_metadata WHERE key='schema_family' LIMIT 1;";
            var family = Convert.ToString(
                await familyCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(family, "contactcore", StringComparison.Ordinal))
                throw new InvalidDataException("The selected database does not have the ContactCore schema identity marker.");
        }
    }

    private static void DeleteSidecars(string databasePath)
    {
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = databasePath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }
}
