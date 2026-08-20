# Repository Reference

This is the canonical file-by-file reference for ContactCore **2.0.12**. It documents **all 124 tracked files** present after the 2026-08-20 cross-platform continuation. Directories such as `bin/`, `obj/`, `TestResults/`, local databases, browser runtime data, exports, backups, restore staging files, secrets, signing material, and other ignored/generated artifacts are intentionally excluded because they are not tracked repository files.

The previous 94-file checkpoint predates the shared UI/native composition projects and Android/iOS/Browser heads. This reference supersedes that count. If a tracked file is added, removed, renamed, or materially repurposed, update this reference in the same change.

## 1. Repository root — 19 files

### `.editorconfig`
Repository-wide editor/format conventions used with `dotnet format`.

### `.env.example`
Documents optional native `CONTACTCORE_DATA_PATH` and `CONTACTCORE_DATABASE_KEY` names without containing a real key.

### `.gitattributes`
Git text/line-ending handling rules for consistent cross-platform checkouts.

### `.gitignore`
Ignores build/IDE output, local databases/WAL/SHM, backups/exports/temp restore artifacts, environment secrets, signing-key material, and other generated/private files.

### `CHANGELOG.md`
Release/change history for 2.0.12 and Unreleased hardening/cross-platform work.

### `CODE_OF_CONDUCT.md`
Community behavior/enforcement policy, including privacy-conscious handling of accidentally shared sensitive data.

### `CONTRIBUTING.md`
Contributor entry point for branch, quality, testing, documentation, privacy, and review expectations.

### `ContactCore.Core.slnx`
Workload-free core verification solution. Contains Domain, Application, Infrastructure, shared UI, native composition, Desktop, and the four existing test projects. CI/CodeQL use it so ordinary runners do not need Android/iOS/WebAssembly workloads.

### `ContactCore.slnx`
Complete repository solution containing Domain, Application, Infrastructure, shared UI, native composition, Desktop, Android, iOS, Browser, and all existing test projects.

### `Directory.Build.props`
Shared MSBuild/compiler/analyzer policy: .NET 10 baseline for ordinary projects, modern C#, nullable/implicit usings, warnings-as-errors, deterministic/CI settings, and centralized 2.0.12 version metadata. Platform heads override `TargetFramework` where required.

### `Directory.Packages.props`
Central NuGet version management for Avalonia core/Desktop/Android/iOS/Browser/themes, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite 10.0.11, MSTest, test SDK, and coverage collector.

### `LICENSE`
MIT license text for repository code; third-party dependency licenses remain separate.

### `PRIVACY.md`
User-facing privacy posture for local-first behavior, native storage/export/backup, runtime-key handling, and absence of mandatory telemetry/cloud behavior.

### `README.md`
Primary project landing page. Documents the cross-platform matrix, native SQLite/browser IndexedDB split, rich contact behavior, build commands, release targets, signing boundaries, privacy/security, and documentation links.

### `ROADMAP.md`
Separates completed 2.0.12/cross-platform work from future UX, resilience, performance, signing, packaging, and manual validation work.

### `SECURITY.md`
Public vulnerability-reporting and supported-security-policy document.

### `SUPPORT.md`
Support channels and privacy-safe diagnostic guidance that discourages public sharing of real contact databases, exports, backups, secrets, or signing material.

### `global.json`
Pins stable .NET SDK baseline `10.0.100` with latest-compatible feature-band roll-forward and prereleases disabled.

### `what_changed.md`
Authoritative continuation/handoff ledger for branch/PR reconciliation, versioning, implementation, release hardening, cross-platform expansion, verification, and remaining boundaries.

## 2. GitHub configuration — 8 files

### `.github/FUNDING.yml`
GitHub funding configuration.

### `.github/ISSUE_TEMPLATE/bug_report.yml`
Structured privacy-conscious bug-report form.

### `.github/ISSUE_TEMPLATE/feature_request.yml`
Feature-request form that asks for use case plus privacy/offline/accessibility/data-compatibility considerations.

### `.github/dependabot.yml`
Dependabot configuration for configured dependency ecosystems.

