# Changelog

All notable changes to ContactCore are documented here. This project follows Semantic Versioning.

## [Unreleased]

### Added
- Cross-platform Avalonia desktop shell for Windows, macOS, and Linux.
- SQLite contact persistence with schema migrations and transactional child-record updates.
- Contact create/edit/delete, favorite/archive model support, search/filter/alphabet navigation architecture.
- Groups, tags, multiple field models, CSV/vCard codecs, duplicate scoring/merge engine, backups, settings, privacy/security docs, tests, CI, CodeQL, and release automation.
- Fail-closed hook for SQLCipher-compatible database encryption providers.
- Explicit spreadsheet-safe CSV export mode that neutralizes common formula-leading cell values without changing the lossless default export.
- Backup regression coverage for successful restore, invalid backup rejection, and rollback after post-restore migration failure.
- Additional CSV/vCard edge and deterministic randomized parser tests.

### Changed
- Updated Microsoft.Data.Sqlite to 10.0.11 to move the native SQLite dependency chain past the vulnerable SQLitePCLRaw 2.1.11 package.
- CI now preserves diagnostics across Windows, Linux, and macOS, uses the repository SDK policy from `global.json`, uploads per-platform test evidence, and uses current GitHub Action generations.
- CodeQL uses the current major action generation with bounded, reproducible restore/build analysis.
- Release automation produces clean `.zip`/`.tar.gz` archives per runtime identifier and fails when expected artifacts are missing.
- Date-only import/export and editor parsing now use invariant culture explicitly.

### Fixed
- Older asynchronous contact-search results can no longer overwrite newer search queries.
- Backup restore now stages candidates, preserves the live database, applies migrations, verifies integrity after restore, and automatically rolls back on failure.
- Restore clears pooled SQLite connections and stale WAL/SHM sidecars before replacing database files.
