using ContactCore.Application;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class BackupService(AppPaths paths, SqliteConnectionFactory factory) : IBackupService
{
    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);
        await using var source = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var destination = Path.Combine(destinationDirectory, $"contactcore-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}.db");
        var builder = new SqliteConnectionStringBuilder { DataSource = destination, Mode = SqliteOpenMode.ReadWriteCreate };
        await using var target = new SqliteConnection(builder.ToString());
        await target.OpenAsync(cancellationToken).ConfigureAwait(false);
        source.BackupDatabase(target);
        return destination;
    }

    public async Task RestoreBackupAsync(string backupFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFile);
        if (!File.Exists(backupFile)) throw new FileNotFoundException("Backup file does not exist.", backupFile);

        await VerifyIntegrityAsync(backupFile, cancellationToken).ConfigureAwait(false);

        Directory.CreateDirectory(paths.DataDirectory);
        var staging = paths.DatabasePath + ".restore";
        var rollback = paths.DatabasePath + ".pre-restore";
        var hadExistingDatabase = File.Exists(paths.DatabasePath);

        TryDelete(staging);
        TryDelete(rollback);
        File.Copy(backupFile, staging, true);

        SqliteConnection.ClearAllPools();
        DeleteSidecars();

        if (hadExistingDatabase)
        {
            File.Copy(paths.DatabasePath, rollback, true);
        }

        try
        {
            File.Move(staging, paths.DatabasePath, true);
            await new DatabaseMigrator(factory).ApplyAsync(cancellationToken).ConfigureAwait(false);
            await VerifyIntegrityAsync(paths.DatabasePath, cancellationToken).ConfigureAwait(false);
            TryDelete(rollback);
        }
        catch
        {
            SqliteConnection.ClearAllPools();
            DeleteSidecars();

            if (hadExistingDatabase && File.Exists(rollback))
            {
                File.Copy(rollback, paths.DatabasePath, true);
            }
            else
            {
                TryDelete(paths.DatabasePath);
            }

            throw;
        }
        finally
        {
            TryDelete(staging);
            TryDelete(rollback);
        }
    }

    private static async Task VerifyIntegrityAsync(string databaseFile, CancellationToken cancellationToken)
    {
        var probeBuilder = new SqliteConnectionStringBuilder { DataSource = databaseFile, Mode = SqliteOpenMode.ReadOnly };
        await using var probe = new SqliteConnection(probeBuilder.ToString());
        await probe.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var check = probe.CreateCommand();
        check.CommandText = "PRAGMA integrity_check;";
        var integrity = Convert.ToString(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Backup failed SQLite integrity_check.");
        }
    }

    private void DeleteSidecars()
    {
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            TryDelete(paths.DatabasePath + suffix);
        }
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