### `.github/pull_request_template.md`
PR checklist covering CI/CodeQL, tests, data safety, identity semantics, migrations/recovery, privacy, documentation, and review evidence.

### `.github/workflows/ci.yml`
Workload-aware cross-platform CI. `ContactCore.Core.slnx` restores/formats/builds/tests on Ubuntu, Windows, and macOS. Dedicated jobs install `wasm-tools`, Android, and iOS workloads and build the Browser, Android, and iOS heads respectively.

### `.github/workflows/codeql.yml`
C# CodeQL workflow using checkout v6, setup-dotnet v5, CodeQL v4, and the workload-free `ContactCore.Core.slnx` so security analysis does not depend on mobile workloads.

### `.github/workflows/release.yml`
Tag-driven 2.0.12 release workflow. Enforces tag/source-version equality; publishes six desktop RIDs plus browser WebAssembly ZIP; build-gates Android/iOS; generates SHA-256 checksums; limits repository write permission to final release creation; does not claim store signing/notarization.

## 3. Documentation — 22 files

### `docs/README.md`
Documentation hub/index and documentation-quality principles, including explicit native/browser and build/signing boundaries.

### `docs/accessibility.md`
Keyboard/focus/theme/reduced-motion behavior, editor/duplicate-review accessibility risks, platform test scenarios, and non-certification boundary.

### `docs/adr/0001-modular-monolith.md`
Accepted decision for layered modular-monolith architecture.

### `docs/adr/0002-sqlite-persistence.md`
Accepted native SQLite persistence decision and associated migration/aggregate/backup guardrails.

### `docs/adr/0003-encryption-provider.md`
Accepted optional SQLCipher-compatible provider boundary with fail-closed requested-key behavior.

### `docs/architecture.md`
Cross-platform project/dependency map; desktop/mobile/browser startup; native SQLite/browser IndexedDB flows; shared UI; persistence, identity, import, duplicate, backup, security, and evolution rules.

### `docs/ci-cd.md`
Three-OS core CI, platform workload jobs, CodeQL, six-RID desktop release matrix, WebAssembly publish, mobile build gate, permissions, checksums, and troubleshooting.

### `docs/data-model.md`
Domain-to-SQLite mapping, scalar/repeated fields, shared group/tag identities, timestamps, complete aggregate replacement, duplicate merge, indexes, normalization, schema identity, and migrations.

### `docs/desktop-ui.md`
Mature desktop Avalonia behavior: desktop composition/layout, rich editor, duplicate review, native pickers/dialogs, settings, shortcuts, and manual verification.

### `docs/development.md`
Contributor engineering rules for layering, complete aggregates, persistence identity/state, SQL/migrations, imports, backup/restore, privacy, UI, tests, Git discipline, PRs, and documentation.

### `docs/import-export.md`
CSV/focused-vCard contracts, supported fields, escaping/parsing, warnings, batch validation, fidelity limits, and privacy/safe-extension guidance.

### `docs/maintainer-guide.md`
Maintainer invariants/workflows for fields/migrations, identities, drafts, duplicates, backup/restore, encryption, preferences, parsers, performance, accessibility, dependencies, CI/releases, docs, and security.

### `docs/performance.md`
Current complexity/performance characteristics, non-claims, benchmark scenarios, profiling, and optimization priorities.

### `docs/platform-support.md`
Canonical platform matrix for Windows/Linux/macOS architectures, Android, iPhone/iPad, Browser/WebAssembly, and ChromeOS routes. Documents persistence models, workload commands, CI coverage, store-signing boundaries, and meaning of cross-platform support.

### `docs/release.md`
2.0.12 tag/version preflight, six desktop packages, browser package, mobile build gate, signing/provisioning boundaries, verification/smoke tests, failures/rollback, and post-release process.

### `docs/repository-reference.md`
This canonical 124-file inventory.

### `docs/security.md`
Engineering threat model and controls for native SQL, aggregate data-loss boundaries, draft/duplicate/backup safeguards, encryption requests, parsers, diagnostics, dependencies, and release risk.

### `docs/setup.md`
Cross-platform source setup. Explains `ContactCore.Core.slnx` vs complete solution, desktop commands, Android/iOS/WebAssembly workloads, browser storage, native paths/keys, and safe disposable development profiles.

