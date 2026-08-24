# Platform Support

ContactCore 2.0.12 separates **source/build support**, **automated verification**, and **production distribution/signing**. A platform appearing in the source tree does not automatically mean a signed store package has been produced or manually certified on representative hardware.

## Current matrix

| Platform | Target / runtime | Persistence | Automated verification | Distribution posture |
|---|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | core Windows CI | ZIP release archive; signing/installer future work |
| Windows ARM64 | `win-arm64` | SQLite | release publish matrix | ZIP release archive; signing/installer future work |
| Linux x64 | `linux-x64` | SQLite | core Ubuntu CI | tar.gz release archive |
| Linux ARM64 | `linux-arm64` | SQLite | release publish matrix | tar.gz release archive |
| macOS Intel | `osx-x64` | SQLite | core macOS CI / release publish | tar.gz release archive; Developer ID/notarization future work |
| macOS Apple Silicon | `osx-arm64` | SQLite | core macOS CI / release publish | tar.gz release archive; Developer ID/notarization future work |
| Android | `net10.0-android`, CI RID `android-arm64` | SQLite | dedicated Android workload + Release build | production keystore/Play Store publishing future work |
| iPhone/iPad | `net10.0-ios`, CI RID `iossimulator-arm64` | SQLite | dedicated macOS/Xcode unsigned simulator Release build | device signing/provisioning/App Store publishing future work |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated `wasm-tools` Release build | static WebAssembly ZIP; hosting is separate |
| ChromeOS | Browser route; Android where supported by device | IndexedDB or SQLite by route | covered through Browser/Android heads | no invented separate native ChromeOS binary |

## What “cross-platform support” means here

For ContactCore, a supported source target means:

- a maintained project/head exists in the repository;
- it composes the shared Domain/Application behavior intentionally;
- it has a defined persistence model;
- it has documented build prerequisites and commands;
- it participates in the repository's current verification strategy;
- limitations are stated instead of being hidden.

It does **not** automatically mean:

- every hardware model/OS patch has been manually tested;
- every accessibility combination has been certified;
- a signed installer/store package exists;
- an app-store review has passed;
- browser data has native-backup durability;
- source build success equals production distribution approval.

## Desktop: Windows, Linux, macOS

The mature `ContactCore.Desktop` Avalonia shell remains the primary desktop head. It uses native SQLite Infrastructure and includes desktop-native file pickers/dialogs for import/export/backup workflows.

Release automation currently targets:

```text
win-x64
win-arm64
linux-x64
linux-arm64
osx-x64
osx-arm64
```

Windows output is packaged as ZIP; Linux/macOS output is packaged as tar.gz to retain Unix executable metadata inside the archive.

These archives are not currently claimed as Authenticode-signed, packaged installers, macOS Developer-ID signed/notarized apps, or Linux distribution packages.

## Android

Project:

```text
src/ContactCore.Android/ContactCore.Android.csproj
```

Target:

```text
net10.0-android
```

CI/runtime identifier:

```text
android-arm64
```

Android uses:

- portable `ContactCore.UI`;
- `ContactCore.Native` composition;
- native SQLite persistence;
- Android Avalonia application/activity hosts.

CI installs the Android workload, restores for `android-arm64`, and performs a Release build. This is a source/build gate, not a Play Store production signing pipeline.

Before public store distribution, representative Android device/emulator checks should cover touch layout, text input, local persistence, file-picker behavior, orientation/configuration changes, lifecycle/background/restore behavior, theme/accessibility, and real signing/package installation.

## iPhone and iPad

Project:

```text
src/ContactCore.iOS/ContactCore.iOS.csproj
```

Target:

```text
net10.0-ios
```

CI runtime identifier:

```text
iossimulator-arm64
```

iOS/iPadOS uses:

- portable `ContactCore.UI`;
- `ContactCore.Native` composition;
- native SQLite persistence;
- `AvaloniaAppDelegate<App>` plus UIKit entry point;
- iPhone/iPad device-family/orientation metadata.

### Xcode/toolchain selection

The .NET iOS workload used by the 2.0.12 branch is verified against the Xcode 26.0 line. The GitHub-hosted macOS image can move its default Xcode forward independently, so CI/release gates explicitly select:

```text
/Applications/Xcode_26.0.app/Contents/Developer
```

before installing/building the iOS workload.

### Simulator trim boundary

`ContactCore.iOS.csproj` applies `TrimMode=copy` only for `iossimulator-arm64` and `iossimulator-x64`.

This is intentional. Application-owned trim/AOT hazards are removed in source through:

- source-generated `System.Text.Json` metadata for native preferences;
- typed compiled Avalonia bindings in the portable production `MainView`.

The public CI simulator gate then validates source/runtime integration without treating linker diagnostics emitted by third-party Avalonia/SQLite assemblies as proof that the unsigned simulator build is a production distribution pipeline.

The simulator gate must **not** be described as evidence that a final iPhone/iPad artifact has been device-trimmed, signed, provisioned, App Store validated, or certified. Those claims require a dedicated protected Apple distribution pipeline with real maintainer-controlled credentials and representative device testing.

## Browser/WebAssembly

Project:

```text
src/ContactCore.Browser/ContactCore.Browser.csproj
```

Target:

```text
net10.0-browser
```

Browser is deliberately not composed through native SQLite Infrastructure. It uses:

- Avalonia.Browser;
- portable shared UI;
- `BrowserContactRepository` behind `IContactRepository`;
- IndexedDB for contact-state persistence;
- localStorage-backed preferences with controlled fallback behavior;
- .NET/JavaScript interop through `[JSImport]`;
- source-generated JSON metadata;
- typed compiled shared-UI bindings.

