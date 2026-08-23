# Testing

ContactCore uses MSTest for behavioral Domain/Application/Infrastructure/portable-UI/Desktop coverage and GitHub Actions build gates for the Android, iOS/iPadOS, and WebAssembly heads. This separation is intentional: core tests prove rules, data safety, and view-model workflow behavior, while platform jobs prove each application target restores/compiles with its required workload.

## Test stack

Central versions live in `Directory.Packages.props`:

- `Microsoft.NET.Test.Sdk`;
- `MSTest`;
- `coverlet.collector`.

ContactCore is version **2.0.12** and treats build warnings as errors. Every test project references `coverlet.collector`, so the shared CI coverage command is available consistently across the test suite.

## Solution choice

Use `ContactCore.Core.slnx` for ordinary quality verification. It contains shared Domain/Application/Infrastructure/UI/native-composition/Desktop code plus all five current test projects without forcing Android/iOS/WebAssembly workloads onto every machine.

`ContactCore.slnx` is the complete solution and additionally contains Android, iOS, and Browser application heads.

## Core quality sequence

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
```

CI executes this sequence on Ubuntu, Windows, and macOS.

## Platform build gates

### Browser / WebAssembly

```bash
dotnet workload install wasm-tools
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

The CI browser job is the compile gate for `net10.0-browser`, Avalonia.Browser, `[JSImport]` declarations, source-generated browser JSON metadata, the shared UI, and browser repository code.

### Android

```bash
dotnet workload install android
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj -r android-arm64
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release -r android-arm64 --no-restore
```

### iOS/iPadOS

On the GitHub macOS runner the workflow first selects the Xcode 26.0 installation accepted by the current .NET iOS workload, then builds the explicit simulator RID:

```bash
sudo xcode-select -s /Applications/Xcode_26.0.app/Contents/Developer
xcodebuild -version
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj -r iossimulator-arm64
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release -r iossimulator-arm64 --no-restore
```

A successful build gate is not a substitute for representative device/simulator/browser testing or store signing/certification.

## Domain tests

Project: `tests/ContactCore.Domain.Tests`

Coverage includes representative validation, non-echoing invalid values, Unicode/accent search normalization, display-name/deep-copy behavior, digits-only phone-key normalization, and conservative country-code-aware phone equivalence boundaries.

## Application tests

Project: `tests/ContactCore.Application.Tests`

### `ContactServiceTests.cs`

Coverage includes:

- scalar/phone/email normalization;
- rich address/organization/group/tag normalization;
- save timestamps;
- whole-batch import validation before write;
- indexed import validation fields;
- deep-copy import behavior;
- one normalized bulk call/shared import timestamp;
- trimmed search forwarding while preserving filters.

### `DuplicateDetectorTests.cs`

Coverage includes normalized duplicate signals, country-code-aware phone duplicate signals/suppression, safe fresh IDs for contact-owned children copied from a secondary contact, and self-merge rejection.

### `ImportExportTests.cs`

Covers CSV/vCard supported round trips and deterministic randomized Unicode/malformed parser robustness.

### `ImportExportHardeningTests.cs`

Covers unsupported/duplicate CSV headers, formula-prefix warnings, escaped vCard delimiters/newlines/backslashes, common `TYPE` mapping, and birthday warning privacy.

## Infrastructure tests

Project: `tests/ContactCore.Infrastructure.Tests`

All storage tests use disposable paths rather than a normal user profile.

### `SqliteRepositoryTests.cs`

Covers rich aggregate round trip/replacement, shared group/tag reassignment, favorites, literal wildcard search, tag/group/A-Z filters, cascade delete, and batch rollback.

### `SqliteMergeTests.cs`

Covers successful survivor update/secondary delete, missing-secondary rollback, and missing-primary rejection/non-resurrection while preserving the remaining record.

### `BackupServiceTests.cs`

Covers verified restore, pre-restore snapshot retention, missing/self-source rejection, invalid/unrelated SQLite rejection, schema-family tampering, older schema migration, future schema rejection, unique backup names, and deterministic post-switch rollback behavior.

The post-switch rollback test uses the production-internal/test-visible `BackupService` probe to inject a failure after the staged database has replaced the active file but before final verification succeeds. It proves that:

- the verified pre-restore snapshot is copied back to the active path;
- the switched-in failed copy is retained under `failed-restore-*.db`;
- the retained failed copy contains the selected restore state;
- staging `.restore-*.tmp` files are cleaned by the `finally` path.

The probe is internal and normal production construction leaves it disabled. This creates deterministic evidence for a destructive recovery branch without widening `IBackupService` or making the public API test-shaped.

High-value future failure injection still includes explicit cleanup-operation failures such as inability to delete/move/copy staging/recovery files, rather than only failures inside the final verification zone.

### `JsonAppPreferencesTests.cs`

Protect runtime-key non-persistence, first-run key handling, malformed JSON safe defaults, and theme/safety preferences.

### `AppPathsTests.cs`

Protect configured/fallback native data-path behavior.

### `RedactingLogTests.cs`

Protect likely email/long-number redaction and diagnostic-length bounds.

## Portable UI tests

Project: `tests/ContactCore.UI.Tests`

The portable UI tests intentionally exercise view models and application contracts without requiring a visual window or platform runtime.

### `MainViewModelSearchTests.cs`

Protects the asynchronous search boundary:

- rapid search edits are debounced into the latest query rather than issuing every intermediate term;
- an already-running stale search receives cancellation when a newer term arrives;
- stale results cannot replace the current query's visible contact list.

The test repository exposes an explicit slow-search synchronization point so the cancellation regression is deterministic instead of depending only on arbitrary sleeps.

### `MainViewModelConfirmationTests.cs`