### `docs/storage-backup-recovery.md`
Native local storage, SQLite connection/schema/migration/transaction policy, backup verification, staged restore/rollback, recovery artifacts, encryption interactions, and operational recommendations.

### `docs/testing.md`
Behavioral test-project coverage, deterministic/temp-data rules, quality commands, CI-only diagnosis, manual release matrix, and regression workflow.

### `docs/troubleshooting.md`
Safety-first diagnosis for SDK/build/startup, paths/settings/search, editor invariants, imports, backup/restore, duplicate merge, themes, file locks, CI differences, and privacy-safe diagnostics.

### `docs/user-guide.md`
End-user guide for contact editing, validation/search/favorites/archive, delete, duplicate review/merge, import/export, native backup/restore, settings, storage/privacy, keyboard and accessibility behavior.

## 4. Domain production project — 4 files

### `src/ContactCore.Domain/ContactCore.Domain.csproj`
Minimal Domain project definition using repository-wide build settings and no infrastructure/UI dependency.

### `src/ContactCore.Domain/ContactModels.cs`
Core `Contact`, `ContactFieldKind`, phone/email/address/organization/group/tag records, display-name fallback, and aggregate `DeepCopy` behavior.

### `src/ContactCore.Domain/ContactValidation.cs`
Domain validation for practical field bounds/syntax with field-oriented messages that avoid echoing invalid values.

### `src/ContactCore.Domain/TextNormalizer.cs`
Accent-insensitive/lowercase Unicode search key plus digits-only phone key used by matching, merge, and identity comparisons.

## 5. Application production project — 5 files

### `src/ContactCore.Application/ContactCore.Application.csproj`
Application project definition referencing Domain.

### `src/ContactCore.Application/Abstractions.cs`
`ContactQuery`, `IContactRepository`, `IBackupService`, and `IAppPreferences`; repository contract includes bulk upsert and stale-safe merge operation.

### `src/ContactCore.Application/ContactService.cs`
Use-case boundary for initialize/count/search/save/import/merge/favorite/archive/delete. Normalizes/timestamps/validates and delegates persistence to abstractions.

### `src/ContactCore.Application/DuplicateDetector.cs`
Duplicate candidate scoring/comparison plus `ContactMerger`; normalizes signals, rejects self-merge, deduplicates rich child data, and gives copied contact-owned rows fresh IDs where needed.

### `src/ContactCore.Application/ImportExport.cs`
`ImportResult`, CSV codec, and focused vCard codec with escaping, warnings, header hardening, formula-prefix warnings, TYPE mapping, and no direct persistence.

## 6. Infrastructure production project — 8 files

### `src/ContactCore.Infrastructure/ContactCore.Infrastructure.csproj`
Native Infrastructure project referencing Domain/Application and centrally versioned SQLite dependency.

### `src/ContactCore.Infrastructure/AppPaths.cs`
Resolves/creates native ContactCore data directory and derives database/settings/backups paths.

### `src/ContactCore.Infrastructure/BackupService.cs`
SQLite-native verified backup plus staged verified restore with pre-restore snapshot, migration, identity/integrity/version checks, sidecar/pool handling, failed-copy retention, and rollback attempt.

### `src/ContactCore.Infrastructure/DatabaseMigrator.cs`
Native SQLite schema authority: migration tracking, relational tables/indexes, schema-family marker, and future-schema rejection.

### `src/ContactCore.Infrastructure/JsonAppPreferences.cs`
Native preferences with safe defaults, theme normalization, replacement writes, first-run runtime-key loading, and deliberate non-serialization of database key.

### `src/ContactCore.Infrastructure/RedactingLog.cs`
Defense-in-depth sanitizer for UI-visible diagnostics.

### `src/ContactCore.Infrastructure/SqliteConnectionFactory.cs`
Central native SQLite connection policy for path/mode/pooling/cache, foreign keys, busy timeout, optional runtime key, and fail-closed cipher verification.

