# Storage, Backup, and Recovery

ContactCore is intentionally local-first. This document describes where data lives, how SQLite connections are configured, how normal writes remain atomic, and exactly how backup/restore attempts protect the active database.

## Local data directory

`AppPaths` chooses the data directory in this order:

1. `CONTACTCORE_DATA_PATH` when it is non-empty;
2. the operating system's local application-data directory plus `ContactCore`;
3. `AppContext.BaseDirectory/ContactCore` when the platform API provides no local-app-data path.

The selected path is normalized with `Path.GetFullPath` and created during `AppPaths` construction.

Files/directories derived from it are:

- `contactcore.db` — active SQLite database;
- `settings.json` — local non-secret preferences;
- `backups/` — ContactCore-managed pre-restore/failed-restore recovery artifacts.

The environment override is a **directory**, not a database-file path.

## SQLite connection policy

`SqliteConnectionFactory` builds connections with:

- `ReadWriteCreate` for normal writable database use;
- `ReadOnly` when probing a selected restore source;
- shared cache;
- configurable pooling (enabled for the active database, disabled for one-off backup/probe files);
- `PRAGMA foreign_keys = ON`;
- `PRAGMA busy_timeout = 5000`.

Any connection-open/configuration failure disposes the partially opened connection before rethrowing.

## Optional keyed SQLite

A key provider supplies the runtime database key. If the key is non-empty, ContactCore:

1. converts the UTF-8 key bytes to hex;
2. sends `PRAGMA key = "x'<hex>'"`;
3. queries `PRAGMA cipher_version`;
4. closes and rejects the connection when no cipher version is reported.

This is a fail-closed integration boundary. Supplying a key to an ordinary SQLite build must not be interpreted as successful encryption.

`JsonAppPreferences` reads `CONTACTCORE_DATABASE_KEY` at runtime but deliberately excludes `DatabaseKey` from the persisted JSON model.

## Normal contact writes

The repository writes complete contact aggregates transactionally. For bulk import, the complete batch uses one transaction. The contact row is upserted, existing child/link rows for that contact are removed, and the current child/link set is inserted. On failure the transaction is rolled back.

This means callers should treat the aggregate passed to `UpsertAsync`/`UpsertManyAsync` as the desired complete persisted state.

## Creating a backup

`BackupService.CreateBackupAsync`:

1. validates/creates the destination directory;
2. creates a collision-resistant filename such as `contactcore-YYYYMMDD-HHmmssfff-<guid>.db`;
3. opens the active database normally;
4. opens the destination database with pooling disabled;
5. uses SQLite's `BackupDatabase` API;
6. runs integrity and ContactCore identity verification on the produced file;
7. returns the backup path only after verification succeeds.

Using SQLite's backup API is important for active WAL-mode databases; blindly copying the main `.db` file can miss state that still exists in WAL sidecars.

## Backup verification

Verification includes:

### SQLite integrity

`PRAGMA integrity_check` must return `ok`.

### Required ContactCore tables

Both `contacts` and `schema_migrations` must exist.

### Schema version

The current migration version must be greater than zero and cannot exceed the build's `LatestSchemaVersion`.

### Schema-family identity

Current-schema databases must contain `app_metadata` with `schema_family = contactcore`. Older supported schema versions can be accepted for restore long enough to migrate; current-created backups require the identity marker immediately.

A valid but unrelated SQLite file is therefore rejected.

## Restore flow

Restore is staged and rollback-aware.

### 1. Source validation

The selected path is normalized and must exist. It cannot be the same path as the active database.

The selected file is opened read-only and checked for integrity, ContactCore structure, supported schema version, and identity rules before active data is touched.

### 2. Pre-restore snapshot

If the active database exists, ContactCore creates a verified snapshot under `backups/` using a name such as:

`pre-restore-<timestamp>-<guid>.db`

This snapshot is the rollback source if the final switched database fails verification.

### 3. Staging

The selected backup is copied to a uniquely named temporary file beside the active database. The copy is not yet the live database.

### 4. Migration and verification in isolation

A path-specific connection factory is created for the staging file. `DatabaseMigrator.ApplyAsync` upgrades any supported older schema. The staged database is then fully reverified using current ContactCore identity requirements.

A migration or verification failure at this stage leaves the active database in place.

### 5. Switch

Before replacement, SQLite pools are cleared and `-wal`/`-shm` sidecars for the active path are deleted. The staged file then moves over the active database path.

### 6. Final verification

The newly active database is opened and fully verified again.

### 7. Rollback after failed final verification

If final verification fails:

1. pools are cleared again;
2. active sidecars are removed;
3. the failed active file is moved into `backups/failed-restore-<timestamp>-<guid>.db` when present;
4. the verified pre-restore snapshot is copied back to `contactcore.db` when there was an original active database;
5. the original exception is rethrown.

The staging temporary file is cleaned in a `finally` block.

## Recovery artifacts

Managed artifacts may include:

- `pre-restore-*.db` — verified snapshot of the pre-restore active database;
- `failed-restore-*.db` — database that failed after becoming active during restore;
- temporary `.restore-*.tmp` files — intended to be deleted automatically.

Do not assume recovery artifacts are harmless. They contain contact data and must receive the same privacy protection as the live database.

## What restore does not promise

- It does not create cloud redundancy.
- It does not encrypt plaintext backups by itself.
- It does not make backups immune to disk failure if live data and backups are stored on the same failed device.
- It does not support a database schema newer than the current application build.
- It does not perform downgrade migrations.

## Operational recommendations

- Keep at least one verified backup outside the primary device if the contacts matter.
- Protect backup media according to the sensitivity of the data.
- Test restore using non-production/fake contact data before relying on a release for critical records.
- Do not manually copy an open `contactcore.db` as the preferred backup mechanism; use the app's backup function.
- Never upload real database files to public GitHub issues or pull requests.
- Before deleting recovery artifacts, confirm that the active database opens and the intended contacts are present.

## Developer checklist for storage changes

When modifying persistence or recovery behavior:

- preserve parameterized SQL for user-controlled values;
- preserve transaction boundaries for aggregate/batch writes;
- add migrations rather than editing historical migration meaning;
- add upgrade and rollback-path tests;
- verify foreign keys remain enabled;
- keep keyed-SQLite behavior fail-closed;
- update `data-model.md`, this file, `security.md`, `testing.md`, and `CHANGELOG.md` where applicable.
