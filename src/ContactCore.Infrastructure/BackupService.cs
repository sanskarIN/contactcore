using ContactCore.Application;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class BackupService(AppPaths paths, SqliteConnectionFactory factory) : IBackupService
{
    public async Task<string> CreateBackupAsync(
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        var destination = Path.Combine(
            destinationDirectory,
            $"contactcore-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.db");

        await using var source = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var target = await factory
            .OpenPathAsync(destination, SqliteOpenMode.ReadWriteCreate, cancellationToken)
            .ConfigureAwait(false);

        source.BackupDatabase(target);
        await VerifyIntegrityAsync(target, cancellationToken).ConfigureAwait(false);
        return destination;
    }

    public async Task RestoreBackupAsync(
        string backupFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFile);
        backupFile = Path.GetFullPath(backupFile);
        if (!File.Exists(backupFile))
            throw new FileNotFoundException("Backup file does not exist.", backupFile);

        await using (var probe = await factory
            .OpenPathAsync(backupFile, SqliteOpenMode.ReadOnly, cancellationToken)
            .ConfigureAwait(false))
        {
            await VerifyIntegrityAsync(probe, cancellationToken).ConfigureAwait(false);
            await VerifyContactCoreSchemaAsync(probe, cancellationToken).ConfigureAwait(false);
        }

        Directory.CreateDirectory(paths.BackupDirectory);
        if (File.Exists(paths.DatabasePath))
        {
            // Preserve the current valid database before replacing it. The SQLite backup API
            // also captures committed WAL content and preserves configured encryption.
            _ = await CreateBackupAsync(paths.BackupDirectory, cancellationToken).ConfigureAwait(false);
        }

        SqliteConnection.ClearAllPools();

        var staging = paths.DatabasePath + ".restore";
        try
        {
            File.Copy(backupFile, staging, overwrite: true);
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var sidecar = paths.DatabasePath + suffix;
                if (File.Exists(sidecar)) File.Delete(sidecar);
            }

            File.Move(staging, paths.DatabasePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    private static async Task VerifyIntegrityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = "PRAGMA integrity_check;";
        var integrity = Convert.ToString(
            await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The SQLite database failed integrity_check and was not used.");
    }

    private static async Task VerifyContactCoreSchemaAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='contacts';";
        var count = Convert.ToInt32(
            await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            System.Globalization.CultureInfo.InvariantCulture);
        if (count != 1)
            throw new InvalidDataException("The selected database is not a recognizable ContactCore backup.");
    }
}
