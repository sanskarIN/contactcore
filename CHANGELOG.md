# Changelog

All notable changes to ContactCore are documented here. The project follows Semantic Versioning for published releases.

## [Unreleased]

### 2026-08-25 ContactCore 2.1.0 preparation

- Advanced source, assembly, file, and informational version metadata to **2.1.0** on the dedicated next-version branch while leaving the still-verifying 2.0.12 release candidate unchanged.
- Added explicit move-up/move-down commands for phones, emails, addresses, organizations, groups, and tags in both the portable `ContactCore.UI` draft and the separate mature `ContactCore.Desktop` draft implementation.
- Added visible keyboard/touch-operable reorder controls to both Avalonia editor surfaces while preserving existing remove controls and row identities.
- Defined repeated-field sequence as user-significant for all six repeated categories.
- Added native SQLite schema version 3 with zero-based `position` columns for phones, emails, addresses, organizations, contact-group links, and contact-tag links; existing v1/v2 rows are backfilled per contact and `(contact_id, position)` indexes support deterministic ordered reloads.
- Updated `SqliteContactRepository` to write current aggregate order on every complete child/link replacement and to reload repeated rows by persisted position rather than incidental SQLite row order.
- Confirmed Browser persistence already preserves repeated-field order through ordered `List<T>` serialization/deserialization without requiring a relational position column.
- Added portable UI, Desktop, native SQLite round-trip, and v2-to-v3 migration regression tests covering reorder boundaries, all repeated categories, identity preservation, saved order, durable reload order, schema advancement, and position backfill.
- Removed hard-coded About-version literals from portable and Desktop view models; both now derive displayed product version from built assembly metadata, with regression coverage on the portable UI path.
- Updated centrally managed `Microsoft.NET.Test.Sdk` from 18.8.1 to 18.9.0 on the 2.1.0 line.
- Added `docs/next-version-2.1.0.md` and synchronized `ROADMAP.md`, `docs/data-model.md`, PR #17 metadata, and `what_changed.md` with the next-version boundary.
- PR #17 remains a draft based on the exact PR #4 source head so its review delta is isolated. Repository PR CI/CodeQL is intentionally not claimed until v2.0.12 merges and PR #17 is retargeted to `main` for normal exact-head validation.

### 2026-08-24 final release-gate hardening

- Tightened country-code phone equivalence so suffix matching requires at least a ten-digit local representation; this prevents a different nine-digit suffix from becoming a destructive false-positive duplicate while retaining exact normalized equality and conservative country-code matching.
- Sealed `BrowserJsonContext` to satisfy the latest-recommended analyzer policy without suppressing `CA1852`.
- Added `JsonAppPreferencesContext` and moved native preferences to source-generated `System.Text.Json` metadata, eliminating reflection-dependent serializer discovery from mobile/AOT builds while keeping database keys runtime-only.
- Converted the shared portable `MainView` to explicit typed compiled bindings, including item templates, removing application-owned reflection-binding trim diagnostics from Browser/iOS release compilation.
- Scoped iOS simulator targets to `TrimMode=copy` after application-owned trim hazards were removed. The public simulator gate verifies source/runtime integration without pretending to validate the separate signed/device/App Store trimming pipeline or fabricating Apple signing credentials.
- Exact code-head verification before documentation synchronization passed core restore/format/build/tests on Ubuntu, Windows, and macOS; Browser/WebAssembly Release build; Android `android-arm64` Release build; and CodeQL. The iOS simulator attempt was still running when the subsequent documentation commits intentionally superseded it, so only the final post-documentation exact-head run may be used as the merge signal.
- Regenerated `docs/repository-reference.md` to the current **132 tracked files** and synchronized CI/release/handoff documentation with the final AOT/mobile boundary.

### Cross-platform continuation for 2.0.12 integration

