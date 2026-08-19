using ContactCore.Application;
using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteBackupService(SqliteDatabase database) : IBackupService
{
    private readonly SqliteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    public async Task<BackupResult> CreateBackupAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentException("Backup destination is required.", nameof(destinationPath));
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(destinationPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(fullPath)) File.Delete(fullPath);

        await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await using var source = await _database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());
        await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        source.BackupDatabase(destination);
        await EnsureIntegrityAsync(destination, cancellationToken).ConfigureAwait(false);

        var info = new FileInfo(fullPath);
        return new BackupResult(fullPath, info.Length, DateTimeOffset.UtcNow);
    }

    public async Task RestoreBackupAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("Backup source is required.", nameof(sourcePath));
        var fullSource = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSource)) throw new FileNotFoundException("Backup file was not found.", fullSource);
        if (string.Equals(fullSource, _database.DatabasePath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The backup source cannot be the active database file.", nameof(sourcePath));

        await using (var candidate = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullSource,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString()))
        {
            await candidate.OpenAsync(cancellationToken).ConfigureAwait(false);
            await EnsureIntegrityAsync(candidate, cancellationToken).ConfigureAwait(false);
        }

        var staging = _database.DatabasePath + ".restore-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.Copy(fullSource, staging, overwrite: false);
            cancellationToken.ThrowIfCancellationRequested();
            SqliteConnection.ClearAllPools();
            File.Move(staging, _database.DatabasePath, overwrite: true);
            await _database.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(staging)) File.Delete(staging);
        }
    }

    private static async Task EnsureIntegrityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"SQLite integrity check failed: {result ?? "no result"}.");
    }
}
