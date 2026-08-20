# Setup

This guide covers a source checkout of ContactCore for desktop, Android, iOS/iPadOS, and browser/WebAssembly development.

## Base requirements

All development paths require:

- Git;
- a stable .NET SDK compatible with the repository `global.json` policy;
- a supported host operating system for the target you intend to build.

The repository pins:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

The baseline is .NET SDK 10.0.100, with roll-forward to an installed later .NET 10 feature band when permitted by the .NET SDK resolver. Preview SDKs are not accepted by this policy.

## Clone

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
```

For contribution work, use the branch/PR workflow in `../CONTRIBUTING.md` rather than committing directly to `main`.

## Confirm SDK resolution

Run from the repository directory:

```bash
dotnet --version
dotnet --info
```

If no compatible SDK resolves, install a stable .NET 10 SDK that satisfies `global.json`. Do not weaken the repository SDK policy merely to fit an unrelated local installation.

## Understand the two solution files

`ContactCore.slnx` is the **complete solution**. It contains the shared layers plus Desktop, Android, iOS, Browser, and test projects. Restoring/building it requires the workloads needed by every included target.

`ContactCore.Core.slnx` is the **workload-free core verification solution**. It contains Domain, Application, Infrastructure, shared UI/native composition, Desktop, and the existing tests. Use it for ordinary desktop development and for the same core quality gate used by CI/CodeQL.

### Core restore/build/test

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

The repository enables nullable reference types, current C# language features, latest-recommended analysis, deterministic builds, and warnings-as-errors through `Directory.Build.props`.

## Desktop: Windows, Linux, macOS

No mobile/WebAssembly workload is needed to run the existing desktop project.

```bash
dotnet restore ContactCore.Core.slnx
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

The desktop application initializes its local SQLite data directory on startup.

### Desktop release RIDs

The release workflow contains these desktop runtime identifiers:

```text
win-x64
win-arm64
linux-x64
linux-arm64
osx-x64
osx-arm64
```

A local RID publish can be produced with the same general pattern used by release automation:

```bash
dotnet publish src/ContactCore.Desktop/ContactCore.Desktop.csproj \
  -c Release \
  -r <RID> \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o artifacts/<RID>
```

Replace `<RID>` with one of the supported values above.

## Android

Project:

```text
src/ContactCore.Android/ContactCore.Android.csproj
```

Target framework:

```text
net10.0-android
```

Install the workload:

```bash
dotnet workload install android
```

Restore and build:

```bash
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release --no-restore
```

For device/emulator deployment, use a configured Android SDK/emulator/device environment supported by the installed .NET Android workload. The project uses the shared Avalonia single-view UI and the native SQLite service composition.

### Android distribution boundary

A successful Release build is not the same as a Play Store-ready signed artifact. Production Android distribution requires a private signing key/keystore and maintainer-controlled credentials. Those secrets must not be committed to this public repository. CI therefore verifies the target build without fabricating release credentials.

## iOS and iPadOS

Project:

```text
src/ContactCore.iOS/ContactCore.iOS.csproj
```

Target framework:

```text
net10.0-ios
```

Use macOS with the required Apple toolchain for normal iOS development. Install the workload:

```bash
dotnet workload install ios
```

Restore and build:

```bash
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release --no-restore
```

`Info.plist` declares both iPhone and iPad device families. The iOS head uses the shared Avalonia single-view UI and native SQLite composition.

### Apple distribution boundary

Device/App Store distribution requires valid Apple signing certificates, provisioning profiles, entitlements as applicable, and maintainer credentials. Those are environment/release secrets, not repository source. The public CI gate verifies source/build compatibility without claiming App Store certification.

## Browser / WebAssembly

Project:

```text
src/ContactCore.Browser/ContactCore.Browser.csproj
```

Target framework:

```text
net10.0-browser
```

Install the WebAssembly workload:

```bash
dotnet workload install wasm-tools
```

Restore and build:

```bash
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

Publish static WebAssembly output:

```bash
dotnet publish src/ContactCore.Browser/ContactCore.Browser.csproj -c Release -o artifacts/browser
```

Serve the published web output through an HTTP(S) development/static server. Do not assume direct `file://` loading represents the supported browser host environment.

### Browser data

The browser target does **not** use the native SQLite database. It stores the complete local contact snapshot in IndexedDB through `BrowserContactRepository` and a JavaScript interop bridge. Preferences use local browser storage with an in-session fallback when persistent preferences are blocked.

Browser-local data can disappear if site data is cleared, a private-browsing session ends, a browser profile is removed, storage is evicted, or policy blocks storage. Export CSV/vCard when a portable copy is needed.

SQLite-native backup/restore is intentionally unavailable in WebAssembly. This is a platform boundary, not a missing button accidentally hidden by the UI.

## Full-solution development

After installing Android, iOS, and WebAssembly workloads on a host/toolchain capable of evaluating those targets, the complete solution can be restored:

