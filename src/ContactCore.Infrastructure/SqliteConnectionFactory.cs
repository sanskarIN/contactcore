using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteConnectionFactory
{
    private readonly string _databasePath;
    private readonly Func<string?>? _keyProvider;

    public SqliteConnectionFactory(string databasePath, Func<string?>? keyProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _databasePath = Path.GetFullPath(databasePath);
        _keyProvider = keyProvider;
    }

    public bool IsEncryptionRequested => !string.IsNullOrEmpty(_keyProvider?.Invoke());

    public Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default) =>
        OpenPathAsync(_databasePath, SqliteOpenMode.ReadWriteCreate, cancellationToken);

    public async Task<SqliteConnection> OpenPathAsync(
        string databasePath,
        SqliteOpenMode mode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = mode,
            Cache = SqliteCacheMode.Private,
            Pooling = true
        };

        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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

    private static async Task ApplyEncryptionKeyAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken)
    {
        // SQLCipher does not support binding PRAGMA key as a normal SQL parameter.
        // Encode the UTF-8 key as a raw hexadecimal key literal so untrusted text never
        // becomes executable SQL syntax.
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hex = Convert.ToHexString(keyBytes);
        await ExecuteAsync(connection, $"PRAGMA key = \"x'{hex}'\";", cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA cipher_version;";
        var version = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(
                "Database encryption was requested, but this build is not using a SQLCipher-compatible SQLite provider. " +
                "ContactCore refuses to silently store or back up an unencrypted database.");
        }
    }

    internal static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
