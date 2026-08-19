using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class DatabaseMigrator(SqliteConnectionFactory factory)
{
    private static readonly string[] VersionOneTables =
    [
        "schema_migrations",
        "contacts",
        "phones",
        "emails",
        "addresses",
        "organizations",
        "groups",
        "tags",
        "contact_groups",
        "contact_tags"
    ];

    private static readonly string[] CurrentTables = [.. VersionOneTables, "app_metadata"];

    private static readonly IReadOnlyList<(int Version, string Sql)> Migrations = new[]
    {
        (1, """
        CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS contacts (
          id TEXT PRIMARY KEY,
          given_name TEXT NOT NULL DEFAULT '', family_name TEXT NOT NULL DEFAULT '', nickname TEXT NOT NULL DEFAULT '',
          birthday TEXT NULL, notes TEXT NOT NULL DEFAULT '', is_favorite INTEGER NOT NULL DEFAULT 0,
          is_archived INTEGER NOT NULL DEFAULT 0, created_at TEXT NOT NULL, updated_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS phones (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, number TEXT NOT NULL, kind INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS emails (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, address TEXT NOT NULL, kind INTEGER NOT NULL);
        CREATE TABLE IF NOT EXISTS addresses (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, label TEXT NOT NULL, street TEXT NOT NULL, city TEXT NOT NULL, region TEXT NOT NULL, postal_code TEXT NOT NULL, country TEXT NOT NULL);
        CREATE TABLE IF NOT EXISTS organizations (id TEXT PRIMARY KEY, contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, name TEXT NOT NULL, title TEXT NULL, department TEXT NULL);
        CREATE TABLE IF NOT EXISTS groups (id TEXT PRIMARY KEY, name TEXT NOT NULL COLLATE NOCASE UNIQUE);
        CREATE TABLE IF NOT EXISTS tags (id TEXT PRIMARY KEY, name TEXT NOT NULL COLLATE NOCASE UNIQUE);
        CREATE TABLE IF NOT EXISTS contact_groups (contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, group_id TEXT NOT NULL REFERENCES groups(id) ON DELETE CASCADE, PRIMARY KEY(contact_id, group_id));
        CREATE TABLE IF NOT EXISTS contact_tags (contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE, tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE, PRIMARY KEY(contact_id, tag_id));
        CREATE INDEX IF NOT EXISTS ix_contacts_name ON contacts(family_name, given_name);
        CREATE INDEX IF NOT EXISTS ix_contacts_flags ON contacts(is_archived, is_favorite);
        CREATE INDEX IF NOT EXISTS ix_phones_number ON phones(number);
        CREATE INDEX IF NOT EXISTS ix_emails_address ON emails(address COLLATE NOCASE);
        """),
        (2, """
        CREATE TABLE IF NOT EXISTS app_metadata (key TEXT PRIMARY KEY, value TEXT NOT NULL);
        INSERT OR IGNORE INTO app_metadata(key, value) VALUES ('schema_family', 'contactcore');
        """)
    };

    public static int LatestSchemaVersion => Migrations.Max(x => x.Version);

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);

        var existingTables = await ReadUserTablesAsync(connection, cancellationToken).ConfigureAwait(false);
        var current = 0;
        if (existingTables.Count > 0)
        {
            await ValidateExistingDatabaseBeforeMutationAsync(connection, existingTables, cancellationToken).ConfigureAwait(false);
            current = await CurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        }

        foreach (var migration in Migrations.Where(x => x.Version > current).OrderBy(x => x.Version))
        {
            await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = (SqliteTransaction)tx;
                cmd.CommandText = migration.Sql;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                cmd.CommandText = "INSERT INTO schema_migrations(version, applied_at) VALUES ($version, $at);";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$version", migration.Version);
                cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { await tx.RollbackAsync(cancellationToken).ConfigureAwait(false); }
                catch (InvalidOperationException) { }
                throw;
            }
        }

        var migratedTables = await ReadUserTablesAsync(connection, cancellationToken).ConfigureAwait(false);
        EnsureRequiredTables(migratedTables, CurrentTables);
        await EnsureSchemaIdentityAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<int> CurrentVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ValidateExistingDatabaseBeforeMutationAsync(
        SqliteConnection connection,
        IReadOnlySet<string> existingTables,
        CancellationToken cancellationToken)
    {
        if (!existingTables.Contains("schema_migrations") || !existingTables.Contains("contacts"))
            throw new InvalidDataException("The existing database is valid SQLite but is not a recognized ContactCore database. No schema changes were applied.");

        var current = await CurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (current <= 0)
            throw new InvalidDataException("The existing database does not contain a valid ContactCore schema version. No schema changes were applied.");
        if (current > LatestSchemaVersion)
            throw new NotSupportedException($"Database schema version {current} is newer than this ContactCore build supports ({LatestSchemaVersion}).");

        EnsureRequiredTables(existingTables, VersionOneTables);

        if (current >= 2)
        {
            if (!existingTables.Contains("app_metadata"))
                throw new InvalidDataException("The existing database is missing the ContactCore schema identity table. No schema changes were applied.");
            await EnsureSchemaIdentityAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlySet<string>> ReadUserTablesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT GLOB 'sqlite_*';";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            tables.Add(reader.GetString(0));
        return tables;
    }

    private static void EnsureRequiredTables(IReadOnlySet<string> existingTables, IEnumerable<string> requiredTables)
    {
        var missing = requiredTables.Where(table => !existingTables.Contains(table)).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException("The database is missing required ContactCore schema tables.");
    }

    private static async Task EnsureSchemaIdentityAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (LatestSchemaVersion < 2) return;
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM app_metadata WHERE key='schema_family' LIMIT 1;";
        var family = Convert.ToString(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(family, "contactcore", StringComparison.Ordinal))
            throw new InvalidDataException("The database schema identity is not ContactCore.");
    }
}
