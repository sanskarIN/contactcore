# ContactCore — v2.0.12 Final Cross-Platform Handoff

## Release checkpoint

ContactCore **2.0.12** is being finalized through the repository's single authoritative integration path.

- Repository: `sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Integration base: `3900063bcdc2f7f0834118abc2580e030f133d73`
- Authoritative branch: `audit/contactcore-20260819`
- Authoritative pull request: **PR #4**
- Code checkpoint immediately before this handoff update: `da035101211b320f680a57b86fff76368eeb663e`
- Version: **2.0.12**
- Intended tag after verified merge: **`v2.0.12`**
- Stack: C# / .NET 10 / Avalonia 12.1.1 / SQLite on native targets / IndexedDB in Browser
- License: MIT
- Product posture: private, local-first, cross-platform contact manager
- Project credit: **Made by the Sanskar**

Older overlapping integration attempts remain superseded. PR #4 is the intended v2.0.12 merge path.

## Platforms and architecture

The project now has deliberate source/build paths for:

| Platform | Target/runtime | Persistence | Verification/release posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | core CI + ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | ZIP release |
| Linux x64 | `linux-x64` | SQLite | core CI + tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | tar.gz release |
| macOS Intel | `osx-x64` | SQLite | core CI + tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | tar.gz release |
| Android | `net10.0-android`, `android-arm64` CI RID | SQLite | dedicated workload/Release build gate |
| iPhone/iPad | `net10.0-ios`, `iossimulator-arm64` CI RID | SQLite | dedicated macOS workload/Release build gate |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated WASM build + browser ZIP |
| ChromeOS | Browser route; Android where supported | IndexedDB/SQLite by route | no false separate native ChromeOS target |

Source/build support is intentionally separated from store signing and certification. The repository does not fabricate Android keystores, Apple signing certificates, provisioning profiles, passwords, private keys, notarization, or store approval.

### Solution layout

`ContactCore.slnx` is the complete cross-platform solution. `ContactCore.Core.slnx` is the workload-free solution used by ordinary three-OS CI and CodeQL.

The product graph contains:

```text
ContactCore.Domain
ContactCore.Application
ContactCore.Infrastructure
ContactCore.UI
ContactCore.Native
ContactCore.Desktop
ContactCore.Android
ContactCore.iOS
ContactCore.Browser
ContactCore.Domain.Tests
ContactCore.Application.Tests
ContactCore.Infrastructure.Tests
ContactCore.Desktop.Tests
```

## Portable and desktop UI

`ContactCore.UI` provides the portable Avalonia single-view application layer used by mobile/browser hosts. `ContactCore.Desktop` retains the richer desktop shell while sharing the same Domain/Application/Infrastructure behavior.

Implemented workflows include:

- contact list/navigation;
- debounced, cancellation-safe free-text search;
- All/Favorites/Archived/A-Z filters;
- rich editing for names, nickname, birthday and notes;
- multiple phones, emails, addresses and organizations;
- groups and tags;
- stable contact and child IDs during ordinary edits;
- safe shared group/tag reassignment behavior;
- explicit unsaved/persisted draft state;
- save/delete/discard workflows;
- duplicate detection with evidence and score;
- explicit survivor selection and confirmation-gated destructive merge;
- CSV/vCard import/export;
- native capability-aware backup/restore;
- theme and safety preferences;
- reduced-motion preference;
- keyboard shortcuts on keyboard-capable hosts;
- storage-provider file picking;
- in-view confirmation for single-view hosts and dialog confirmation on desktop.

### Avalonia release cleanup

The final hardening pass also fixed framework/compiler issues found by real GitHub Actions builds:

- both Avalonia application classes resolve to the correct framework `Application` base;
- portable and desktop file-picker collections are indexed directly rather than using unsupported extension assumptions;
- duplicate/contact wrapper primary-constructor capture warnings are removed from source where practical;
- `ConfirmDialog` now provides the public parameterless constructor required by compiled Avalonia XAML while retaining the message-taking constructor used by the app;
- all obsolete `TextBox.Watermark` properties were migrated to Avalonia 12 `PlaceholderText`;
- no remaining repository occurrence of `Watermark` exists at this checkpoint.

## Native composition

`ContactCore.Native` composes native application services around:

```text
AppPaths
JsonAppPreferences
SqliteConnectionFactory
DatabaseMigrator
SqliteContactRepository
ContactService
BackupService
```

Android and iOS/iPadOS reuse this native composition instead of introducing weaker platform-specific contact models.

## Browser/WebAssembly

The browser target deliberately does **not** reference native SQLite infrastructure. It implements `IContactRepository` through browser-native persistence.

Implemented browser pieces include:

- `Microsoft.NET.Sdk.WebAssembly` + `net10.0-browser`;
- Avalonia.Browser startup;
- .NET `[JSImport]` bridge;
- IndexedDB JavaScript module;
- browser-local preferences;
- static host assets;
- `BrowserContactRepository`.

`BrowserContactRepository`:

- loads a complete local contact snapshot;
- rejects malformed serialized state and duplicate contact IDs;
- preserves rich aggregate fields and IDs;
- implements search/filter behavior behind the existing application contract;
- serializes writes behind a `SemaphoreSlim`;
- snapshots in-memory state before mutation;
- persists a replacement state through IndexedDB;
- restores pre-write state when persistence fails;
- requires both reviewed records for duplicate merge;
- deep-copies values across repository boundaries.

Browser capabilities explicitly report native database backup/encryption as unavailable. CSV/vCard export remains available for portable copies. Cross-tab conflict synchronization remains future work and is not overclaimed.

## Data integrity and safety preserved

The cross-platform integration preserves and extends the hardened 2.0.12 behavior:

- stable root/contact-owned identities;
- contact validation;
- exact delimiter-containing group/tag names;
- safe group/tag rename-as-reassignment;
- race-safe search;
- literal SQLite wildcard handling;
- validated batch import;
- transactional native persistence;
- hardened CSV/vCard parsing;
- dedicated spreadsheet-safe CSV export mode while ordinary CSV remains lossless;
- duplicate evidence/survivor selection;
- stale-safe duplicate merge;
- verified SQLite-native backups;
- restore staging, schema identity checks, pre-restore recovery snapshot and rollback path;
- runtime-only database key handling;
- fail-closed requested cipher verification;
- destructive-action confirmation safeguards;
- path/error privacy hardening.

## Analyzer, compiler and test hardening

The final runner-driven hardening pass fixed defects rather than weakening the production quality gate.

- `TreatWarningsAsErrors` remains enabled globally.
- `AnalysisLevel` remains `latest-recommended`.
- test-only naming/micro-optimization rules are scoped to `tests/**/*.cs`.
- Avalonia binding-specific analyzer exceptions are narrowly scoped instead of disabled globally.
- JSON preferences reuse a static `JsonSerializerOptions` instance.
- date/string transformations that require stable interchange behavior use invariant handling.
- MSTest v4 attribute/API changes were reconciled.
- infrastructure internals are exposed only to `ContactCore.Infrastructure.Tests` through `InternalsVisibleTo`; the production migration API was not widened merely for tests.
- redundant imports and formatter failures identified by CI were removed.

## Dependency posture

Central package management currently pins the release line to:

```text
Avalonia                  12.1.1
Avalonia.Android          12.1.1
Avalonia.Browser          12.1.1
Avalonia.Desktop          12.1.1
Avalonia.iOS              12.1.1
Avalonia.Themes.Fluent    12.1.1
CommunityToolkit.Mvvm      8.4.2
Microsoft.Data.Sqlite     10.0.11
Microsoft.NET.Test.Sdk    18.8.1
MSTest                     4.3.3
coverlet.collector        10.0.1
```

The final dependency review on 2026-08-20 confirmed the principal runtime/framework/test packages are on current stable release lines. Microsoft.Data.Sqlite 10.0.11 replaced the earlier dependency path that resolved a vulnerable SQLitePCLRaw line.

## CI

`.github/workflows/ci.yml` now runs six independent gates with `fail-fast: false` where appropriate.

### Core matrix

Ubuntu, Windows and macOS each run:

```text
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

