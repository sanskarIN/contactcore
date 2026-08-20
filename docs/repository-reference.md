# Repository Reference

This is the canonical file-by-file reference for ContactCore **2.0.12**. It documents **all 94 tracked files** present after the 2026-08-20 final release-hardening continuation. Directories such as `bin/`, `obj/`, `TestResults/`, local databases, exports, backups, restore staging files, secrets, and other ignored/generated artifacts are intentionally excluded because they are not tracked repository files.

If a tracked file is added, removed, renamed, or materially repurposed, update this reference in the same change. Temporary addenda used during the audit were folded into this file and removed so this remains the single authoritative inventory.

## 1. Repository root — 18 files

### `.editorconfig`
Repository-wide editor/format conventions used with `dotnet format`. Changes can create broad diffs and should not be mixed casually with feature work.

### `.env.example`
Documents optional `CONTACTCORE_DATA_PATH` and `CONTACTCORE_DATABASE_KEY` environment-variable names without containing a real key. The data path is a directory override; the database key is runtime-only.

### `.gitattributes`
Git text/line-ending handling rules. Keeps platform checkouts consistent.

### `.gitignore`
Ignores build/IDE output plus local databases, WAL/SHM files, backups, exports, restore/temp artifacts, environment secrets, signing-key material, and other private/generated files covered by the current policy.

### `CHANGELOG.md`
Release/change history. Contains the dated **2.0.12** release-preparation section covering the full editor, shared group/tag dictionary reassignment semantics, parser hardening, stale-safe duplicate merge/data-safety fixes, tests, version/release automation, documentation, security boundaries, and known limitations. Its post-checkpoint `Unreleased` section records the patched SQLite provider dependency, current GitHub Actions major-version refresh, PR reconciliation, and the exact-head verification requirement.

### `CODE_OF_CONDUCT.md`
Community behavior/enforcement policy, including privacy-conscious handling of accidentally shared sensitive data.

### `CONTRIBUTING.md`
Contributor entry point for branch, quality, testing, documentation, privacy, and review expectations. Deeper engineering rules are in `docs/development.md` and `docs/maintainer-guide.md`.

### `ContactCore.slnx`
Solution containing four production projects and four corresponding test projects. Repository restore/format/build/test commands operate on this solution.

### `Directory.Build.props`
Shared MSBuild/compiler/analyzer policy: .NET 10 target, modern C#, nullable/implicit usings, warnings-as-errors, deterministic/CI build settings, analyzer configuration, and centralized **2.0.12** project/assembly/file/informational version metadata.

### `Directory.Packages.props`
Central NuGet version management for Avalonia, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, MSTest, test SDK, coverage collector, and related dependencies. The current 2.0.12 release-hardening head uses Microsoft.Data.Sqlite 10.0.11 to avoid the vulnerable SQLitePCLRaw native-bundle line that blocked restore in CI.

### `LICENSE`
MIT license text for ContactCore's repository code. Third-party/native dependency licenses remain separate.

### `PRIVACY.md`
User-facing privacy posture: offline/local-first behavior, storage/export/backup implications, temporary copies, runtime-key handling, and absence of mandatory telemetry/cloud behavior.

### `README.md`
Primary project landing page. Identifies version 2.0.12 and documents the full rich editor, contact-owned versus shared group/tag identity semantics, search/filtering, import/export, stale-safe duplicate review/atomic merge, backup/restore, themes, security boundaries, release targets/package names/checksums, setup, documentation links, support/funding, and current limitations without overclaiming encryption/signing/accessibility.

### `ROADMAP.md`
Separates completed 2.0.12 work from future work. Rich editing, unsaved-draft safety, stale-safe duplicate transactions, rich normalization tests, and release-version/package/checksum hardening are complete; reorder/global taxonomy/undo/deeper failure injection/scale/encryption-provider/signing/manual-audit work remains open.

