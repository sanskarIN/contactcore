# Platform Support

ContactCore 2.0.12 is structured as a shared C#/.NET application with platform-specific Avalonia heads. The contact domain, validation, import/export, duplicate detection/merge rules, and most presentation workflows are shared. Persistence is selected by platform so the application remains local-first without pretending that native SQLite is available inside a browser sandbox.

## Support matrix

| Platform | Project / target | Local persistence | Build verification | Distribution status |
|---|---|---|---|---|
| Windows x64 | `ContactCore.Desktop`, `win-x64` | SQLite | three-OS core CI + release publish | automated ZIP |
| Windows ARM64 | `ContactCore.Desktop`, `win-arm64` | SQLite | release publish | automated ZIP |
| Linux x64 | `ContactCore.Desktop`, `linux-x64` | SQLite | three-OS core CI + release publish | automated tar.gz |
| Linux ARM64 | `ContactCore.Desktop`, `linux-arm64` | SQLite | release publish | automated tar.gz |
| macOS Intel | `ContactCore.Desktop`, `osx-x64` | SQLite | three-OS core CI + release publish | automated tar.gz |
| macOS Apple Silicon | `ContactCore.Desktop`, `osx-arm64` | SQLite | three-OS core CI + release publish | automated tar.gz |
| Android | `ContactCore.Android`, `net10.0-android` | SQLite | dedicated Android workload CI | source/build target; store signing is not automated |
| iPhone | `ContactCore.iOS`, `net10.0-ios` | SQLite | dedicated iOS workload CI on macOS | source/build target; Apple signing/provisioning is not automated |
| iPad | `ContactCore.iOS`, `net10.0-ios` | SQLite | dedicated iOS workload CI on macOS | source/build target; Apple signing/provisioning is not automated |
| Web browser | `ContactCore.Browser`, `net10.0-browser` | IndexedDB; preferences use local browser storage | dedicated WebAssembly workload CI | automated browser ZIP |
| ChromeOS | browser target; Android app where the device supports Android apps | browser IndexedDB or Android SQLite | covered by browser/Android build gates | no separate native ChromeOS package |

A platform being listed here means the repository contains a deliberate application target and a corresponding build path. It does **not** mean every device model, Linux distribution, browser engine, accessibility combination, app-store submission, signing identity, or packaging format has been manually certified.

## Shared application architecture

The portable UI is in `src/ContactCore.UI`. It contains:

- the Avalonia `App` capable of classic desktop and single-view application lifetimes;
- the responsive `MainView` used by phone/tablet/browser heads;
- the complete rich contact draft/editor model;
- search, favorites, archive, A-Z filtering, duplicate review/merge, settings, import/export, and destructive-action confirmation workflows;
- platform-capability flags so native-only functionality is not exposed as if it worked in WebAssembly.

`src/ContactCore.Native` composes the existing hardened SQLite infrastructure for native targets. Android and iOS reference that composition layer. The existing `ContactCore.Desktop` application remains the mature desktop shell and continues to use the same Domain/Application/Infrastructure layers.

## Native persistence: desktop, Android, iOS/iPadOS

Native targets retain the SQLite storage path:

```text
ContactCore.Application
        |
        v
IContactRepository
        |
        v
SqliteContactRepository
        |
        v
Microsoft.Data.Sqlite
```

The same repository therefore preserves the existing transaction, migration, search, duplicate-merge, and backup/recovery semantics on supported native targets.

Native application data is resolved through `AppPaths`. `CONTACTCORE_DATA_PATH` remains an optional development/advanced override where the runtime environment permits environment variables. `CONTACTCORE_DATABASE_KEY` remains runtime-only and still fails closed unless a compatible SQLite encryption provider is actually available.

## Browser persistence

A WebAssembly application cannot be treated as an ordinary native process with unrestricted filesystem/database access. ContactCore therefore uses a dedicated `BrowserContactRepository` that implements the same `IContactRepository` contract and persists the full contact aggregate through a JavaScript bridge into IndexedDB.

The browser repository:

