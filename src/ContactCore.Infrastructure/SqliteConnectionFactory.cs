using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteConnectionFactory
{
    private readonly Func<string?>? _keyProvider;

    public SqliteConnectionFactory(string databasePath, Func<string?>? keyProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        DatabasePath = Path.GetFullPath(databasePath);
        _keyProvider = keyProvider;
    }

    public string DatabasePath { get; }

    public Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default) =>
        OpenPathAsync(DatabasePath, readOnly: false, pooling: true, cancellationToken);

    public Task<SqliteConnection> OpenPathAsync(
        string databasePath,
        bool readOnly,
        bool pooling = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        return OpenCoreAsync(Path.GetFullPath(databasePath), readOnly, pooling, cancellationToken);
    }

    private async Task<SqliteConnection> OpenCoreAsync(
        string databasePath,
        bool readOnly,
        bool pooling,
        CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = pooling
        };

        var connection = new SqliteConnection(builder.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var key = _keyProvider?.Invoke();
            if (!string.IsNullOrEmpty(key))
                await ApplyEncryptionKeyAsync(connection, key, cancellationToken).ConfigureAwait(false);

            await ExecuteAsync(connection, "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;", cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task ApplyEncryptionKeyAsync(SqliteConnection connection, string key, CancellationToken cancellationToken)
    {
        // PRAGMA key only becomes effective when the process is using a SQLCipher-compatible SQLite build.
        // Parameterization is not supported for PRAGMA key, so encode UTF-8 bytes as a hex key literal and
        // never interpolate the original secret into SQL text.
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hex = Convert.ToHexString(keyBytes);
        await ExecuteAsync(connection, $"PRAGMA key = \"x'{hex}'\";", cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA cipher_version;";
        var version = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (string.IsNullOrWhiteSpace(version))
        {
            await connection.CloseAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                "Database encryption was requested, but this build is not using a SQLCipher-compatible SQLite provider. " +
                "ContactCore refuses to silently store an unencrypted database.");
        }
    }

    internal static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
