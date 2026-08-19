<div align="center">
  <img src="src/ContactCore.Desktop/Assets/logo.svg" width="128" alt="ContactCore logo" />
  <h1>ContactCore</h1>
  <p>A private, offline-first desktop contact book built for durable local ownership of your data.</p>

[![CI](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml)
[![CodeQL](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)
</div>

> **Made by the Sanskar**

## Why ContactCore

ContactCore keeps an address book on your own computer without mandatory accounts, telemetry, advertising, or cloud synchronization. The project combines a cross-platform Avalonia UI, layered C# architecture, transactional SQLite persistence, portable contact formats, tested backup/restore behavior, and open-source release automation.

The project is currently **pre-release**. The repository is being built incrementally; see [`ROADMAP.md`](ROADMAP.md) and [`what_changed.md`](what_changed.md) for the exact engineering checkpoint.

## Features

### Implemented core

- Create, edit, search, favorite, archive/restore, and delete contacts.
- Domain/storage model for multiple phone numbers, email addresses, postal addresses, organizations, groups, and tags.
- Birthday, nickname, notes, favorite/archive state, and timestamps.
- Unicode/diacritic-aware search normalization and parameterized SQLite search across names, emails, phones, organizations, groups, and tags.
- Configurable search filters for favorites and archived contacts.
- Duplicate scoring from name/email/phone signals plus deterministic merge primitives.
- CSV import/export codec and vCard 4.0 import/export codec.
- Versioned SQLite schema, foreign keys, indexes, and transactional aggregate writes.
- Integrity-checked SQLite backups and staged restore behavior.
- Avalonia desktop workspace with light/dark/system theme support through platform theme resources.
- Safe application error messages that avoid echoing raw contact content.
- Optional database path override through `CONTACTCORE_DATA_PATH`.
- Fail-closed behavior if `CONTACTCORE_DATABASE_KEY` is supplied without a configured encrypted provider.

### Planned polish

The roadmap includes richer multi-value editor screens, duplicate-review UI, native file pickers for import/export/restore, settings, expanded accessibility review, parser fuzzing, batched large-result materialization, real fictional-data screenshots, and signed platform packaging.

## Screenshots

Real screenshots are intentionally deferred until a desktop build has been verified on supported platforms. When added, screenshots must contain fictional data only.

## Supported platforms

Primary desktop targets:

| Platform | Release target |
|---|---|
| Windows | `win-x64` |
| Linux | `linux-x64` |
| macOS Intel | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |

## Technology

- C# / .NET 10
- Avalonia 12.1.1
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Data.Sqlite 10.0.10
- MSTest 4.3.3 + coverlet collector
- GitHub Actions, CodeQL, Dependabot

Package versions are centrally managed in `Directory.Packages.props`; compiler/analyzer policy lives in `Directory.Build.props`.

## Quick start

Prerequisites: Git and the .NET SDK compatible with `global.json`.

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.slnx
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Full Windows/macOS/Linux setup and SDK-upgrade guidance: [`docs/setup.md`](docs/setup.md).

## Development quality gates

```bash
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

Warnings are treated as errors and nullable reference types are enabled solution-wide.

Read:

- [`docs/development.md`](docs/development.md)
- [`docs/testing.md`](docs/testing.md)
- [`CONTRIBUTING.md`](CONTRIBUTING.md)

## Architecture

ContactCore is a modular monolith:

```text
Domain ← Application ← Infrastructure ← Desktop
```

- **Domain**: contact aggregate, value records, validation, normalization.
- **Application**: workflows, repository/backup interfaces, duplicate logic, CSV/vCard codecs.
- **Infrastructure**: SQLite migrations/repository and backup/restore implementation.
- **Desktop**: Avalonia composition root, view model, and shell.

Details: [`docs/architecture.md`](docs/architecture.md) and [`docs/adr/`](docs/adr/).

## Local data, backup, and privacy

The running application does not require a network connection. Contact data is stored in a SQLite database under the current user's local application-data area unless `CONTACTCORE_DATA_PATH` is set to an absolute database file path.

CSV exports and SQLite backups can contain sensitive personal information. Protect those files with appropriate filesystem/encrypted-storage controls and never upload a real contact database/export to a public issue.

The normal SQLite database is **not claimed to be encrypted**. If an encryption key environment variable is supplied without an encrypted provider, startup fails rather than silently using plaintext storage.

Read [`PRIVACY.md`](PRIVACY.md), [`SECURITY.md`](SECURITY.md), [`THREAT_MODEL.md`](THREAT_MODEL.md), and [`docs/security.md`](docs/security.md).

## Build and release

Tags matching `v*.*.*` trigger the release workflow. Each runtime build runs tests, publishes a self-contained desktop artifact, packages it, and feeds the final GitHub Release job.

See [`docs/release.md`](docs/release.md). Current automated artifacts are unsigned; the project does not claim code signing/notarization until that is configured and verified.

## Documentation

- Architecture: [`docs/architecture.md`](docs/architecture.md)
- Setup: [`docs/setup.md`](docs/setup.md)
- Development: [`docs/development.md`](docs/development.md)
- Testing: [`docs/testing.md`](docs/testing.md)
- Security engineering: [`docs/security.md`](docs/security.md)
- Accessibility: [`docs/accessibility.md`](docs/accessibility.md)
- Performance: [`docs/performance.md`](docs/performance.md)
- Troubleshooting: [`docs/troubleshooting.md`](docs/troubleshooting.md)
- Release: [`docs/release.md`](docs/release.md)
- Changelog: [`CHANGELOG.md`](CHANGELOG.md)
- Roadmap: [`ROADMAP.md`](ROADMAP.md)

## Contributing

Contributions are welcome. Use fictional data only, keep commits small and meaningful, add regression tests for bug fixes, and complete the pull-request checklist. See [`CONTRIBUTING.md`](CONTRIBUTING.md) and [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md).

## License

ContactCore is open source under the [MIT License](LICENSE).

## Contact, support, and funding

- Business: **sanskarin@outlook.in**
- Business: **sanskarin.business@gmail.com**
- Support: **supportramsandesh@gmail.com**
- GitHub: https://github.com/sanskarIN
- Buy Me a Coffee: https://buymeacoffee.com/sanskarIN

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)

**Made by the Sanskar**