- loads the local contact snapshot during repository initialization;
- keeps all rich contact fields and stable IDs;
- implements search/favorite/archive/tag/group/A-Z filtering;
- performs stale-safe duplicate merge checks;
- serializes writes behind a repository gate;
- restores the previous in-memory state when browser persistence fails;
- keeps data inside the browser profile/site storage unless the user explicitly exports it.

Browser preferences use local browser storage with a session fallback for environments that block it.

### Browser backup boundary

SQLite-native database backup/restore is intentionally disabled in the browser target because there is no native ContactCore SQLite database there. CSV and vCard export remain available for portable browser copies. Those interchange formats have the same documented fidelity limits as desktop exports and are not represented as full-fidelity SQLite backups.

Clearing site data, using private browsing, browser-storage eviction, changing browser profiles, or an administrator policy can remove browser-local data. Users who need a portable copy should export it explicitly.

## Android

Project: `src/ContactCore.Android/ContactCore.Android.csproj`

Target framework: `net10.0-android`.

The Android head contains an `AvaloniaAndroidApplication<App>` host and `AvaloniaMainActivity`. It uses the shared single-view UI and native SQLite service composition.

Developer workload:

```bash
dotnet workload install android
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release
```

CI installs the Android workload and performs a Release build. Store-ready distribution still requires a private signing key and the maintainer's release credentials. Signing secrets must never be committed to this repository.

## iOS and iPadOS

Project: `src/ContactCore.iOS/ContactCore.iOS.csproj`

Target framework: `net10.0-ios`.

The iOS head contains an `AvaloniaAppDelegate<App>`, UIKit entry point, and `Info.plist` declaring both iPhone and iPad device families. It uses the shared single-view UI and native SQLite service composition.

Developer workload:

```bash
dotnet workload install ios
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release
```

A macOS development machine with the required Apple toolchain is needed for normal iOS device/simulator development. Distribution to devices or the App Store additionally requires valid Apple signing/provisioning credentials; those credentials are deliberately not stored in the public repository.

## Browser / WebAssembly

Project: `src/ContactCore.Browser/ContactCore.Browser.csproj`

Target framework: `net10.0-browser`.

Developer workload:

```bash
dotnet workload install wasm-tools
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release
```

Publishing:

```bash
dotnet publish src/ContactCore.Browser/ContactCore.Browser.csproj -c Release -o artifacts/browser
```

The published static files must be served over an HTTP(S) server; opening generated files directly with a `file://` URL is not a supported hosting model.

## Desktop architectures

The tag-driven release workflow publishes six native desktop runtime identifiers:

```text
win-x64
win-arm64
linux-x64
linux-arm64
osx-x64
osx-arm64
```

Windows artifacts are ZIP files; Linux and macOS artifacts are tar.gz files. The final release job also creates `SHA256SUMS.txt` for packaged release assets.

These archives are not claimed to be signed installers, notarized macOS applications, package-manager packages, or store-certified binaries.

## CI design

Cross-platform verification is split deliberately:

1. `ContactCore.Core.slnx` is workload-free and is restored/formatted/built/tested on Ubuntu, Windows, and macOS.
2. The browser job installs `wasm-tools` and builds `ContactCore.Browser`.
3. The Android job installs the Android workload and builds `ContactCore.Android`.
4. The iOS job runs on macOS, installs the iOS workload, and builds `ContactCore.iOS`.
5. CodeQL analyzes the workload-free core solution so security analysis is not coupled to mobile workload availability.

The full `ContactCore.slnx` remains the complete repository solution and includes every production head.

## What “cross-platform” does not promise

Cross-platform source support is different from app-store certification and device-by-device validation. The following remain separate release-engineering or validation work:

- Android production signing / Play Store submission;
- Apple signing, provisioning, notarization where applicable, and App Store submission;
- installer/package-manager formats beyond the current archives;
- manual accessibility and native UI audits on representative phones/tablets/desktops;
- browser compatibility testing across every browser/version;
- native Linux distribution certification across every distro/windowing stack;
- optional SQLCipher/secure-secret-store integration for production encryption-at-rest claims.

Documentation must preserve these boundaries rather than turning a compile target into a stronger certification claim.