```bash
dotnet restore ContactCore.slnx
```

In practice, iOS compilation is a macOS-specific responsibility, so CI splits platform heads into dedicated jobs instead of requiring one machine to be a universal build host.

## Native local data directory

Desktop, Android, and iOS/iPadOS use the `AppPaths`/SQLite path. `AppPaths` resolves the platform local-application-data location and appends `ContactCore`. If no local-app-data root is returned, it falls back to `AppContext.BaseDirectory/ContactCore`.

The native directory derives:

```text
ContactCore/
├── contactcore.db
├── settings.json
└── backups/
```

The platform-resolved location is shown in Settings in the shared/mobile UI and in the desktop Settings surface.

## Override native data directory

`CONTACTCORE_DATA_PATH` is an optional **directory** override on native runtimes where environment variables are practical. ContactCore normalizes it with `Path.GetFullPath` and creates it when required.

PowerShell example:

```powershell
$env:CONTACTCORE_DATA_PATH = "C:\Temp\ContactCoreDev"
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Bash/zsh example:

```bash
export CONTACTCORE_DATA_PATH="$HOME/.local/share/ContactCoreDev"
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

ContactCore still names the database `contactcore.db` inside the selected directory. Do not supply a database filename where a directory is expected.

For destructive migration/import/restore testing, use a deliberately disposable directory containing fictional contacts.

## Optional native database key

`CONTACTCORE_DATABASE_KEY` requests keyed SQLite behavior on native targets.

The default dependency is ordinary `Microsoft.Data.Sqlite`. ContactCore does not claim that setting a key magically encrypts normal SQLite. When a key is supplied, the connection path attempts key setup and verifies that cipher support actually exists; startup fails closed when a compatible cipher provider is not active.

Examples:

```powershell
$env:CONTACTCORE_DATABASE_KEY = "load-this-from-a-secret-source"
```

```bash
export CONTACTCORE_DATABASE_KEY='load-this-from-a-secret-source'
```

Never put a real key in `.env.example`, tracked scripts, source, screenshots, issues, fixtures, or documentation. `JsonAppPreferences` deliberately excludes the runtime database key from `settings.json`.

The browser target does not use this SQLite encryption integration and reports the capability as unavailable.

## `.env.example`

`.env.example` documents supported native environment-variable names. ContactCore reads variables through `Environment.GetEnvironmentVariable`; it does not require a dotenv package.

If external tooling loads a real `.env`, keep that file ignored and untracked.

## Platform notes

### Windows

The desktop head runs on supported Windows desktop environments. Release automation publishes both x64 and ARM64 ZIP archives. They are archives, not MSI/MSIX installers unless such packaging is explicitly added later.

### macOS

Desktop release automation publishes Intel and Apple Silicon archives. Current artifacts are not represented as signed/notarized applications. iOS/iPadOS development additionally requires Apple tooling and signing for device/store distribution.

### Linux

Avalonia desktop execution requires a functioning graphical desktop/session and compatible runtime graphics/windowing dependencies. Exact native dependency package names vary by distribution. Release automation publishes x64 and ARM64 tar.gz archives, but that is not a certification of every Linux distribution/windowing stack.

### ChromeOS

ContactCore has no separate ChromeOS-native project. ChromeOS users can use the browser/WebAssembly target; compatible ChromeOS devices may also run the Android target. Those routes retain their respective browser/Android storage behavior.

## Import/export behavior

Portable UI heads and desktop support one `.csv`, `.vcf`, or `.vcard` import at a time through Avalonia storage APIs where the platform picker supports the action. Import text is bounded at 5,000,000 characters.

CSV/vCard are interoperability formats, not complete database backups. Read `import-export.md` before relying on them for data transfer.

## Resetting a disposable native development profile

Only for a directory you deliberately created as disposable:

1. close ContactCore;
2. verify `CONTACTCORE_DATA_PATH` points to the disposable directory;
3. preserve anything needed;
4. delete that disposable directory;
5. restart the native app to create a fresh schema.

Do not use this as a shortcut on real contact data.

For browser testing, clear site data only when the browser profile contains fictional/disposable ContactCore data or after exporting anything you need.

## IDE usage

Open `ContactCore.slnx` when you want to inspect every target. Open/use `ContactCore.Core.slnx` for ordinary workload-free desktop/core development.

Your IDE must have the relevant .NET workload/toolchain for platform heads you intend to build. Regardless of IDE, run CLI quality commands because GitHub Actions uses the CLI toolchain.

## Next reading

- `platform-support.md` — exact platform/persistence/distribution matrix.
- `architecture.md` — shared layers and platform heads.
- `user-guide.md` — application workflows.
- `development.md` — contribution workflow.
- `testing.md` — tests and CI parity.
- `ci-cd.md` — workload-specific GitHub Actions jobs.
- `storage-backup-recovery.md` — native database safety and browser boundary.
- `troubleshooting.md` — failure diagnosis.