Test results are retained per OS.

### Browser

```text
install wasm-tools
restore ContactCore.Browser
Release build ContactCore.Browser
```

### Android

```text
install android workload
restore ContactCore.Android -r android-arm64
Release build ContactCore.Android -r android-arm64
```

### iOS/iPadOS

```text
install ios workload
restore ContactCore.iOS -r iossimulator-arm64
Release build ContactCore.iOS -r iossimulator-arm64
```

The explicit mobile RIDs prevent the previous host-RID leak where Android incorrectly inherited `linux-x64`.

CI keeps read-only repository permission and uses same-PR concurrency cancellation so obsolete attempts cannot become the merge signal.

## CodeQL

CodeQL uses the current C# action major and builds the workload-free core solution. Android/iOS/Browser compilation remains independently enforced by CI.

## GitHub Actions runtime modernization

The workflow action stack was updated during this hardening pass:

```text
actions/checkout@v6
actions/setup-dotnet@v5
github/codeql-action/*@v4
actions/upload-artifact@v7
actions/download-artifact@v8
softprops/action-gh-release@v3
```

This removes the obsolete Node 20-era artifact/release action majors observed in earlier runner output.

## Release workflow

The tag-driven workflow now provides:

### Preflight

- setup using `global.json`;
- project `Version` resolution;
- exact rejection unless the tag is `v<Version>`.

### Desktop publish matrix

```text
win-x64
win-arm64
linux-x64
linux-arm64
osx-x64
osx-arm64
```

Windows artifacts are ZIP archives. Linux/macOS artifacts are tar.gz archives.

### Browser package

The WebAssembly publish output is packaged as a ZIP.

### Mobile release gate

Android and iOS use the same explicit platform RIDs as CI. A tag cannot publish the final GitHub release unless the mobile Release-build gates succeed.

### Final release

The final job:

- downloads verified build artifacts;
- creates `SHA256SUMS.txt`;
- receives the only workflow `contents: write` permission;
- creates the GitHub release with generated release notes and packaged assets.

No artifact is falsely described as signed, notarized, store-certified, or production-mobile-signed.

## Repository inventory

The earlier cross-platform handoff documented **124 tracked files**. The final hardening pass adds one intentional test-visibility file:

```text
src/ContactCore.Infrastructure/Properties/AssemblyInfo.cs
```

It contains only `InternalsVisibleTo("ContactCore.Infrastructure.Tests")` so tests can verify internal migration state without widening the production API.

Current canonical inventory: **125 tracked files**.

Relative to the earlier 94-file checkpoint, the branch therefore contains **31 added tracked files** and no deletion introduced by this continuation.

## Documentation state

The repository includes and synchronizes:

- `README.md`;
- `CHANGELOG.md`;
- `ROADMAP.md`;
- `PRIVACY.md`;
- `SECURITY.md`;
- `SUPPORT.md`;
- `CONTRIBUTING.md`;
- `docs/README.md`;
- `docs/platform-support.md`;
- `docs/setup.md`;
- `docs/development.md`;
- `docs/architecture.md`;
- `docs/data-model.md`;
- `docs/desktop-ui.md`;
- `docs/user-guide.md`;
- `docs/import-export.md`;
- `docs/storage-backup-recovery.md`;
- `docs/security.md`;
- `docs/accessibility.md`;
- `docs/performance.md`;
- `docs/testing.md`;
- `docs/troubleshooting.md`;
- `docs/ci-cd.md`;
- `docs/release.md`;
- `docs/maintainer-guide.md`;
- `docs/repository-reference.md`;
- ADRs under `docs/adr/`;
- this handoff.

## Verification history and current boundary

The final hardening was driven by actual GitHub Actions output. Earlier runs exposed and led to fixes for:

- vulnerable SQLite dependency resolution;
- format/analyzer errors;
- incorrect Avalonia application type resolution;
- Android host RID leakage;
- portable/desktop primary-constructor capture diagnostics;
- unsupported file-picker LINQ assumptions;
- JSON serializer options allocation warning;
- stale MSTest usage;
- test access to internal migration helpers;
- missing Avalonia compiled-XAML dialog constructor;
- obsolete Avalonia `Watermark` properties;
- obsolete artifact/release action runtime majors.

The coding environment used for the GitHub edits does not provide a trusted local .NET workload/toolchain capable of substituting for the platform matrix, so local success is not invented. **GitHub Actions CI + CodeQL on the latest PR #4 merge candidate remain the authoritative gate.**

Required merge conditions:

- core build/test — Ubuntu: success;
- core build/test — Windows: success;
- core build/test — macOS: success;
- Browser Release build: success;
- Android Release build: success;
- iOS/iPadOS simulator Release build: success;
- CodeQL: success/no unresolved newly introduced actionable result;
- results must correspond to the latest PR merge candidate, not a cancelled/superseded run.

PR CI checks GitHub's synthetic merge of the PR head with `main`, so it validates the actual merge candidate even though raw commit ancestry shows the feature branch diverged before the current `main` merge commit.

## Remaining non-blocking roadmap

These items are intentionally future work rather than falsely completed 2.0.12 claims:

- drag/reorder UX for repeated fields;
- global group/tag taxonomy cleanup;
- general undo UX;
- deeper restore failure injection;
- automated real-IndexedDB browser test harness;
- browser cross-tab conflict resolution;
- broader device lifecycle/accessibility automation;
- high-scale benchmarks and duplicate-candidate optimization;
- production SQLCipher + OS secret-store integration;
- Windows/macOS signing/notarization;
- Android production signing/store publishing;
- Apple signing/provisioning/App Store publishing;
- additional installer/package-manager/store formats.

## Merge and release procedure

1. Keep PR #4 as the authoritative integration path.
2. Run CI and CodeQL on the latest PR merge candidate.
3. Fix any actionable failure with a small, reviewable commit.
4. Repeat exact-candidate verification after any code/workflow/documentation change that changes the PR head.
5. Merge PR #4 only after all required gates are green.
6. Confirm `main` contains 2.0.12 metadata after merge.
7. Create `v2.0.12` only from the intended verified merged commit.
8. Confirm six desktop archives, the browser WASM ZIP, mobile build gates and `SHA256SUMS.txt` in the tag workflow.
9. Do not claim Android/iOS store packages until a real secure signing/provisioning pipeline produces them.

## Final posture

ContactCore 2.0.12 now has a deliberate cross-platform architecture and release-quality source/build path for **Windows, Linux, macOS, Android, iPhone, iPad and modern WebAssembly-capable browsers**, with ChromeOS covered through supported browser/Android routes.

Native targets use SQLite; Browser uses IndexedDB. Platform capability differences are explicit rather than hidden behind inaccurate claims. Production analyzers remain strict, mobile runtime targets are explicit, release actions are current, and the repository is ready for final exact-head CI/CodeQL verification before merge.