- Added `ContactCore.UI`, a portable Avalonia single-view presentation layer containing the shared application host, responsive contact shell, full rich editor, search/filter workflows, duplicate review/merge, import/export, settings, and capability-aware destructive/data tools.
- Added `ContactCore.Native`, a small native composition layer that reuses the existing hardened `AppPaths`, preferences, SQLite connection/migration/repository, `ContactService`, and verified `BackupService` for mobile heads.
- Added first-class `net10.0-android` application target with `AvaloniaAndroidApplication<App>`, `AvaloniaMainActivity`, shared responsive UI, and native SQLite persistence.
- Added first-class `net10.0-ios` application target for iPhone/iPad with `AvaloniaAppDelegate<App>`, UIKit entry point, device-family/orientation metadata, shared responsive UI, and native SQLite persistence.
- Added first-class `net10.0-browser` WebAssembly target with Avalonia.Browser, browser startup assets, .NET/JavaScript interop, IndexedDB contact persistence, and browser-local preferences.
- Added `BrowserContactRepository` behind the existing `IContactRepository` contract. Browser writes are serialized, merge operations stale-check both records, and the previous in-memory snapshot is restored when IndexedDB persistence fails.
- Browser SQLite-native backup/restore and native database-encryption claims are explicitly disabled by platform capability; CSV/vCard export remains the portable-copy path.
- Added `ContactCore.Core.slnx` so three-OS core restore/format/build/test and CodeQL remain workload-free while `ContactCore.slnx` remains the complete solution containing every platform head.
- CI has dedicated WebAssembly (`wasm-tools`), Android, and iOS workload/build jobs in addition to the Ubuntu/Windows/macOS core matrix.
- CodeQL restores/builds `ContactCore.Core.slnx` instead of requiring mobile/WebAssembly workloads.
- Release automation publishes Windows x64/ARM64, Linux x64/ARM64, macOS Intel/Apple Silicon, and browser WebAssembly packages; Android/iOS Release builds are mandatory release gates.
- Android/iOS store/device signing remains deliberately external to the public repository; no private keystore, certificate, provisioning profile, or signing secret is committed or fabricated.
- Added `docs/platform-support.md` and synchronized README/setup/architecture/CI/release documentation with native SQLite vs browser IndexedDB behavior, workload commands, ChromeOS routes, and signing/validation boundaries.
- Added `docs/release-smoke-test.md`, a repeatable exact-SHA manual verification record covering automated gates, fictional fixtures, release artifacts/checksums, desktop/browser/mobile smoke matrices, data-safety/accessibility/privacy checks, deviations, and release sign-off.
- Regenerated `docs/repository-reference.md` through the current **132 tracked files**, including release-hardening, portable-UI-test, release-smoke-record, and native-preferences source-generation additions.

### Release hardening after the 2.0.12 preparation checkpoint

- Updated `Microsoft.Data.Sqlite` from 10.0.10 to 10.0.11 so restore resolves a patched SQLitePCLRaw native bundle instead of vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 flagged by GitHub Actions as a high-severity advisory.
- Updated `actions/checkout` to v6 and `actions/setup-dotnet` to v5 across CI/release workflows, and updated CodeQL actions to v4, while preserving concurrency policy, SDK policy, packaging, and least-privilege release permissions.
- PR #12 was closed without merge after its confirmed dependency fix was transferred to the authoritative v2.0.12 PR #4; the stronger PR #4 implementation remains the only intended integration path.
- Exact-head CI and CodeQL remain the merge gate; no green result is claimed until those workflows complete successfully on the final PR #4 head.

### 2026-08-23 exact-head CI repair and resilience expansion

- Fixed international duplicate-phone comparison so formatting that differs only by a plausible one-to-three-digit country calling code can match conservatively while `PhoneKey` remains a lossless digits-only normalization primitive; the August 24 boundary now requires at least ten digits on the shorter representation.
- Applied the same phone-equivalence rule to duplicate scoring and contact merge de-duplication, with regression tests for accepted country-code variants and short/mismatched non-equivalences.
- Added the centrally managed `coverlet.collector` reference to Application and Infrastructure tests so all five current test projects support the shared `--collect:"XPlat Code Coverage"` CI command.
- Added `BrowserJsonContext.cs` with source-generated `System.Text.Json` metadata and moved browser contact/preferences persistence to AOT/trimming-safe serializer overloads instead of suppressing `IL2026`.
- Made `BrowserContactRepository` dispose its owned `SemaphoreSlim`, resolving the WebAssembly analyzer resource-lifetime failure.
- Explicitly selects `/Applications/Xcode_26.0.app/Contents/Developer` in both normal iOS CI and the tag-release iOS gate so the .NET iOS workload is not broken by a newer rolling-runner default Xcode.
- Added a dedicated `ContactCore.UI.Tests` project to both solution files with XPlat coverage enabled.
- Added deterministic portable view-model search tests proving rapid input is debounced to the latest query and that a newer query cancels an already-running stale search before stale results can replace the visible list.
- Added portable destructive-action/restore tests proving permanent deletion is confirmation-gated by default, cancellation preserves the contact, the explicit no-confirmation preference works, and restore cannot invoke the backup service until confirmation.
- Added an internal-only post-switch restore verification probe and regression coverage proving that a final restore failure rolls the active database back to the verified pre-restore snapshot, retains the switched-in failed copy, and cleans staging temp files.
- Added a repeatable release smoke-test record and integrated it into release documentation so manual evidence must identify the exact tested SHA/environment instead of becoming an untracked ad hoc checklist.
- Regenerated the canonical repository inventory and synchronized testing, storage/recovery, release, CI, roadmap, README, changelog, and handoff documentation with the current state.

## [2.0.12] - 2026-08-19

### Added

