# Changelog

All notable changes to ContactCore are documented here. The project follows Semantic Versioning for published releases.

## [Unreleased]

### Release hardening after the 2.0.12 preparation checkpoint

- Updated `Microsoft.Data.Sqlite` from 10.0.10 to 10.0.11 so restore resolves a patched SQLitePCLRaw native bundle instead of vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 flagged by GitHub Actions as a high-severity advisory.
- Updated `actions/checkout` to v6 and `actions/setup-dotnet` to v5 across CI/release workflows, and updated CodeQL actions to v4, while preserving the existing cross-platform gates, concurrency policy, SDK policy, packaging, and least-privilege release permissions.
- PR #12 was closed without merge after its confirmed dependency fix was transferred to the authoritative v2.0.12 PR #4; the stronger PR #4 implementation remains the only intended integration path.
- Exact-head CI and CodeQL remain the merge gate; no green result is claimed until those workflows complete successfully on the final PR #4 head.

## [2.0.12] - 2026-08-19

### Added

- Cross-platform Avalonia desktop shell for Windows, macOS, and Linux.
- Layered Domain/Application/Infrastructure/Desktop solution plus one test project per production layer.
- Rich contact model for names, birthday, notes, favorite/archive state, multiple phones/emails/addresses/organizations, groups, and tags.
- Full desktop editing for all repeated contact collections in the current model, including add/edit/remove controls and stable contact-owned child identities.
- Independent group/tag rows so names containing commas or semicolons round-trip exactly.
- Explicit persisted-versus-unsaved draft state.
- SQLite contact persistence with ordered schema migrations, foreign keys, indexes, transactional aggregate updates, literal search wildcard handling, and case-insensitive group/tag identities.
- CSV and focused vCard 4.0 import/export codecs plus desktop picker integration.
- Whole-batch import validation and one-transaction bulk persistence.
- Duplicate scoring plus an interactive duplicate-review screen with confidence, reasons, side-by-side summaries, survivor choice, confirmation, and atomic merge/delete storage.
- SQLite-native backup creation with integrity/version/ContactCore identity verification.
- Staged restore with verified pre-restore recovery snapshots and rollback handling.
- Settings/About/privacy surface with System/Light/Dark theme, reduced-motion preference, and permanent-delete confirmation preference.
- Keyboard shortcuts (`Ctrl+N`, editor-only `Ctrl+S`, `Ctrl+F`, `Esc`) and explicit visible-focus styling.
- Cross-platform GitHub Actions CI definitions, CodeQL, Dependabot, and tag-driven release publishing.
- Version-checked release preflight: release tags must match the project version resolved from `Directory.Build.props`.
- Platform-specific packaged release archives (`.zip` on Windows, `.tar.gz` on Linux/macOS) and generated `SHA256SUMS.txt` checksums.
- Documentation hub plus user/setup/architecture/data model/desktop UI/import-export/storage/security/accessibility/performance/development/testing/CI/release/troubleshooting/maintainer/ADR/repository-reference documentation.
- Regression tests across all four layers, including rich-editor identity/data behavior, shared group/tag reassignment, atomic duplicate merge races, parser hardening, paths/preferences/redaction, repository rich-field/query behavior, and backup/restore safety.

### Versioning

- Centralized the source version at **2.0.12** in `Directory.Build.props`.
- Set `Version`/`VersionPrefix` to `2.0.12`, `AssemblyVersion`/`FileVersion` to `2.0.12.0`, and `InformationalVersion` to `2.0.12`.
- Release automation now reads the built project version and refuses mismatched tags such as attempting to publish `v2.0.13` from 2.0.12 source metadata.
- Reduced release-job permissions: build/publish jobs use read-only repository access and only the final GitHub Release job receives `contents: write`.

### Changed

- `IContactRepository` includes `UpsertManyAsync` for batch writes and `MergeAsync` for survivor-update/secondary-delete atomicity.
- `ContactService.ImportAsync` normalizes and validates the complete batch before persistence and prefixes validation fields with the imported contact index.
- `ContactService.MergeAsync` reloads both records, uses `ContactMerger`, normalizes/validates the survivor aggregate, and delegates the destructive write to the repository transaction.
- `SqliteContactRepository.MergeAsync` requires both reviewed records to still exist, writes the complete survivor aggregate, and deletes the secondary contact in one transaction; a missing primary or secondary cancels/rolls back the operation.
- Contact-service normalization covers addresses, organizations, groups, and tags in addition to scalar/phone/email fields.
- User search text is trimmed and SQLite `LIKE` wildcard characters `%`, `_`, and backslash are escaped as literals.
- Duplicate matching/merge logic has null/self-merge safeguards, threshold clamping, normalized comparison, structural de-duplication for richer child records, and fresh IDs for copied secondary contact-owned child rows.
- The editor preserves original contact ID, creation timestamp, and IDs of contact-owned repeated rows that remain.
- Unchanged group/tag assignments retain their shared dictionary identities; a true per-contact group/tag rename becomes reassignment to a new dictionary identity, while case-only/normalization-equivalent edits keep the original canonical identity/name.
- Blank newly added rich rows are ignored; legacy label-only addresses remain preservable.
- Case-insensitive duplicate group/tag rows are collapsed at draft conversion while the first applicable identity is retained.
- Unsaved **Delete / discard** discards locally rather than flowing through database deletion/confirmation.
- `Ctrl+S` is restricted to the visible contact editor so Settings/Data Tools/Duplicate Review cannot accidentally save a stale draft.
- Preferences use temp-file replacement; malformed JSON falls back to safe defaults.
- Runtime database key loading occurs even when no settings file exists yet and is excluded from serialized `settings.json`.
- Restore verifies selected input before active data changes, stages/migrates/verifies before replacement, retains a verified recovery snapshot, verifies after the switch, and attempts rollback on final verification failure.
- Backup/recovery filenames include timestamp plus random identity to avoid collisions.
- Desktop import is bounded at 5,000,000 characters and supports storage-provider portability for backup picker inputs.
- Desktop error/status presentation sanitizes likely PII patterns and caps diagnostic output.
- README and deep documentation describe the implemented full editor and interactive duplicate merge rather than the retired compact-editor limitation.
- Temporary documentation addenda created during the audit were folded into the canonical guides/reference and removed.

