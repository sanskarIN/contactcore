# Storage, Backup, and Recovery

ContactCore is local-first on every current application target, but **local persistence is platform-specific**. Desktop/Android/iOS use the native SQLite path. Browser/WebAssembly uses IndexedDB through `BrowserContactRepository`. Backup/recovery claims must follow that distinction.

## Storage matrix

| Target family | Contact persistence | Preferences | Native DB backup/restore |
|---|---|---|---|
| Windows/Linux/macOS | SQLite | `settings.json` | yes |
| Android | SQLite | `settings.json` | yes in service capability; platform picker/runtime behavior still requires device validation |
| iOS/iPadOS | SQLite | `settings.json` | yes in service capability; platform picker/runtime behavior still requires device validation |
| Browser/WebAssembly | IndexedDB | browser local storage with session fallback | no |

## Native local data directory

`AppPaths` chooses native data location in this order:

1. `CONTACTCORE_DATA_PATH` when non-empty;
2. OS local application-data directory plus `ContactCore`;
3. `AppContext.BaseDirectory/ContactCore` if the runtime reports no local-app-data root.

Derived paths:

```text
ContactCore/
├── contactcore.db
├── settings.json
└── backups/
```

The environment override is a **directory**, not a database filename.

## Native SQLite connection policy

`SqliteConnectionFactory` centralizes:

- `ReadWriteCreate` normal access;
- `ReadOnly` backup-source probes;
- shared cache;
- controlled pooling;
- `PRAGMA foreign_keys = ON`;
- `PRAGMA busy_timeout = 5000`;
- optional fail-closed keyed-SQLite setup.

A partially opened/configured connection is disposed on failure.

## Optional native keyed SQLite

When a runtime key exists ContactCore:

1. converts key bytes to hex;
2. sends `PRAGMA key`;
3. queries `PRAGMA cipher_version`;
4. rejects/ closes the connection if compatible cipher support cannot be proven.

This is an integration boundary, not a claim that ordinary SQLite becomes encrypted merely because an environment variable was set. `JsonAppPreferences` never serializes the runtime key.

Browser/WebAssembly does not use this SQLite-key mechanism and reports native database encryption capability as unavailable.

## Native contact writes

`SqliteContactRepository` writes complete contact aggregates transactionally. Bulk import uses one transaction. Duplicate merge requires both reviewed records and performs survivor update + secondary deletion in one transaction.

A supplied `Contact` represents the desired complete persisted aggregate.

## Native backup creation

`BackupService.CreateBackupAsync`:

1. validates/creates destination directory;
2. creates a collision-resistant filename;
3. opens active SQLite database;
4. opens destination with one-off connection settings;
5. uses SQLite `BackupDatabase`;
6. verifies integrity/schema/version/ContactCore identity;
7. returns only a verified backup path.

Using SQLite's backup API matters for active WAL-mode databases; copying only the primary `.db` file can omit WAL state.

## Native backup verification

Verification includes:

- `PRAGMA integrity_check` must return `ok`;
- required `contacts` and `schema_migrations` tables;
- schema version > 0 and not newer than this build;
- current-schema `app_metadata` with `schema_family = contactcore`;
- supported legacy schema may be accepted for isolated migration during restore.

A valid unrelated SQLite database is rejected.

## Native staged restore

### 1. Validate source

Normalize path, require file existence, reject active DB itself, open read-only, verify integrity/structure/version/identity before modifying live data.

### 2. Create pre-restore snapshot

If an active DB exists, create a verified `pre-restore-<timestamp>-<guid>.db` under `backups/`.

### 3. Stage selected backup

Copy to a unique temporary file beside active data.

### 4. Migrate and verify staging

Create a path-specific factory, run `DatabaseMigrator.ApplyAsync`, then fully reverify. Failure leaves active DB unchanged.

### 5. Switch

Clear pools, remove relevant `-wal`/`-shm` sidecars, move staged DB over active path.

### 6. Verify active result

Open newly active DB and fully verify again.

### 7. Roll back a failed final verification

If final verification fails:

1. clear pools/sidecars;
2. retain failed active copy under `backups/failed-restore-...db` when possible;
3. copy verified pre-restore snapshot back when an original active DB existed;
4. rethrow original failure;
5. clean staging temp in `finally`.

## Native recovery artifacts

Possible artifacts:

