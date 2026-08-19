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
        var destination = Path.Combine(destinationDirectory, $"contactcore-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.db");
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
        var probeBuilder = new SqliteConnectionStringBuilder { DataSource = backupFile, Mode = SqliteOpenMode.ReadOnly };
        await using (var probe = new SqliteConnection(probeBuilder.ToString()))
        {
            await probe.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var check = probe.CreateCommand();
            check.CommandText = "PRAGMA integrity_check;";
            var integrity = Convert.ToString(await check.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Backup failed SQLite integrity_check.");
        }
        Directory.CreateDirectory(paths.DataDirectory);
        var staging = paths.DatabasePath + ".restore";
        File.Copy(backupFile, staging, true);
        File.Move(staging, paths.DatabasePath, true);
        foreach (var suffix in new[] { "-wal", "-shm" }) { var sidecar = paths.DatabasePath + suffix; if (File.Exists(sidecar)) File.Delete(sidecar); }
    }
}