Browser CI installs `wasm-tools` and performs a Release build. Trimming/AOT diagnostics remain meaningful in this target and are not disabled just because the iOS simulator has a different scoped trim policy.

The Browser ZIP is static web content. A maintainer must deploy it to an appropriate HTTP(S) origin/server before users can run it.

## Browser persistence boundary

Browser storage is origin/profile managed. It can be removed by:

- clearing site data;
- private-session teardown;
- browser/storage policy;
- storage eviction;
- profile reset/removal.

ContactCore therefore does not advertise IndexedDB as equivalent to verified native SQLite backup. CSV/vCard export remains the current explicit portable-copy path on Browser.

Browser capabilities intentionally report native database backup/encryption as unavailable.

A real-browser automated IndexedDB harness and cross-tab conflict handling remain future work.

## ChromeOS

There is no fabricated `net10.0-chromeos` target. ChromeOS support uses routes the platform actually provides:

1. modern Browser/WebAssembly route where compatible;
2. Android application route on ChromeOS devices that support Android applications.

The persistence model follows the chosen route: IndexedDB for Browser, SQLite for Android.

## Shared UI

`ContactCore.UI` supplies the responsive portable experience used by Android/iOS/Browser heads. It contains:

- contact list/search/filter workflows;
- full rich editor;
- duplicate review and both survivor directions;
- import/export hooks;
- capability-aware backup/settings behavior;
- destructive-action confirmation overlay;
- typed compiled production bindings for AOT/trim-safe markup.

The mature desktop shell remains separate because it has deeper desktop-native picker/dialog/window behavior.

## Native persistence/security

Windows/Linux/macOS Desktop plus Android/iOS native composition use SQLite.

Native behavior includes:

- schema-family identity;
- ordered migrations;
- foreign keys/indexes;
- transactional aggregate writes;
- literal wildcard search handling;
- verified backup/restore;
- runtime-only database-key request;
- fail-closed cipher verification when encryption is requested.

The normal public native SQLite dependency should not be described as encrypted at rest unless a production-supported cipher provider has actually been selected, packaged, licensed, and tested.

## Build commands

### Core/Desktop quality

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

### Browser

```bash
dotnet workload install wasm-tools
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

### Android

```bash
dotnet workload install android
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj -r android-arm64
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release -r android-arm64 --no-restore
```

### iOS simulator

```bash
sudo xcode-select -s /Applications/Xcode_26.0.app/Contents/Developer
xcodebuild -version
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj -r iossimulator-arm64
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release -r iossimulator-arm64 --no-restore
```

The project supplies the simulator-only trim policy; these commands do not create a signed App Store binary.

## Automated verification matrix

PR CI requires the current synthetic merge candidate to pass:

- Ubuntu core restore/format/build/tests;
- Windows core restore/format/build/tests;
- macOS core restore/format/build/tests;
- Browser Release build;
- Android `android-arm64` Release build;
- iOS `iossimulator-arm64` Release build using compatible Xcode/simulator trim policy;
- CodeQL.

A successful run for an older commit is not approval of a newer PR head.

## Manual verification still required

Before stronger user-facing platform claims, use fictional/disposable data to check representative targets.

### Desktop

- startup/install/executable behavior;
- keyboard/focus/high-DPI/theme;
- native file pickers;
- import/export/backup/restore;
- accessibility/screen reader where available.

### Android

- phone/tablet touch layout;
- software/hardware keyboard input;
- file picker/storage permissions;
- orientation/lifecycle/background-resume;
- accessibility/theme;
- signed package installation when distribution exists.

### iPhone/iPad

- phone/tablet layout;
- touch/input/file-picker behavior;
- rotation/lifecycle/background-resume;
- VoiceOver/accessibility/theme;
- actual device signing/provisioning/install;
- distribution/device trimming verification in the real signed pipeline.

### Browser

- WebAssembly boot from deployed HTTP(S) origin;
- IndexedDB persistence across reloads;
- import/export picker behavior;
- storage-blocked/private-profile failure behavior;
- multiple representative browser engines;
- accessibility and cross-tab behavior as claims expand.

## Signing and store boundary

The repository does not contain or fabricate:

- Android production keystores/passwords;
- Apple signing certificates/private keys;
- provisioning profiles;
- App Store Connect credentials;
- Developer ID/notarization secrets;
- Windows signing keys.

When a production signing pipeline is introduced, use protected secrets, least privilege, trusted release contexts, and explicit verification instructions. Never expose signing secrets to untrusted pull-request code.

## Packaging

Current automated packages are:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-win-arm64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-linux-arm64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
contactcore-v2.0.12-browser-wasm.zip
SHA256SUMS.txt
```

Android/iOS are build gates rather than automatically attached store packages.

## Release evidence

Use [`release-smoke-test.md`](release-smoke-test.md) for repeatable exact-SHA manual evidence. The record includes platform/environment details, fictional fixture rules, artifacts/checksums, desktop/browser/mobile smoke matrices, data safety, accessibility/privacy checks, deviations, and final release decision.

Do not reuse a manual record after the candidate SHA changes.

## Current limitations

- repeated rich fields do not yet have drag/drop reordering;
- global group/tag taxonomy management is not yet implemented;
- there is no general undo stack;
- Browser still needs a real IndexedDB automation harness and cross-tab conflict strategy;
- deeper native restore cleanup/failure injection remains useful;
- scale benchmarks/candidate optimization remain future work;
- production SQLCipher/secure secret-store integration is not yet shipped;
- signed/notarized/store distribution is not automated;
- representative manual device/browser/accessibility validation remains required.

See `README.md`, `ci-cd.md`, `release.md`, `setup.md`, and `what_changed.md` for related source, verification, distribution, and handoff details.