- `pre-restore-*.db` — verified pre-restore state;
- `failed-restore-*.db` — failed switched database retained for diagnosis/recovery;
- `.restore-*.tmp` — staging temp intended for automatic cleanup.

All contain or may contain contact data and need the same protection as the live database.

## Browser/WebAssembly storage

The browser does **not** reference `ContactCore.Infrastructure` and does not own `contactcore.db`.

`BrowserContactRepository` implements `IContactRepository` and keeps a complete domain aggregate representation in a browser-local snapshot stored in IndexedDB.

### Initialization

- enter repository initialization gate;
- call JavaScript storage bridge;
- read IndexedDB contact-state record;
- deserialize documents into domain contacts;
- reject malformed JSON/duplicate contact IDs as invalid browser-store state;
- mark repository initialized only after load completes.

### Browser writes

Each write:

1. initializes repository;
2. enters `SemaphoreSlim` write gate;
3. snapshots current in-memory dictionary;
4. applies mutation;
5. serializes ordered full contact documents;
6. calls JavaScript `saveContacts`;
7. JavaScript performs IndexedDB readwrite transaction/`put`;
8. if persistence fails, restore prior in-memory dictionary and rethrow;
9. release gate.

This avoids leaving the current application instance in a half-mutated state when browser persistence throws.

### Browser duplicate merge

The browser repository requires both survivor and secondary IDs still exist before mutation. It updates survivor/removes secondary in the gated state replacement, then persists the resulting snapshot. Failed persistence restores the pre-merge in-memory snapshot.

It is not a collaborative/cross-tab transaction protocol. Multi-tab simultaneous editing/conflict resolution is not currently a supported synchronization feature.

## Browser preferences

Browser theme/reduced-motion/delete-confirmation settings use local browser storage. If storage access throws, the preference implementation retains a session fallback rather than failing contact initialization solely because preference persistence is blocked.

No SQLite database key is stored/used by the browser target.

## Browser backup/recovery boundary

There is no SQLite-native `BackupService` capability for WebAssembly. Shared UI receives:

```text
SupportsDatabaseBackups = false
SupportsDatabaseEncryption = false
```

and guides the user toward CSV/vCard export for portable copies.

Important consequences:

- clearing browser site data can remove contacts;
- private/incognito session teardown can remove contacts;
- profile deletion or enterprise policy can remove/block storage;
- browser quota/eviction behavior is controlled partly by browser policy;
- changing origins/hosts can produce a different browser storage namespace;
- CSV/vCard exports are still interchange formats with documented fidelity limits, not a native SQLite backup clone.

A future full-fidelity browser backup feature should define a versioned browser export/import format and integrity/migration semantics explicitly instead of reusing SQLite backup terminology.

## What storage/recovery does not promise

- no cloud redundancy/sync;
- no automatic off-device redundancy;
- no default native encryption-at-rest claim;
- no browser cryptographic-at-rest claim;
- no downgrade migration support;
- no support for future native schema in older build;
- no cross-tab browser synchronization/conflict resolution;
- no guarantee browser policy cannot evict/clear site data;
- no claim that CSV/vCard is a full-fidelity database backup.

## Operational recommendations

### Native users

- use verified database backup for full native recovery;
- keep at least one protected copy away from primary device if data matters;
- test restore with fictional/disposable data before relying on a release;
- do not use manual open-database file copying as preferred backup method;
- protect recovery artifacts like live data.

### Browser users

- explicitly export important contacts before clearing site data, changing browser profile, or major migration;
- do not rely on private browsing as durable storage;
- test browser releases in a disposable profile/origin;
- understand that moving deployment to another origin can change where browser-local data is found.

### Everyone

Never upload real DB files, browser data dumps, backups, exports, keys, or screenshots containing contacts to public issues/PRs.

## Developer checklist for storage changes

- preserve Application repository contracts;
- preserve native parameterized SQL;
- preserve native aggregate/batch transaction boundaries;
- preserve browser gated-write rollback behavior;
- add migrations instead of changing historical native migration meaning;
- version/migrate browser serialized state deliberately if representation changes;
- keep native keyed-SQLite fail-closed;
- never expose native backup/encryption controls on browser as false claims;
- add tests/build gates appropriate to changed target;
- update `data-model.md`, `architecture.md`, `security.md`, `testing.md`, `platform-support.md`, and `CHANGELOG.md` where relevant.