- Cross-platform Avalonia desktop shell for Windows, macOS, and Linux.
- Layered Domain/Application/Infrastructure/Desktop solution plus behavioral test projects for Domain, Application, Infrastructure, portable UI, and Desktop workflows.
- Rich contact model for names, birthday, notes, favorite/archive state, multiple phones/emails/addresses/organizations, groups, and tags.
- Full desktop editing for all repeated contact collections in the current model, including add/edit/remove controls and stable contact-owned child identities.
- Independent group/tag rows so names containing commas or semicolons round-trip exactly.
- Explicit persisted-versus-unsaved draft state.
- SQLite contact persistence with ordered schema migrations, foreign keys, indexes, transactional aggregate updates, literal wildcard handling, and case-insensitive group/tag identities.
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
- Regression tests across the core/data/UI layers, including rich-editor identity/data behavior, shared group/tag reassignment, atomic duplicate merge races, parser hardening, paths/preferences/redaction, repository rich-field/query behavior, backup/restore safety, portable search cancellation, and confirmation-gated destructive actions.

### Versioning

- Centralized the source version at **2.0.12** in `Directory.Build.props`.
- Set `Version`/`VersionPrefix` to `2.0.12`, `AssemblyVersion`/`FileVersion` to `2.0.12.0`, and `InformationalVersion` to `2.0.12`.
- Release automation reads the built project version and refuses mismatched tags such as attempting to publish `v2.0.13` from 2.0.12 source metadata.
- Reduced release-job permissions: build/publish jobs use read-only repository access and only the final GitHub Release job receives `contents: write`.

### Changed

- `IContactRepository` includes `UpsertManyAsync` for batch writes and `MergeAsync` for survivor-update/secondary-delete atomicity.
- `ContactService.ImportAsync` normalizes and validates the complete batch before persistence and prefixes validation fields with the imported contact index.
- `ContactService.MergeAsync` reloads both records, uses `ContactMerger`, normalizes/validates the survivor aggregate, and delegates the destructive write to the repository transaction.
- `SqliteContactRepository.MergeAsync` requires both reviewed records to still exist, writes the complete survivor aggregate, and deletes the secondary contact in one transaction; a missing primary or secondary cancels/rolls back the operation.
- Contact-service normalization covers addresses, organizations, groups, and tags in addition to scalar/phone/email fields.
- User search text is trimmed and SQLite `LIKE` wildcard characters `%`, `_`, and backslash are escaped as literals.
- Duplicate matching/merge logic has null/self-merge safeguards, threshold clamping, normalized comparison, conservative country-code-aware phone equivalence, structural de-duplication for richer child records, and fresh IDs for copied secondary contact-owned child rows.
- The editor preserves original contact ID, creation timestamp, and IDs of contact-owned repeated rows that remain.
- Unchanged group/tag assignments retain their shared dictionary identities; a true per-contact group/tag rename becomes reassignment to a new dictionary identity, while case-only/normalization-equivalent edits keep the original canonical identity/name.
- Blank newly added rich rows are ignored; legacy label-only addresses remain preservable.
- Case-insensitive duplicate group/tag rows are collapsed at draft conversion while the first applicable identity is retained.
- Unsaved **Delete / discard** discards locally rather than flowing through database deletion/confirmation.
- `Ctrl+S` is restricted to the visible contact editor so Settings/Data Tools/Duplicate Review cannot accidentally save a stale draft.
- Preferences use temp-file replacement; malformed JSON falls back to safe defaults; native preference serialization now uses generated JSON metadata for mobile/AOT safety.
- Runtime database key loading occurs even when no settings file exists yet and is excluded from serialized `settings.json`.
- Restore verifies selected input before active data changes, stages/migrates/verifies before replacement, retains a verified recovery snapshot, verifies after the switch, and attempts rollback on final verification failure.
- Backup/recovery filenames include timestamp plus random identity to avoid collisions.
- Desktop import is bounded at 5,000,000 characters and supports storage-provider portability for backup picker inputs.
- Desktop error/status presentation sanitizes likely PII patterns and caps diagnostic output.
- Browser serialization uses generated JSON metadata rather than reflection-dependent runtime discovery in trimmed WebAssembly builds.
- Portable shared UI uses typed compiled XAML bindings to remove reflection-binding dependencies from trimmed Browser/mobile application code.
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
- Fixed duplicate phone suppression when otherwise identical local numbers differ only by a plausible country-code prefix, while preventing nine-digit suffix overmatching.
- Fixed Browser/WebAssembly trimming failures caused by reflection-based JSON serializer discovery and non-compiled shared bindings.
- Fixed iOS CI/release failures caused by the runner selecting an Xcode version newer than the installed .NET iOS workload accepts; the simulator gate also now has an explicit simulator-only trim boundary.

### Security and data safety