Protects high-impact confirmation behavior:

- permanent delete is deferred while confirmation is enabled;
- cancelling a pending delete preserves the contact;
- disabling the delete-confirmation preference permits the documented direct-delete path;
- restore selection queues confirmation before `IBackupService.RestoreBackupAsync` can run;
- confirming restore invokes exactly the reviewed backup path and returns the view model to its successful status.

### `TestDoubles.cs`

Provides isolated in-memory repository, backup, preference, and platform-service doubles. They use fictional data only and do not touch a real ContactCore database, browser origin, backup, or user profile.

High-value portable-UI additions that remain future work include settings/theme callback tests, picker-cancellation/status tests, and stable responsive Avalonia integration tests.

## Desktop tests

Project: `tests/ContactCore.Desktop.Tests`

`ContactDraftViewModelTests.cs` protects:

- root ID/creation timestamp/flags;
- persisted vs unsaved state;
- exact birthday parsing;
- phone/email/address/organization ID preservation;
- unchanged group/tag shared identity;
- true rename to new dictionary identity;
- case-only canonical identity/name preservation;
- delimiter-containing group/tag names;
- row removal/blank suppression;
- label-only legacy address preservation;
- source aggregate non-mutation.

These are deliberately non-visual tests.

## Browser persistence test posture

`BrowserContactRepository` is build-gated and its JSON serialization path is compile-time source-generated, but the repository still does not contain an automated real-IndexedDB browser harness. Manual browser verification for release should use a disposable origin/profile and check:

- first load with no data;
- create/edit/reload persistence;
- rich child-field preservation;
- search/favorite/archive/A-Z behavior;
- stale-safe duplicate merge;
- import/export picker paths;
- preferences across reload;
- behavior when browser persistent storage is unavailable/blocked;
- correct absence of native SQLite backup/restore UI.

Future automated work should use an isolated browser environment rather than mocking away the storage boundary entirely.

## Mobile verification posture

Android/iOS CI currently proves project/workload compilation. Before user-facing mobile distribution, perform manual or automated device/simulator checks for:

- startup/resume/background lifecycle;
- SQLite data persistence;
- touch layout and scrolling on small screens/tablets;
- software/hardware keyboard input;
- orientation changes;
- file-picker import/export availability;
- destructive confirmation usability;
- theme/contrast/accessibility labels/focus where applicable;
- app upgrade/data migration with fictional disposable profiles.

Android/iOS store signing is release engineering, not a unit-test assertion.

Use `release-smoke-test.md` to record the exact candidate SHA, platform/device/browser details, skipped checks, and evidence from manual release verification.

## Temporary-data hygiene

Infrastructure/native tests should:

1. create unique paths under `Path.GetTempPath()`;
2. use fictional data only;
3. clear SQLite pools before cleanup when necessary;
4. delete temporary paths while tolerating reasonable OS file-lock timing differences.

Browser tests should use a dedicated test origin/profile/database name and clear only disposable test data.

Portable UI tests should use in-memory or explicitly fake services and never point at the native or browser production stores.

Never point tests at real ContactCore user data.

## Determinism

Tests should not depend on:

- local time zone;
- real contacts;
- network services;
- machine-specific paths;
- execution order;
- changing random seeds without recording them;
- real signing credentials;
- a developer's normal browser profile.

Use fixed randomness for ordinary parser robustness; reserve broad fuzzing for dedicated jobs/tooling. Asynchronous view-model tests should prefer explicit synchronization signals and bounded polling over large timing assumptions. Destructive recovery failure paths should prefer narrow internal fault injection over timing-dependent races when a test seam can remain outside public contracts.

## Coverage policy

CI collects XPlat Code Coverage for all five current test projects, but the project intentionally avoids a fixed percentage promise. Prioritize branches where regression could cause data loss, partial writes, unsafe restore, stale destructive merge, secret leakage, malformed-input crashes, stale asynchronous UI state, or misleading platform behavior.

## Manual verification matrix

Automated coverage does not replace release checks for:

### Desktop

- first launch on representative Windows/Linux/macOS architectures;
- rich edit/search/filter/duplicates;
- native file pickers;
- verified backup/restore using fictional data;
- keyboard/focus/theme/high-DPI/accessibility behavior.

### Browser

- static host boot over HTTP(S);
- IndexedDB persistence/reload;
- import/export;
- blocked/cleared storage behavior;
- responsive layout and accessibility on representative browsers.

### Android

- representative phone/tablet/emulator lifecycle, touch, storage, orientation, file pickers, accessibility.

### iOS/iPadOS

- representative iPhone/iPad/simulator lifecycle, touch, storage, orientation, file pickers, accessibility and required Apple toolchain behavior.

Document exactly what was tested; avoid a blanket “tested everywhere” claim.

## CI-only failure checklist

Compare:

- target workload installation;
- SDK resolution via `global.json`;
- OS-specific path/file-lock/case sensitivity;
- native SQLite libraries;
- Avalonia platform packages;
- Android SDK/JDK/toolchain;
- Apple/Xcode/iOS workload/toolchain;
- WebAssembly SDK and `[JSImport]` source generation;
- JSON source-generation/trimming diagnostics for Browser;
- XAML parser/bindings/resources;
- generated MVVM source;
- JavaScript host assets/runtime config.

Treat a platform-only failure as a real compatibility signal until understood; do not delete the failing gate simply to obtain a green badge.

## Regression workflow

For a bug fix:

1. identify/create the smallest test or platform reproduction for the bad behavior;
2. use fictional/disposable fixtures;
3. implement at the correct layer;
4. run the focused test/build;
5. run the core quality sequence;
6. run affected platform build(s);
7. update docs when behavior/limitations changed;
8. retain the regression unless replaced by stronger coverage.
