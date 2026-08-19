# Setup

## Required tools

ContactCore is pinned by `global.json` to .NET SDK `10.0.100` with feature-band roll-forward enabled. Install Git and a compatible .NET 10 SDK before cloning.

Recommended editor choices:

- Visual Studio 2026 or another Visual Studio release that supports .NET 10;
- JetBrains Rider with .NET 10 support;
- Visual Studio Code with the official C# tooling.

The repository does not require a global Avalonia template installation because package references are restored from the project files.

## Verify your machine

```bash
git --version
dotnet --info
dotnet --list-sdks
```

`dotnet --version` from the repository root should resolve to an allowed .NET 10 SDK. If it reports that the requested SDK cannot be found, install/update the .NET 10 SDK rather than editing `global.json` locally.

## Windows 11

A typical command-line installation uses Windows Package Manager:

```powershell
winget install --id Git.Git -e
winget install --id Microsoft.DotNet.SDK.10 -e
```

Close and reopen the terminal after installation, then verify `git --version` and `dotnet --info`.

## macOS

Install Git through Xcode Command Line Tools or your preferred package manager. Install a supported .NET 10 SDK from Microsoft or a trusted package manager, then verify:

```bash
git --version
dotnet --info
```

On Apple Silicon, use an arm64 .NET SDK. ContactCore's release workflow targets both `osx-arm64` and `osx-x64`.

## Linux

Install Git from your distribution package manager. Install the Microsoft-supported .NET 10 SDK package for your distribution. On Debian/Ubuntu-family systems, after configuring Microsoft's package feed for your exact distribution release, the SDK package name is typically:

```bash
sudo apt-get update
sudo apt-get install dotnet-sdk-10.0
```

Do not paste package-feed commands intended for a different distribution/release. Use Microsoft's current Linux installation page when setting up the feed.

## Clone and restore

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.slnx
```

## Build

```bash
dotnet build ContactCore.slnx -c Release
```

Warnings are treated as errors, so a successful build is also a useful static-quality gate.

## Run

```bash
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

On first launch ContactCore creates its local application-data directory and initializes `contactcore.db` automatically.

## Test

```bash
dotnet test ContactCore.slnx -c Release
```

For the same formatting gate used by CI:

```bash
dotnet format ContactCore.slnx --verify-no-changes
```

## Updating an out-of-support SDK

Do not blindly change framework/package versions only because a machine has a newer SDK.

1. Check the currently pinned SDK in `global.json` and target framework in `Directory.Build.props`.
2. Confirm the desired SDK is supported by the repository's dependencies and CI runners.
3. Install the new SDK side-by-side.
4. Update `global.json`, target framework, and package versions in one focused branch.
5. Run restore, format, release build, full tests, and platform smoke tests.
6. Review .NET/Avalonia breaking-change notes.
7. Update documentation and create an ADR when the upgrade changes architecture/runtime behavior.

## Local data safety during development

ContactCore uses the operating system's local application-data folder. Before testing destructive migrations or restore behavior against an existing development database, create a backup. Never copy a real personal contact database into this public repository.

For isolated experiments, prefer a temporary OS user profile or a throwaway database path in tests.

## Next reading

- `docs/development.md`
- `docs/testing.md`
- `docs/troubleshooting.md`
- `docs/architecture.md`