### `src/ContactCore.Infrastructure/SqliteContactRepository.cs`
Concrete native repository for query/filter/load/delete, transactional aggregate upsert/bulk import, shared group/tag linking, literal wildcard escaping, and atomic stale-safe duplicate merge.

## 7. Desktop production project — 14 files

### `src/ContactCore.Desktop/App.axaml`
Desktop Avalonia application resources/theme/style inclusion.

### `src/ContactCore.Desktop/App.axaml.cs`
Mature desktop composition root creating native paths/preferences/SQLite/services/view model/window and initializing the application.

### `src/ContactCore.Desktop/Assets/logo.svg`
Tracked ContactCore vector logo used by README/project presentation and desktop assets.

### `src/ContactCore.Desktop/ConfirmDialog.axaml`
Modal owner-centered destructive-action confirmation dialog visual tree.

### `src/ContactCore.Desktop/ConfirmDialog.axaml.cs`
Dialog code-behind returning nullable Boolean result; only explicit `true` confirms.

### `src/ContactCore.Desktop/ContactCore.Desktop.csproj`
Desktop Avalonia executable project definition and project/package references.

### `src/ContactCore.Desktop/DataSafetyCommands.cs`
Partial desktop main-view-model commands for persisted delete, unsaved discard, restore confirmation/execution, and picker-temp cleanup.

### `src/ContactCore.Desktop/DuplicateCommands.cs`
Partial desktop command for reverse duplicate survivor direction.

### `src/ContactCore.Desktop/MainWindow.axaml`
Primary mature desktop visual tree: search/navigation/list, full rich editor, Settings, Data Tools, duplicate comparison/survivor controls, and status/footer.

### `src/ContactCore.Desktop/MainWindow.axaml.cs`
Desktop platform adapter for import/export/backup pickers, bounded text reads, stream-backed backup temp copies, confirmation dialogs, callback wiring, and keyboard shortcuts.

### `src/ContactCore.Desktop/Program.cs`
Desktop process entry point configuring Avalonia classic desktop lifetime.

### `src/ContactCore.Desktop/RichFieldViewModels.cs`
Desktop editable rich row models plus duplicate preview model and group/tag original-name identity support.

### `src/ContactCore.Desktop/Styles/DesignSystem.axaml`
Desktop visual styles for surfaces/cards/labels/buttons/avatar/status/alphabet/focus behavior.

### `src/ContactCore.Desktop/ViewModels.cs`
Desktop list/draft/main view-model implementation preserving full aggregate/identity semantics, persistence state, search/debounce, import/export/backup/settings, duplicates, and status handling.

## 8. Shared UI production project — 9 files

### `src/ContactCore.UI/ContactCore.UI.csproj`
Portable Avalonia UI library referencing Application/Domain plus Avalonia core/themes and CommunityToolkit.Mvvm. Compiled bindings are disabled by default for the current portable binding model.

### `src/ContactCore.UI/AppServices.cs`
Defines `AppPlatformCapabilities`, the shared `AppServices` composition record, and `AppBootstrapper` factory used by Android/iOS/Browser heads.

### `src/ContactCore.UI/App.axaml`
Portable Avalonia application resource root with Fluent theme.

### `src/ContactCore.UI/App.axaml.cs`
Portable Avalonia `App`. Supports classic desktop and `ISingleViewApplicationLifetime`, applies theme, constructs shared `MainViewModel/MainView`, and starts initialization.

### `src/ContactCore.UI/RichFieldViewModels.cs`
Portable phone/email/address/organization/group/tag draft view models, contact list item, and duplicate pair preview model.

### `src/ContactCore.UI/ContactDraftViewModel.cs`
Portable full aggregate editor/draft conversion. Preserves root/contact-owned IDs and shared group/tag reassignment semantics; provides rich add/remove commands and birthday parsing.

### `src/ContactCore.UI/MainViewModel.cs`
Portable contact workflow for search/filters/new/edit/save/delete, duplicates, CSV/vCard import/export, capability-aware backup/restore, settings/theme, confirmation overlay, debounce, and safe status messages.

### `src/ContactCore.UI/MainView.axaml`
Responsive single-view visual shell used by phone/tablet/browser heads. Contains contact list, full rich editor, duplicates, data tools, settings/About, horizontal navigation, and in-view destructive-action confirmation.