### `SECURITY.md`
Public vulnerability-reporting and supported-security-policy document. Routes private vulnerabilities away from public issues and complements `docs/security.md`.

### `SUPPORT.md`
Support channels and privacy-safe diagnostic guidance. Covers the full 2.0.12 editor and stale-safe duplicate expectations while discouraging real contact databases, backups, exports, or keys in public reports.

### `global.json`
Pins the stable .NET 10 SDK baseline (`10.0.100`) with latest-compatible feature-band roll-forward and prereleases disabled. Development, CI and the hardened release workflow use this policy.

### `what_changed.md`
Authoritative v2.0.12 continuation/handoff ledger. Records branch/PR reconciliation, version metadata, implemented features/fixes, the SQLite advisory resolution, regression coverage, release pipeline, verification boundary, remaining roadmap, and exact merge/release procedure.

## 2. GitHub configuration — 8 files

### `.github/FUNDING.yml`
GitHub funding configuration for the project's support/funding link.

### `.github/ISSUE_TEMPLATE/bug_report.yml`
Structured privacy-conscious bug-report form. Requests reproducible details, uses v2.0.12 as the current version example, and explicitly discourages real contact data/secrets in public reports.

### `.github/ISSUE_TEMPLATE/feature_request.yml`
Structured feature-request form that asks for use case plus privacy/offline/accessibility/data-compatibility considerations.

### `.github/dependabot.yml`
Dependabot configuration for dependency update proposals. Automated updates still require normal compatibility/security/license review.

### `.github/pull_request_template.md`
PR checklist covering CI/CodeQL, tests, data-safety/aggregate preservation, contact-owned versus shared-dictionary identity semantics, stale duplicate protection, migrations/recovery, privacy, documentation, and review evidence.

### `.github/workflows/ci.yml`
Cross-platform CI matrix for Ubuntu, Windows, and macOS. Restores, verifies formatting, builds Release, runs tests with XPlat coverage, uploads available results, and cancels obsolete same-ref runs. Checkout uses v6 and .NET setup uses v5 on the current release-hardening head.

### `.github/workflows/codeql.yml`
C# CodeQL analysis for relevant pushes/pull requests plus scheduled analysis, with minimal required permissions and concurrency handling. The current workflow uses checkout v6, setup-dotnet v5, and CodeQL v4.

