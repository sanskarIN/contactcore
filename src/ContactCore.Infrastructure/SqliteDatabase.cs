using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class SqliteDatabase
{
    private const int CurrentSchemaVersion = 1;

    public SqliteDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required.", nameof(databasePath));
        DatabasePath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        ConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = true
        }.ToString();
    }

    public string DatabasePath { get; }
    public string ConnectionString { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await ExecuteAsync(connection, transaction, """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            """, cancellationToken).ConfigureAwait(false);

        var version = await GetSchemaVersionAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        if (version < 1)
        {
            await ExecuteAsync(connection, transaction, """
                CREATE TABLE contacts (
                    id TEXT PRIMARY KEY NOT NULL,
                    given_name TEXT NOT NULL DEFAULT '',
                    family_name TEXT NOT NULL DEFAULT '',
                    nickname TEXT NOT NULL DEFAULT '',
                    birthday TEXT NULL,
                    notes TEXT NOT NULL DEFAULT '',
                    is_favorite INTEGER NOT NULL DEFAULT 0 CHECK (is_favorite IN (0, 1)),
                    is_archived INTEGER NOT NULL DEFAULT 0 CHECK (is_archived IN (0, 1)),
                    created_utc TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                );

                CREATE TABLE phones (
                    id TEXT PRIMARY KEY NOT NULL,
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    label TEXT NOT NULL DEFAULT '',
                    number TEXT NOT NULL,
                    kind INTEGER NOT NULL
                );

                CREATE TABLE emails (
                    id TEXT PRIMARY KEY NOT NULL,
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    label TEXT NOT NULL DEFAULT '',
                    address TEXT NOT NULL,
                    kind INTEGER NOT NULL
                );

                CREATE TABLE addresses (
                    id TEXT PRIMARY KEY NOT NULL,
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    label TEXT NOT NULL DEFAULT '',
                    street TEXT NOT NULL DEFAULT '',
                    city TEXT NOT NULL DEFAULT '',
                    region TEXT NOT NULL DEFAULT '',
                    postal_code TEXT NOT NULL DEFAULT '',
                    country TEXT NOT NULL DEFAULT ''
                );

                CREATE TABLE organizations (
                    id TEXT PRIMARY KEY NOT NULL,
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    name TEXT NOT NULL,
                    title TEXT NULL,
                    department TEXT NULL
                );

                CREATE TABLE groups (
                    id TEXT PRIMARY KEY NOT NULL,
                    name TEXT NOT NULL COLLATE NOCASE UNIQUE
                );

                CREATE TABLE tags (
                    id TEXT PRIMARY KEY NOT NULL,
                    name TEXT NOT NULL COLLATE NOCASE UNIQUE
                );

                CREATE TABLE contact_groups (
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    group_id TEXT NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
                    PRIMARY KEY (contact_id, group_id)
                );

                CREATE TABLE contact_tags (
                    contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
                    tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
                    PRIMARY KEY (contact_id, tag_id)
                );

                CREATE INDEX ix_contacts_name ON contacts(family_name COLLATE NOCASE, given_name COLLATE NOCASE);
                CREATE INDEX ix_contacts_favorite ON contacts(is_favorite, is_archived);
                CREATE INDEX ix_phones_contact ON phones(contact_id);
                CREATE INDEX ix_phones_number ON phones(number);
                CREATE INDEX ix_emails_contact ON emails(contact_id);
                CREATE INDEX ix_emails_address ON emails(address COLLATE NOCASE);
                CREATE INDEX ix_addresses_contact ON addresses(contact_id);
                CREATE INDEX ix_organizations_contact ON organizations(contact_id);
                CREATE INDEX ix_contact_groups_contact ON contact_groups(contact_id);
                CREATE INDEX ix_contact_tags_contact ON contact_tags(contact_id);
                """, cancellationToken).ConfigureAwait(false);

            await using var migration = connection.CreateCommand();
            migration.Transaction = (SqliteTransaction)transaction;
            migration.CommandText = "INSERT INTO schema_migrations(version, applied_utc) VALUES ($version, $applied);";
            migration.Parameters.AddWithValue("$version", 1);
            migration.Parameters.AddWithValue("$applied", DateTimeOffset.UtcNow.ToString("O"));
            await migration.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            version = 1;
        }

        if (version != CurrentSchemaVersion)
            throw new InvalidOperationException($"Unsupported ContactCore database schema version {version}; expected {CurrentSchemaVersion}.");

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<int> GetSchemaVersionAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
