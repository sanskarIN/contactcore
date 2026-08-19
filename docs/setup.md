# Setup

This guide covers a source checkout of ContactCore for development or local desktop execution.

## Requirements

- Git.
- .NET SDK compatible with the repository `global.json` policy.
- Windows, macOS, or a 64-bit Linux desktop capable of running Avalonia.

The repository currently pins:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

This means the baseline is .NET SDK 10.0.100, with roll-forward to an installed later .NET 10 feature band when allowed by the .NET SDK resolver. Preview SDKs are not accepted by this policy.

## Clone

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
```

For contribution work, use the branch/PR workflow described in `../CONTRIBUTING.md` rather than committing directly to `main`.

## Confirm SDK resolution

```bash
dotnet --version
dotnet --info
```

Run these commands from the repository directory so `global.json` participates in SDK selection.

If `dotnet --version` reports that a compatible SDK cannot be found, install a stable .NET 10 SDK matching the policy rather than changing `global.json` simply to fit an unrelated local SDK.

## Restore

```bash
dotnet restore ContactCore.slnx
```

Package versions are centrally managed in `Directory.Packages.props`. Individual project files normally reference packages without repeating version numbers.

## Build

```bash
dotnet build ContactCore.slnx -c Release --no-restore
```

The repository enables nullable reference types, latest C# language features, latest-recommended analysis, deterministic builds, and warnings-as-errors through `Directory.Build.props`.

For a normal development build, omit `-c Release` if desired, but Release is the configuration used by CI quality checks.

## Run the desktop application

```bash
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

The application initializes its local data directory and SQLite database on startup.

## Run tests

```bash
dotnet test ContactCore.slnx -c Release
```

For the same main sequence used in CI:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

See `testing.md` for the test-project map and coverage expectations.

## Local data directory

By default `AppPaths` uses the operating system's local application-data directory and appends `ContactCore`. If the platform returns no local-app-data root, it falls back to `AppContext.BaseDirectory/ContactCore`.

The directory contains/derives:

```text
ContactCore/
├── contactcore.db
├── settings.json
└── backups/
```

The exact platform-resolved path is also shown in the application Settings surface.

## Override the data directory

Set `CONTACTCORE_DATA_PATH` to an absolute or relative **directory** path. ContactCore normalizes it with `Path.GetFullPath` and creates it when needed.

Examples:

### PowerShell

```powershell
$env:CONTACTCORE_DATA_PATH = "C:\Temp\ContactCoreDev"
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

### Bash/zsh

```bash
export CONTACTCORE_DATA_PATH="$HOME/.local/share/ContactCoreDev"
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

ContactCore still names the database `contactcore.db` inside the selected directory. Do not set this variable to a filename and expect it to be treated as the database file.

### Development recommendation

When testing destructive migrations, imports, or restores, point `CONTACTCORE_DATA_PATH` to a disposable directory containing fictional data. This prevents development tests from touching a real personal contact database.

## Optional database-key environment variable

`CONTACTCORE_DATABASE_KEY` requests keyed SQLite behavior.

### Important behavior

The default repository dependency is `Microsoft.Data.Sqlite`; ContactCore does not pretend that merely setting a key makes ordinary SQLite encrypted. When a key is supplied, the connection factory sends a hex-encoded `PRAGMA key` and then queries `PRAGMA cipher_version`. If no compatible cipher provider is active, the connection is closed and startup fails.

Therefore, setting this variable on the default ordinary SQLite runtime may intentionally make the application refuse to open.

### PowerShell example

```powershell
$env:CONTACTCORE_DATABASE_KEY = "use-a-secret-source-not-source-control"
```

### Bash/zsh example

```bash
export CONTACTCORE_DATABASE_KEY='use-a-secret-source-not-source-control'
```

Do not place a real key into `.env.example`, committed scripts, source code, screenshots, issue descriptions, or test fixtures. `JsonAppPreferences` keeps the runtime key out of `settings.json`.

For production encryption-provider integration, read `security.md` and ADR `adr/0003-encryption-provider.md` first.

## `.env.example`

The repository includes `.env.example` only as documentation of supported environment names. ContactCore itself reads environment variables through `Environment.GetEnvironmentVariable`; it does not require a dotenv package to load a `.env` file.

If your local shell/tooling loads `.env`, ensure the real `.env` remains ignored and never commit it.

## Platform notes

### Windows

Use a supported Windows desktop with the stable .NET 10 SDK. Native Avalonia file pickers are used for import/export/restore. A source run does not require an installer.

### macOS

The source project targets normal Avalonia desktop execution. Release automation publishes both `osx-x64` and `osx-arm64` self-contained artifacts. Current workflow output is not documented as notarized or signed; macOS security prompts may therefore apply to downloaded release artifacts.

### Linux

Avalonia requires a functioning graphical desktop/session and compatible native graphics/windowing dependencies. Exact package names vary by distribution. If the app fails before displaying a window, inspect `dotnet --info`, desktop-session availability, and Avalonia runtime prerequisites for the distribution.

The current release workflow publishes `linux-x64`.

## Import file behavior

The desktop importer supports one `.csv`, `.vcf`, or `.vcard` file at a time. It decodes UTF-8 text with BOM detection and enforces a 5,000,000-character maximum to bound resource use.

Use fictional data when testing import behavior.

## Resetting a disposable development profile

Only for a **development directory you intentionally made disposable**:

1. close ContactCore;
2. verify `CONTACTCORE_DATA_PATH` points to the disposable directory;
3. preserve anything you need;
4. delete that directory;
5. restart the app to create a fresh schema.

Do not use this procedure on real contact data as a troubleshooting shortcut.

## IDE usage

The repository can be opened in any editor/IDE with .NET 10 and C# support. The `.slnx` solution contains all four production and all four test projects.

Regardless of IDE, run the CLI quality commands before proposing changes because GitHub Actions uses the CLI toolchain.

## Next reading

- `user-guide.md` — application workflows.
- `architecture.md` — project/layer boundaries.
- `development.md` — coding workflow.
- `testing.md` — tests and CI parity.
- `storage-backup-recovery.md` — database safety.
- `troubleshooting.md` — failure diagnosis.