### `.github/workflows/release.yml`
Tag-driven release workflow for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`. Preflight resolves the project version and rejects a mismatched tag; SDK setup uses `global.json`; each target runs tests/publishes and is packaged as ZIP or tar.gz; the final job publishes `SHA256SUMS.txt`; repository write permission is limited to that final release job. Checkout uses v6 and .NET setup uses v5. The workflow does not implement or claim code signing/notarization.

## 3. Documentation — 21 files

### `docs/README.md`
Documentation navigation hub and documentation-quality principles for users, contributors, maintainers, reviewers, and security auditors.

### `docs/accessibility.md`
Implemented keyboard/focus/theme/reduced-motion behavior plus full-editor/duplicate-review accessibility risks, keyboard/screen-reader/scaling scenarios, platform test matrix, and explicit non-certification boundary.

### `docs/adr/0001-modular-monolith.md`
Accepted architecture decision to use a layered modular monolith instead of a coupled single project or distributed-service design.

### `docs/adr/0002-sqlite-persistence.md`
Accepted decision to use local SQLite behind Application abstractions, including migration/aggregate/backup tradeoffs and guardrails.

### `docs/adr/0003-encryption-provider.md`
Accepted decision to keep SQLCipher-compatible provider integration optional while failing closed when a runtime key is requested but cipher support cannot be proven.

### `docs/architecture.md`
Layer/dependency map plus startup/read/write/import/backup/restore flows, full-editor identity invariant, unsaved-draft boundary, atomic duplicate merge flow, platform/error/security/test architecture, and evolution rules.

### `docs/ci-cd.md`
CI/CodeQL behavior plus the 2.0.12 version-gated release workflow, SDK policy, packaging, SHA-256 checksums, least-privilege release permissions, quality gate, dependency automation, and failure diagnosis.

### `docs/data-model.md`
Complete domain-to-SQLite mapping for scalar/repeated fields, shared group/tag dictionaries, per-contact rename-as-reassignment semantics, identity/timestamps, complete-aggregate replacement, stale-safe duplicate merge, indexes, normalization, schema identity, and migrations.

### `docs/desktop-ui.md`
Exact current Avalonia behavior: composition, three-column layout, full repeated-field editor, contact-owned versus shared group/tag identity behavior, persisted/unsaved drafts, search/debounce, filters, permanent delete, stale-safe duplicate review/survivor choice/atomic merge, data tools, picker limits, settings, shortcuts, callbacks, errors, and manual UI verification.

### `docs/development.md`
Contributor engineering rules for project layering, complete aggregate editing, shared dictionary reassignment, persistence-state semantics, stale-safe duplicate merge, SQL/migrations, import, backup/restore, privacy, UI, tests, Git discipline, PRs, and documentation definition of done.

### `docs/import-export.md`
CSV/focused-vCard contracts, supported fields, escaping/parsing, unsupported/duplicate CSV-header handling, formula-prefix warnings, vCard TYPE/escaping behavior, batch validation/atomicity, field-fidelity limits, and safe extension guidance.

### `docs/maintainer-guide.md`
Long-term maintainer invariants and workflows for fields/migrations, contact-owned/shared-dictionary editor identities, unsaved drafts, stale-safe duplicate transactions, backup/restore, encryption, preferences, parsers, performance, accessibility, dependencies, CI/releases, docs, security, and repository hygiene.

### `docs/performance.md`
Current complexity/performance characteristics and non-claims: leading-wildcard search, root-plus-child loading, pairwise duplicate scan, in-memory codec behavior, backup/restore costs, benchmark scenarios, profiling, and optimization priorities.

### `docs/release.md`
Exact 2.0.12 release procedure: source version/tag equality, four RIDs/packages, global.json SDK policy, preflight/publish/release stages, checksum generation, permission model, smoke-test requirements, signing/notarization truth, schema compatibility, failed/partial release handling, and post-release checks.

### `docs/repository-reference.md`
This canonical exhaustive tracked-file inventory. It supersedes/removes the temporary reference/addendum files used during the audit.

### `docs/security.md`
Engineering security/privacy model: assets/trust boundaries, SQL controls, complete-aggregate data-loss boundary, unsaved-draft protection, duplicate heuristic/atomic merge safeguards, backup/restore, fail-closed encryption request, import/parser hardening, diagnostics, dependencies, release and unmitigated threats.

### `docs/setup.md`
Source setup for SDK/clone/restore/build/run/test, paths/environment values, disposable development profiles, platform notes, keyed-SQLite behavior, and IDE/CLI parity.

### `docs/storage-backup-recovery.md`
Local storage, connection policy, schema/migrations, aggregate transaction model, backup verification, staged restore/rollback sequence, recovery artifacts, encryption-provider interactions, failure behavior, and operational recommendations.

### `docs/testing.md`
Concrete v2.0.12 coverage map across all four test projects, including rich Application normalization, shared group/tag reassignment and both stale duplicate-merge directions, quality commands, deterministic/temp-data rules, remaining high-value cases, manual release matrix, CI-only failure diagnosis, and regression workflow.

### `docs/troubleshooting.md`
Safety-first diagnosis for SDK/build/startup, encryption/path/settings/search, rich editor invariants, unsaved drafts, shortcuts, CSV/vCard, backup/restore, permanent delete, duplicate merge, themes, file locks, CI platform differences, and privacy-safe diagnostics.

### `docs/user-guide.md`
End-user guide for full contact editing, unsaved discard, validation/search/favorites/archive, permanent delete, duplicate review/merge, import/export, backup/restore, settings, storage/encryption, privacy habits, keyboard and accessibility behavior.

## 4. Domain production project — 4 files

### `src/ContactCore.Domain/ContactCore.Domain.csproj`
Minimal Domain project definition using repository-wide build settings and no infrastructure/UI dependency.

### `src/ContactCore.Domain/ContactModels.cs`
Core `Contact`, `ContactFieldKind`, phone/email/address/organization/group/tag records, display-name fallback, and `DeepCopy` aggregate behavior.

### `src/ContactCore.Domain/ContactValidation.cs`
Domain validation for practical name/note bounds, email syntax/length, and phone syntax/length, returning field-oriented messages that avoid echoing invalid values.

### `src/ContactCore.Domain/TextNormalizer.cs`
Accent-insensitive/lowercase Unicode search key plus digits-only phone key used by matching, merge, and identity comparisons.

## 5. Application production project — 5 files

### `src/ContactCore.Application/ContactCore.Application.csproj`
Application project definition with the Domain project reference.

### `src/ContactCore.Application/Abstractions.cs`
`ContactQuery`, `IContactRepository`, `IBackupService`, and `IAppPreferences`. The repository contract includes bulk upsert and atomic `MergeAsync`.

### `src/ContactCore.Application/ContactService.cs`
Use-case boundary for initialize/count/search/save/import/duplicate-merge/favorite/archive/delete. Normalizes scalar and all current repeated rich fields, timestamps, validates, performs whole-batch import validation, and delegates persistence/atomic merge to repository abstractions.

### `src/ContactCore.Application/DuplicateDetector.cs`
Duplicate candidate scoring/comparison plus `ContactMerger`. Uses normalized name/email/phone/birthday signals, clamps thresholds, rejects self-merge, structurally de-duplicates richer child data, and assigns fresh IDs to copied secondary contact-owned child records where needed.

### `src/ContactCore.Application/ImportExport.cs`
`ImportResult`, CSV codec, and focused vCard codec. Handles quoting/escaping/warnings/header hardening/formula warnings/TYPE mapping and never persists directly.

## 6. Infrastructure production project — 8 files

### `src/ContactCore.Infrastructure/ContactCore.Infrastructure.csproj`
Infrastructure project definition referencing Domain/Application and centrally versioned SQLite dependency.

### `src/ContactCore.Infrastructure/AppPaths.cs`
Resolves/creates ContactCore data directory and derives database/settings/backups paths, honoring optional directory override/fallback behavior.

### `src/ContactCore.Infrastructure/BackupService.cs`
SQLite-native verified backup plus staged verified restore with pre-restore snapshot, migration, identity/integrity/version checks, sidecar/pool handling, final verification, failed-copy retention, and rollback attempt.

### `src/ContactCore.Infrastructure/DatabaseMigrator.cs`
Schema authority: migration tracking, ordered transactional migrations, relational tables/indexes, schema-family marker, future-schema rejection, and identity validation.

### `src/ContactCore.Infrastructure/JsonAppPreferences.cs`
Local preferences implementation with safe defaults, theme normalization, temp/replacement writes, first-run runtime-key loading, and deliberate non-serialization of the database key.

### `src/ContactCore.Infrastructure/RedactingLog.cs`
Defense-in-depth sanitizer for UI-visible diagnostics: common email/long-number shape redaction plus output-length cap. Not a complete PII classifier.

### `src/ContactCore.Infrastructure/SqliteConnectionFactory.cs`
Central connection policy for paths, access/read-only mode, pooling/cache, foreign keys, busy timeout, runtime key application, and fail-closed cipher-version verification.

### `src/ContactCore.Infrastructure/SqliteContactRepository.cs`
Concrete repository: count/get/search/delete, filters/literal wildcard escaping, aggregate load, transactional single/bulk upsert, contact-owned child/link replacement, shared group/tag insertion/linking, and atomic duplicate merge. Shared group/tag values are resolved by case-insensitive name, allowing safe per-contact reassignment to a new dictionary identity. Duplicate merge requires both the chosen primary/survivor and secondary records to still exist, preventing stale-primary resurrection and rolling back when either reviewed record disappeared.

## 7. Desktop production project — 14 files

### `src/ContactCore.Desktop/App.axaml`
Avalonia application resource root/theme/style inclusion.

### `src/ContactCore.Desktop/App.axaml.cs`
Desktop composition root. Creates paths/preferences/factory/migrator/repository/services/view model, applies theme, assigns main window, and initializes the application.

### `src/ContactCore.Desktop/Assets/logo.svg`
Tracked ContactCore vector logo used by README/project presentation and available to the desktop project.

### `src/ContactCore.Desktop/ConfirmDialog.axaml`
Modal owner-centered destructive-action confirmation dialog visual tree.

### `src/ContactCore.Desktop/ConfirmDialog.axaml.cs`
Confirmation dialog code-behind returning nullable Boolean result; only explicit `true` is treated as confirmation by callers.

### `src/ContactCore.Desktop/ContactCore.Desktop.csproj`
Avalonia executable project definition, resources and project/package references.

### `src/ContactCore.Desktop/DataSafetyCommands.cs`
Partial `MainWindowViewModel` commands for persisted contact deletion, unsaved-draft discard, restore confirmation/execution, and picker-temp cleanup.

### `src/ContactCore.Desktop/DuplicateCommands.cs`
Partial `MainWindowViewModel` command for the reverse duplicate-merge direction, allowing the user to keep the second candidate explicitly.

### `src/ContactCore.Desktop/MainWindow.axaml`
Primary desktop visual tree: search/navigation/list, full scalar/repeated-field editor, exact group/tag row UI, Settings, Data Tools, duplicate candidate/comparison/survivor controls, and status/footer.

### `src/ContactCore.Desktop/MainWindow.axaml.cs`
Platform adapter for import/export/backup pickers, bounded UTF-8 text reads, stream-backed backup temp copies, confirmation dialogs, delegate wiring/unwiring, and keyboard shortcuts including editor-only `Ctrl+S`.

### `src/ContactCore.Desktop/Program.cs`
Desktop process entry point configuring and starting Avalonia application lifetime.

### `src/ContactCore.Desktop/RichFieldViewModels.cs`
Editable row models for phone, email, address, organization, group and tag plus duplicate-pair preview model. Group/tag draft rows retain `OriginalName` so true per-contact renames can become new shared-dictionary assignments while normalization-equivalent edits keep the canonical existing identity/name.

### `src/ContactCore.Desktop/Styles/DesignSystem.axaml`
Shared desktop visual styles for surfaces, cards, labels, buttons, avatar/logo, status/muted text, alphabet controls, and visible focus behavior using theme resources.

### `src/ContactCore.Desktop/ViewModels.cs`
Core list/draft/main view-model implementation. Preserves root identity and contact-owned child IDs, retains unchanged group/tag shared identities, converts true group/tag renames to fresh dictionary identities, preserves complete aggregate state, tracks explicit persistence state, handles rich add/remove commands, search/filter/debounce, editor-only save guard, import/export/backup/settings, duplicate review/first-direction merge, and status handling.

## 8. Domain tests — 2 files

### `tests/ContactCore.Domain.Tests/ContactCore.Domain.Tests.csproj`
Domain MSTest project definition/reference.

### `tests/ContactCore.Domain.Tests/ContactValidationTests.cs`
Validation/normalization/domain-model regression tests, including valid/invalid fields, non-echoing messages, Unicode search normalization, display/deep-copy/phone-key behavior represented by the current suite.

## 9. Application tests — 5 files

### `tests/ContactCore.Application.Tests/ContactCore.Application.Tests.csproj`
Application MSTest project definition/reference.

### `tests/ContactCore.Application.Tests/ContactServiceTests.cs`
Fake-repository tests for scalar/phone/email normalization, full address/organization/group/tag normalization, timestamping, whole-batch import validation-before-write, indexed issue fields, deep-copy import behavior, one bulk call/shared timestamp, and trimmed search forwarding.

### `tests/ContactCore.Application.Tests/DuplicateDetectorTests.cs`
Duplicate scoring and merge tests for normalized signals, duplicate phone suppression, copied contact-owned child ID safety, and self-merge rejection.

### `tests/ContactCore.Application.Tests/ImportExportHardeningTests.cs`
Regression tests for unsupported/duplicate CSV headers, spreadsheet-formula-prefix warnings, supported escaped vCard round trips, common TYPE mapping, and invalid birthday warning privacy.

### `tests/ContactCore.Application.Tests/ImportExportTests.cs`
Baseline CSV/vCard round-trip tests plus deterministic randomized Unicode/malformed parser robustness coverage.

## 10. Infrastructure tests — 7 files

### `tests/ContactCore.Infrastructure.Tests/ContactCore.Infrastructure.Tests.csproj`
Infrastructure MSTest project definition/reference.

### `tests/ContactCore.Infrastructure.Tests/AppPathsTests.cs`
Environment/fallback path resolution/derivation tests using controlled disposable paths.

### `tests/ContactCore.Infrastructure.Tests/BackupServiceTests.cs`
Backup/restore safety tests: verified restore, retained pre-restore state, missing/self source guards, invalid/unrelated SQLite rejection, schema-family tampering, legacy migration, future schema rejection, and unique backup names.

### `tests/ContactCore.Infrastructure.Tests/JsonAppPreferencesTests.cs`
Preferences regression tests for runtime-key non-persistence/first-run handling, malformed JSON safe defaults, theme and safety preference behavior, and temp-write semantics represented by the suite.

### `tests/ContactCore.Infrastructure.Tests/RedactingLogTests.cs`
Diagnostic sanitizer tests for likely email/long-number redaction and output-length boundaries.

### `tests/ContactCore.Infrastructure.Tests/SqliteMergeTests.cs`
Atomic duplicate persistence tests: successful survivor update/secondary deletion, rollback when the secondary record is missing, and rejection/non-resurrection when the reviewed primary disappeared while preserving the secondary record.

### `tests/ContactCore.Infrastructure.Tests/SqliteRepositoryTests.cs`
Repository tests for base/rich aggregate round trip/replacement, shared group/tag reassignment after rename, favorites, literal `%`/`_`/backslash search, tag/group/StartsWith filters, cascade delete, and whole-batch rollback.

## 11. Desktop tests — 2 files

### `tests/ContactCore.Desktop.Tests/ContactCore.Desktop.Tests.csproj`
Desktop MSTest project definition/reference to the desktop production project.

### `tests/ContactCore.Desktop.Tests/ContactDraftViewModelTests.cs`
Non-visual editor regression tests: root identity/timestamps/flags, persisted versus unsaved state, exact birthday parsing, contact-owned phone/email/address/organization ID preservation/editing/removal, unchanged group/tag shared identity preservation, true rename-to-new-dictionary-identity behavior, case-only canonical identity/name preservation, delimiter-containing group/tag names, label-only address preservation, blank-row suppression, and source-aggregate non-mutation.

## Inventory totals

| Area | Tracked files |
|---|---:|
| Root | 18 |
| `.github` | 8 |
| `docs` | 21 |
| Domain source | 4 |
| Application source | 5 |
| Infrastructure source | 8 |
| Desktop source | 14 |
| Domain tests | 2 |
| Application tests | 5 |
| Infrastructure tests | 7 |
| Desktop tests | 2 |
| **Total** | **94** |

This total intentionally counts files only, not directories. The canonical reference must be regenerated if the tracked tree changes after this checkpoint.