### Import/export hardening

- CSV files with no recognized ContactCore header import zero contacts and return a warning instead of creating meaningless unnamed records.
- Duplicate CSV header names use the first occurrence and return a warning rather than throwing.
- CSV formula-like text is preserved and accompanied by an explicit spreadsheet-safety warning.
- vCard export uses deterministic CRLF line endings.
- vCard structured-name parsing respects escaped semicolons.
- vCard unescaping handles supported backslash/newline/comma/semicolon values character-by-character.
- Common vCard `TYPE` values map to `ContactFieldKind`.
- Nested `BEGIN:VCARD` and unterminated-card cases return controlled warnings.
- Invalid vCard birthday warnings no longer echo the imported value.

### Fixed

- Corrected a compile-time duplicate-merger reference from the nonexistent `OrganizationAffiliation` type to the actual `ContactOrganization` domain type.
- Fixed first-launch database-key handling so `CONTACTCORE_DATABASE_KEY` is not silently ignored when `settings.json` does not yet exist.
- Fixed unsaved new-contact deletion semantics.
- Fixed a keyboard shortcut path that could invoke contact save outside the editor.
- Fixed group/tag delimiter loss by replacing comma/semicolon serialization in the editor with independent rows.
- Fixed per-contact group/tag rename handling so an edited shared dictionary row no longer reuses its old ID with a different name, avoiding SQLite primary-key conflicts and unintended global-rename semantics.
- Fixed blank new address rows so they do not become empty persisted address records while still preserving legacy label-only addresses.
- Fixed stale-primary duplicate merge behavior so a removed chosen survivor cannot be silently recreated from reviewed UI state.

### Security and data safety

- Added ContactCore schema-family identity metadata for safer database/backup recognition.
- Databases with schema versions newer than the running build are rejected.
- Optional database encryption remains fail closed: when a database key is requested, `cipher_version` must prove a SQLCipher-compatible provider is active.
- Database keys are runtime-only and not stored in normal preferences.
- Permanent deletion defaults to confirmation enabled; when required confirmation is unavailable, deletion is blocked.
- Restore always requires desktop confirmation.
- Duplicate merge always requires confirmation independent of permanent-delete preference.
- Duplicate merge is transactional across survivor update and secondary deletion and rejects stale review state when either record vanished.
- Batch import is validated before one-transaction persistence.
- Validation/parser messages avoid intentionally echoing invalid private values.
- CSV spreadsheet-formula neutralization is **not** claimed; formula-like text is preserved and warning/documentation make the boundary explicit.
- Release workflow permissions follow least privilege until the release-creation step.

### Testing

- Duplicate scoring/merge tests cover child-ID safety and self-merge rejection.
- Atomic SQLite merge tests cover normal merge, missing-secondary rollback, and missing-primary non-resurrection while preserving the secondary record.
- CSV/vCard tests cover round-trip behavior, malformed/randomized text boundaries, unsupported/duplicate CSV headers, formula-prefix warnings, escaped vCard names/notes, common TYPE mapping, and non-echoing birthday warnings.
- SQLite tests cover aggregate round-trip, cascade deletion, bulk rollback, rich child persistence/query behavior, shared group/tag reassignment, and merge transactions.
- Backup/restore tests cover verified restore, invalid input protection, schema migration/version boundaries, identity checks, and unique backup naming.
- Preferences tests cover runtime key non-persistence, first-run key behavior, malformed JSON defaults, and theme normalization.
- Desktop draft tests cover root identity/timestamps/flags, persisted state, exact birthday parsing, complete repeated-field editing, contact-owned ID preservation, shared group/tag rename identity rules, removal semantics, delimiter-containing group/tag names, label-only address preservation, blank-row suppression, and source-aggregate non-mutation.
- Contact-service tests cover scalar/phone/email and address/organization/group/tag normalization plus whole-batch import validation/indexing.
- Path/redaction tests cover environment-path handling and sanitization boundaries.

### Known limitations

- Repeated rich-field rows support add/edit/remove but not drag/drop reordering.
- Groups/tags are editable per contact; there is no dedicated global group/tag taxonomy-management UI or global rename/cleanup workflow yet.
- Orphaned shared group/tag dictionary rows can remain after the last relationship is removed; ordinary per-contact editing does not silently delete them.
- Duplicate review uses an in-memory pairwise candidate scan; large address books may require indexed candidate generation before high-scale use.
- Duplicate merge has confirmation and transaction safety but no general-purpose undo stack.
- CSV remains a limited interchange format (first phone/email and selected scalar fields), not a full-fidelity backup.
- CSV formula-like content is preserved rather than spreadsheet-neutralized.
- vCard support remains a focused subset rather than a complete implementation of every property/encoding/parameter.
- The default ordinary SQLite build is not encrypted at rest unless a compatible cipher provider is deliberately integrated.
- Release artifacts are not documented as code-signed or notarized.
- Manual accessibility, screen-reader, high-DPI, native-picker, and platform release verification remains required before conformance claims.

### Documentation checkpoint

The 2.0.12 documentation pass is synchronized with the intended release branch. See `docs/README.md` for navigation, `docs/repository-reference.md` for tracked-file coverage, and `what_changed.md` for the continuation/audit checkpoint and exact verification state.
