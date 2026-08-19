using Microsoft.Data.Sqlite;

namespace ContactCore.Infrastructure;

public sealed class DatabaseMigrator(SqliteConnectionFactory factory)
{
    private static readonly IReadOnlyList<(int Version, string Sql)> Migrations = new[]
    {
        (1, """
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
        """),
        (3, """
        CREATE TABLE IF NOT EXISTS contact_dates (
          id TEXT PRIMARY KEY,
          contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
          label TEXT NOT NULL,
          date_value TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS contact_notes (
          id TEXT PRIMARY KEY,
          contact_id TEXT NOT NULL REFERENCES contacts(id) ON DELETE CASCADE,
          label TEXT NOT NULL,
          content TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_contact_dates_contact ON contact_dates(contact_id);
        CREATE INDEX IF NOT EXISTS ix_contact_notes_contact ON contact_notes(contact_id);
        """)
    };

    public int LatestVersion => Migrations.Max(x => x.Version);

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await VerifyIntegrityAsync(connection, cancellationToken).ConfigureAwait(false);
        await SqliteConnectionFactory.ExecuteAsync(
            connection,
            "CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, applied_at TEXT NOT NULL);",
            cancellationToken).ConfigureAwait(false);

        var current = await CurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (current > LatestVersion)
        {
            throw new InvalidOperationException(
                $"This database uses schema version {current}, but this ContactCore build supports only up to {LatestVersion}. " +
                "Use a newer ContactCore version rather than attempting a downgrade.");
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
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        await VerifyIntegrityAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyIntegrityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA quick_check;";
        var result = Convert.ToString(
            await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            System.Globalization.CultureInfo.InvariantCulture);
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "The ContactCore database failed SQLite quick_check. Migration was stopped to avoid worsening corruption.");
        }
    }

    private static async Task<int> CurrentVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
        return Convert.ToInt32(
            await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
