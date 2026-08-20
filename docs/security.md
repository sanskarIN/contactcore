# Security and encryption

## Threat model
ContactCore protects against accidental cloud disclosure by being offline-first, against common SQL injection by parameterized statements, against partially restored data by staging/integrity checks with automatic rollback, against misleading encryption configuration by fail-closed verification, and against common spreadsheet formula execution risks through an explicit spreadsheet-safe CSV export mode.

## Database encryption option
The open-source default build uses ordinary SQLite. If `CONTACTCORE_DATABASE_KEY` is set, `SqliteConnectionFactory` sends a hex-encoded `PRAGMA key` and then queries `PRAGMA cipher_version`. If no cipher version is reported, startup fails. This means users may integrate a maintained SQLCipher-compatible native SQLite distribution without ContactCore ever silently treating plaintext SQLite as encrypted.

As of August 2026, the formerly convenient community encryption bundles in SQLitePCLRaw are deprecated. For production encryption, use a currently supported vendor/native build (for example official SQLCipher builds from its maintainer) or a carefully maintained internal native build. Do not commit proprietary binaries or license keys to this repository.

## Backup restore safety
A restore is verified before and after replacement. ContactCore stages the candidate database, clears pooled SQLite connections, removes stale WAL/SHM sidecars, keeps a rollback copy of the live database, applies schema migrations to the restored candidate, performs another SQLite integrity check, and restores the previous database automatically if migration or verification fails.

Backups are local database files and may contain private contact information. Store and transfer them with the same care as the live database.

## CSV and spreadsheet formula safety
`ContactCsvCodec.Export` is the lossless/general CSV export and deliberately preserves text exactly. A value such as `=1+1` therefore remains `=1+1` after a normal export/import round trip.

`ContactCsvCodec.ExportForSpreadsheet` is the explicit mode for CSV that will be opened directly in spreadsheet applications. It prefixes text cells that begin with common formula-trigger characters (`=`, `+`, `-`, `@`, tab, carriage return, newline, and full-width equivalents) with an apostrophe before normal CSV escaping. This prevents those exported cells from beginning directly with a formula trigger.

Spreadsheet behavior differs across products and can change when a CSV is edited or re-saved. The spreadsheet-safe mode is a defensive export policy, not a claim that CSV can provide universal active-content security. Use the normal export for exact machine-to-machine round trips and the spreadsheet-safe export for direct spreadsheet viewing.

## Secrets
No app secret is required. Database keys should be supplied at runtime or by an OS secret store integration; `.env` is ignored and `.env.example` contains names only.