### `src/ContactCore.UI/MainView.axaml.cs`
Portable Avalonia `StorageProvider` picker integration, bounded import reader, export writer, stream-backed native backup picker handling, delegate wiring, and keyboard shortcuts where applicable.

## 9. Native composition project — 2 files

### `src/ContactCore.Native/ContactCore.Native.csproj`
Small native composition library referencing Application, Infrastructure, and shared UI.

### `src/ContactCore.Native/NativeAppServices.cs`
`NativeAppServiceFactory` that composes `AppPaths`, native preferences, SQLite connection/migrator/repository, `ContactService`, and `BackupService` for Android/iOS shared UI heads.

## 10. Android application project — 3 files

### `src/ContactCore.Android/ContactCore.Android.csproj`
`net10.0-android` executable with application ID/version, Android package format, Avalonia.Android dependency, and shared UI/native composition references.

### `src/ContactCore.Android/Application.cs`
Android `AvaloniaAndroidApplication<App>` host. Configures `AppBootstrapper` with Android native SQLite services.

### `src/ContactCore.Android/MainActivity.cs`
Exported main-launcher `AvaloniaMainActivity` with orientation/screen-size/UI-mode configuration-change handling.

## 11. iOS/iPadOS application project — 4 files

### `src/ContactCore.iOS/ContactCore.iOS.csproj`
`net10.0-ios` executable with application ID/version, minimum OS metadata, Avalonia.iOS dependency, and shared UI/native composition references.

### `src/ContactCore.iOS/AppDelegate.cs`
Registered `AvaloniaAppDelegate<App>` host configuring native iOS/iPadOS services.

### `src/ContactCore.iOS/Main.cs`
UIKit application entry point invoking `UIApplication.Main` with `AppDelegate`.

### `src/ContactCore.iOS/Info.plist`
Application metadata declaring ContactCore ID/version/minimum OS, iPhone+iPad device families, and supported orientations.

## 12. Browser/WebAssembly application project — 10 files

### `src/ContactCore.Browser/ContactCore.Browser.csproj`
`Microsoft.NET.Sdk.WebAssembly` executable targeting `net10.0-browser`, referencing Avalonia.Browser plus shared UI/Application/Domain without native Infrastructure.

### `src/ContactCore.Browser/BrowserStorageInterop.cs`
.NET 10 `[JSImport]` declarations for asynchronous contact load/save and preference load/save through the browser storage module.

### `src/ContactCore.Browser/BrowserContactRepository.cs`
`IContactRepository` browser implementation. Loads full aggregate state, performs local queries, serializes writes behind a gate, stale-checks merges, persists versioned JSON through IndexedDB, and restores prior in-memory state on persistence failure.

### `src/ContactCore.Browser/BrowserAppServices.cs`
Browser composition: `ContactService`, `BrowserContactRepository`, browser preferences, unsupported native-backup adapter, and capability metadata indicating IndexedDB/no native DB backup/encryption.

### `src/ContactCore.Browser/Program.cs`
WebAssembly .NET entry point configuring browser services and starting Avalonia via `StartBrowserAppAsync("out")`.

### `src/ContactCore.Browser/runtimeconfig.template.json`
WebAssembly browser host runtime configuration template.

### `src/ContactCore.Browser/wwwroot/contactcore-storage.js`
JavaScript storage module. Creates/opens IndexedDB, reads/writes the single contact-state record transactionally, and stores preferences in localStorage with session fallback behavior in the .NET layer.

### `src/ContactCore.Browser/wwwroot/main.js`
Loads the ContactCore storage module before starting the .NET WebAssembly runtime, exposes the expected global interop object, creates runtime, and invokes .NET main.

### `src/ContactCore.Browser/wwwroot/index.html`
Static browser host document containing the `out` application container, loading placeholder, stylesheet, and module bootstrap script.

### `src/ContactCore.Browser/wwwroot/app.css`
Browser host/full-viewport/loading-shell CSS around the Avalonia WebAssembly surface.

## 13. Domain tests — 2 files

