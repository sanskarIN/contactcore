using ContactCore.Application;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class BackupService(AppPaths paths, SqliteConnectionFactory factory) : IBackupService
{
    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var destination = Path.Combine(
            destinationDirectory,
            $"contactcore-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.db");

        await using var source = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var target = await factory
            .OpenPathAsync(destination, readOnly: false, pooling: false, cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        source.BackupDatabase(target);
        cancellationToken.ThrowIfCancellationRequested();
        await VerifyContactCoreDatabaseAsync(target, cancellationToken).ConfigureAwait(false);
        return destination;
    }

    public async Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFile);
        var backupPath = Path.GetFullPath(backupFile);
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file does not exist.", backupPath);

        if (string.Equals(backupPath, factory.DatabasePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The backup source cannot be the active ContactCore database.", nameof(backupFile));

        // Verify the candidate with the same encryption configuration used by the active database.
        await using (var probe = await factory
            .OpenPathAsync(backupPath, readOnly: true, pooling: false, cancellationToken)
            .ConfigureAwait(false))
        {
            await VerifyContactCoreDatabaseAsync(probe, cancellationToken).ConfigureAwait(false);
        }

        Directory.CreateDirectory(paths.DataDirectory);
        Directory.CreateDirectory(paths.BackupDirectory);

        var token = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}";
        var recoveryPath = Path.Combine(paths.BackupDirectory, $"pre-restore-{token}.db");
        var stagingPath = paths.DatabasePath + $".restore-{Guid.NewGuid():N}.tmp";
        var failedRestorePath = Path.Combine(paths.BackupDirectory, $"failed-restore-{token}.db");
        var hadActiveDatabase = File.Exists(paths.DatabasePath);

        try
        {
            if (hadActiveDatabase)
                await SnapshotActiveDatabaseAsync(recoveryPath, cancellationToken).ConfigureAwait(false);

            File.Copy(backupPath, stagingPath, overwrite: false);
            cancellationToken.ThrowIfCancellationRequested();

            // Ensure no pooled handle can keep an old WAL/SHM pair alive while the database file is replaced.
            SqliteConnection.ClearAllPools();
            DeleteSidecars(paths.DatabasePath);
            File.Move(stagingPath, paths.DatabasePath, overwrite: true);

            try
            {
                await using var restored = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
                await VerifyContactCoreDatabaseAsync(restored, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                SqliteConnection.ClearAllPools();
                DeleteSidecars(paths.DatabasePath);

                if (File.Exists(paths.DatabasePath))
                    File.Move(paths.DatabasePath, failedRestorePath, overwrite: true);

                if (hadActiveDatabase && File.Exists(recoveryPath))
                    File.Copy(recoveryPath, paths.DatabasePath, overwrite: true);
                else if (File.Exists(paths.DatabasePath))
                    File.Delete(paths.DatabasePath);

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
        await using var source = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var target = await factory
            .OpenPathAsync(destination, readOnly: false, pooling: false, cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        source.BackupDatabase(target);
        await VerifyContactCoreDatabaseAsync(target, cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyContactCoreDatabaseAsync(
        SqliteConnection connection,
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

        await using var identity = connection.CreateCommand();
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

    private static void DeleteSidecars(string databasePath)
    {
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = databasePath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }
}
