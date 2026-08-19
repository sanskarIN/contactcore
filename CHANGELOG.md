# Changelog

All notable changes to ContactCore are documented here. The project intends to use Semantic Versioning for published releases. Until a version is tagged, work remains under **Unreleased**.

## [Unreleased]

### Added

- Cross-platform Avalonia desktop shell for Windows, macOS, and Linux.
- Layered Domain/Application/Infrastructure/Desktop solution plus one test project per production layer.
- Rich contact domain/storage model for names, birthday, notes, favorite/archive state, multiple phones/emails/addresses/organizations, groups, and tags.
- SQLite contact persistence with schema migrations, foreign keys, indexes, transactional aggregate updates, and case-insensitive group/tag identities.
- Local search across names/phones/emails, favorite/archive filtering, and A–Z navigation.
- CSV and focused vCard 4.0 import/export codecs.
- Duplicate scoring and deterministic application-layer merge logic.
- SQLite-native backup creation with integrity/version/ContactCore identity verification.
- Data tools desktop surface for import, CSV/vCard export, backup creation, and restore.
- Settings/About/privacy desktop surface with System/Light/Dark theme, reduced-motion preference, and permanent-delete confirmation preference.
- Keyboard shortcuts (`Ctrl+N`, `Ctrl+S`, `Ctrl+F`, `Esc`) and explicit visible-focus styling.
- Cross-platform GitHub Actions CI, CodeQL, Dependabot, and tag-driven self-contained release publishing.
- Documentation hub plus deep user, setup, architecture, data model, desktop UI, import/export, storage/recovery, security, accessibility, performance, development, testing, CI/CD, release, troubleshooting, maintainer, ADR, and exhaustive file-reference documentation.
- Desktop regression tests proving that compact edits preserve additional/unexposed rich contact child data.

### Changed

- Bulk contact persistence now supports `UpsertManyAsync` so imports can be written in one SQLite transaction.
- `ContactService.ImportAsync` normalizes and validates the complete batch before persistence and prefixes validation fields with the imported contact index.
- User search text is trimmed and SQL `LIKE` wildcard characters are escaped so `%`, `_`, and backslash are treated literally in the intended search pattern.
- Duplicate matching/merge handling has stronger null/self-merge safeguards, threshold clamping, normalized field comparison, and fresh IDs for child records copied from a secondary contact.
- Preference writes use a temporary file followed by replacement; malformed preference JSON falls back to safe defaults.
- Database encryption configuration is runtime-only in preferences rather than serialized into `settings.json`.
- Restore flow now verifies selected input before active data changes, stages/migrates/verifies before replacement, retains a verified pre-restore snapshot, verifies after the switch, and attempts rollback on final verification failure.
- Backup/recovery filenames use timestamps plus random identifiers to avoid collisions.
- Desktop import uses native file pickers and bounds selected text at 5,000,000 characters.
- Restore supports storage-provider files that do not expose a normal local path by creating a temporary local picker copy and deleting it after use on a best-effort basis.
- Desktop error/status presentation sanitizes email-shaped/long-number-shaped text and caps displayed diagnostic messages at 2,000 characters.
- `ContactDraftViewModel` now retains a deep copy of the complete loaded contact and overlays compact-editor changes onto that aggregate before save.
- Editing the visible first phone/email preserves its existing child ID, label, and field kind; clearing it removes only that first value while keeping remaining values.
- Additional phones/emails plus addresses, organizations, groups, and tags now survive ordinary compact edit/save conversion even though those values are not yet directly editable in the main UI.
- Documentation now distinguishes **rich-field preservation** (implemented) from **full rich-field editing controls** (still planned).

### Security and data-safety

- Added ContactCore schema-family identity metadata for safer database/backup recognition.
- Databases with schema versions newer than the running build are rejected rather than treated as downgrade-compatible.
- Optional database encryption remains fail closed: when a database key is requested, `cipher_version` must prove a SQLCipher-compatible provider is active.
- Permanent deletion defaults to confirmation enabled; if required confirmation is unavailable, deletion is blocked.
- Restore requires desktop confirmation.
- Validation messages avoid echoing invalid email/phone values.
- Tests assert database keys are not persisted in normal JSON settings.
- Compact desktop edits no longer rebuild a rich contact from only the visible first phone/email; unexposed child collections are preserved through a deep-copy baseline.
- CSV spreadsheet-formula neutralization is **not** currently implemented; documentation calls this out explicitly so standard quoting is not misrepresented as formula-safety mitigation.

### Testing

- Added/expanded tests for duplicate scoring and merge child-ID safety/self-merge rejection.
- Added CSV/vCard round-trip tests and seeded randomized-Unicode CSV parser robustness coverage.
- Added SQLite aggregate round-trip, cascade-delete, and bulk-write rollback tests.
- Added backup/restore tests for verified restore, invalid backup rejection without replacing active data, legacy schema migration, future schema rejection, and unique backup names.
- Added preferences tests for non-persistence of database key, malformed-JSON safe defaults, and theme normalization.
- Added desktop draft tests for preservation of ID/creation/favorite/archive state and exact ISO birthday parsing.
- Added desktop draft tests for rich-child preservation during compact phone/email edits and clears, including source-aggregate non-mutation.

### Known limitations

- The current desktop editor directly exposes one phone and one email and does not yet provide controls to edit addresses, organizations, groups, tags, or additional phone/email rows. Those hidden values are now preserved across compact edit/save operations but remain read-only from the main editor.
- A newly created in-memory draft already has a generated contact ID, so the current delete surface can still present a permanent-delete confirmation before the contact has actually been persisted. Explicit new/unsaved draft state is planned.
- The main-window **Find duplicates** command reports likely-pair count/highest score; a full interactive duplicate review/merge screen is not yet wired even though merge logic exists in Application.
- CSV is a limited interchange format (first phone/email and selected scalar fields), not a full-fidelity backup.
- vCard support is intentionally focused and not a complete implementation of every property/encoding/parameter.
- The default ordinary SQLite build is not encrypted at rest unless a compatible encryption provider is deliberately integrated.
- Current release workflow artifacts are self-contained/single-file but are not documented as code-signed or notarized.

### Documentation checkpoint

The deep documentation pass intentionally records both completed behavior and unresolved risks. See `docs/README.md` for navigation, `docs/repository-reference.md` for every tracked file, and `what_changed.md` for the continuation branch/PR/check status.