### `tests/ContactCore.Domain.Tests/ContactCore.Domain.Tests.csproj`
Domain MSTest project definition/reference.

### `tests/ContactCore.Domain.Tests/ContactValidationTests.cs`
Validation/normalization/domain-model regression tests including invalid/valid fields, non-echoing errors, Unicode search normalization, display/deep-copy/phone-key behavior.

## 14. Application tests — 5 files

### `tests/ContactCore.Application.Tests/ContactCore.Application.Tests.csproj`
Application MSTest project definition/reference.

### `tests/ContactCore.Application.Tests/ContactServiceTests.cs`
Fake-repository tests for scalar/rich normalization, timestamping, batch import validation-before-write, deep-copy behavior, shared timestamp, and query forwarding.

### `tests/ContactCore.Application.Tests/DuplicateDetectorTests.cs`
Duplicate scoring/merge tests for normalized signals, duplicate suppression, copied child-ID safety, and self-merge rejection.

### `tests/ContactCore.Application.Tests/ImportExportHardeningTests.cs`
Regression tests for CSV header/formula hardening, vCard escaping/TYPE mapping, and warning privacy.

### `tests/ContactCore.Application.Tests/ImportExportTests.cs`
Baseline CSV/vCard round-trip tests plus deterministic randomized Unicode/malformed parser robustness coverage.

## 15. Infrastructure tests — 7 files

### `tests/ContactCore.Infrastructure.Tests/ContactCore.Infrastructure.Tests.csproj`
Infrastructure MSTest project definition/reference.

### `tests/ContactCore.Infrastructure.Tests/AppPathsTests.cs`
Environment/fallback path resolution/derivation tests using controlled disposable paths.

### `tests/ContactCore.Infrastructure.Tests/BackupServiceTests.cs`
Backup/restore safety tests covering verified restore, snapshots, missing/self sources, unrelated/tampered SQLite rejection, migration/future-schema behavior, and unique backup names.

### `tests/ContactCore.Infrastructure.Tests/JsonAppPreferencesTests.cs`
Preferences regression tests for runtime-key non-persistence/first-run handling, malformed JSON defaults, themes/safety preferences, and write semantics.

### `tests/ContactCore.Infrastructure.Tests/RedactingLogTests.cs`
Diagnostic sanitizer tests for likely email/long-number redaction and output bounds.

### `tests/ContactCore.Infrastructure.Tests/SqliteMergeTests.cs`
Atomic duplicate persistence tests for success plus stale missing-secondary and missing-primary/non-resurrection rollback behavior.

### `tests/ContactCore.Infrastructure.Tests/SqliteRepositoryTests.cs`
Repository tests for base/rich aggregate round trip/replacement, dictionary reassignment, favorites, literal wildcard search, tag/group/A-Z filters, cascade delete, and batch rollback.

## 16. Desktop tests — 2 files

### `tests/ContactCore.Desktop.Tests/ContactCore.Desktop.Tests.csproj`
Desktop MSTest project referencing the mature desktop production project.

### `tests/ContactCore.Desktop.Tests/ContactDraftViewModelTests.cs`
Non-visual desktop editor regressions for root/timestamp/flags/persistence state, birthday, contact-owned IDs, group/tag identity/reassignment/case behavior, delimiter names, label-only addresses, blank-row suppression, and source non-mutation.

## Inventory totals

| Area | Tracked files |
|---|---:|
| Root | 19 |
| `.github` | 8 |
| `docs` | 22 |
| Domain source | 4 |
| Application source | 5 |
| Infrastructure source | 8 |
| Desktop source | 14 |
| Shared UI source | 9 |
| Native composition source | 2 |
| Android source | 3 |
| iOS/iPadOS source | 4 |
| Browser/WebAssembly source | 10 |
| Domain tests | 2 |
| Application tests | 5 |
| Infrastructure tests | 7 |
| Desktop tests | 2 |
| **Total** | **124** |

This total intentionally counts tracked files only, not directories. It is derived from the previous verified 94-file inventory plus the 30 tracked files added by the cross-platform continuation; no tracked file was deleted in that continuation. Regenerate this inventory whenever the tracked tree changes.