- Added ContactCore schema-family identity metadata for safer database/backup recognition.
- Databases with schema versions newer than the running build are rejected.
- Optional database encryption remains fail closed: when a database key is requested, `cipher_version` must prove a SQLCipher-compatible provider is active.
- Database keys are runtime-only and not stored in normal preferences.
- Permanent deletion defaults to confirmation enabled; when required confirmation is unavailable, deletion is blocked.
- Restore always requires desktop/portable confirmation before the backup service is invoked.
- Duplicate merge always requires confirmation independent of permanent-delete preference.
- Duplicate merge is transactional across survivor update and secondary deletion and rejects stale review state when either record vanished.
- Batch import is validated before one-transaction persistence.
- Validation/parser messages avoid intentionally echoing invalid private values.
- CSV spreadsheet-formula neutralization is **not** claimed; formula-like text is preserved and warning/documentation make the boundary explicit.
- Release workflow permissions follow least privilege until the release-creation step.

### Testing

- Duplicate scoring/merge tests cover child-ID safety, self-merge rejection, international country-code-equivalent phone handling, and protection from shorter suffix overmatching.
- Atomic SQLite merge tests cover normal merge, missing-secondary rollback, and missing-primary non-resurrection while preserving the secondary record.
- CSV/vCard tests cover round-trip behavior, malformed/randomized text boundaries, unsupported/duplicate CSV headers, formula-prefix warnings, escaped vCard names/notes, common TYPE mapping, and non-echoing birthday warnings.
- SQLite tests cover aggregate round-trip, cascade deletion, bulk rollback, rich child persistence/query behavior, shared group/tag reassignment, and merge transactions.
- Backup/restore tests cover verified restore, invalid input protection, schema migration/version boundaries, identity checks, unique backup naming, and deterministic post-switch rollback with failed-copy retention/staging cleanup.
- Preferences tests cover runtime key non-persistence, first-run key behavior, malformed JSON defaults, and theme normalization.
- Desktop draft tests cover root identity/timestamps/flags, persisted state, exact birthday parsing, complete repeated-field editing, contact-owned ID preservation, shared group/tag rename identity rules, removal semantics, delimiter-containing group/tag names, label-only address preservation, blank-row suppression, and source-aggregate non-mutation.
- Portable UI tests cover search debounce/cancellation ordering and destructive-delete/restore confirmation state.
- Contact-service tests cover scalar/phone/email and address/organization/group/tag normalization plus whole-batch import validation/indexing.
- Path/redaction tests cover environment-path handling and sanitization boundaries.
- Every current test project references the centrally pinned coverage collector for the shared three-OS XPlat coverage command.

### Known limitations

- Repeated rich-field rows now support explicit move-up/move-down ordering in the 2.1.0 feature line; pointer drag/drop, reorder announcements, and representative accessibility validation remain future work.
- Groups/tags are editable per contact; there is no dedicated global group/tag taxonomy-management UI or global rename/cleanup workflow yet.
- Orphaned shared group/tag dictionary rows can remain after the last relationship is removed; ordinary per-contact editing does not silently delete them.
- Duplicate review uses an in-memory pairwise candidate scan; large address books may require indexed candidate generation before high-scale use.
- Duplicate merge has confirmation and storage-safety protections but no general-purpose undo stack.
- CSV remains a limited interchange format (first phone/email and selected scalar fields), not a full-fidelity backup.
- CSV formula-like content is preserved rather than spreadsheet-neutralized.
- vCard support remains a focused subset rather than a complete implementation of every property/encoding/parameter.
- The default ordinary SQLite native build is not encrypted at rest unless a compatible cipher provider is deliberately integrated.
- Browser persistence is browser-profile/origin-managed and can be removed by site-data clearing, private-session teardown, policy, or storage eviction; it is not represented as a native SQLite backup model.
- Browser repository persistence still needs an automated real-IndexedDB harness and cross-tab conflict handling before stronger multi-tab claims.
- Explicit native cleanup-operation failure injection beyond the tested post-switch rollback branch remains future resilience work.
- Android/iOS build support does not equal Play Store/App Store signing/certification; the iOS simulator trim policy is not production device trimming verification.
- Release artifacts are not documented as code-signed or notarized.
- Manual accessibility, screen-reader, high-DPI, native-picker, phone/tablet lifecycle/orientation, and representative browser verification remains required before stronger conformance claims.

### Documentation checkpoint

The 2.0.12 release documentation remains frozen around the authoritative PR #4 release candidate and its 132-file canonical inventory. The active next-version continuation is tracked separately on draft PR #17 through `docs/next-version-2.1.0.md`, `ROADMAP.md`, `docs/data-model.md`, this Unreleased section, and `what_changed.md`. The 2.1.0 line must be retargeted to `main` and pass exact-head CI + CodeQL after v2.0.12 merges before it can be called release-ready.
