<div align="center">
  <img src="src/ContactCore.Desktop/Assets/logo.svg" width="128" alt="ContactCore logo" />
  <h1>ContactCore</h1>
  <p>A polished, private, offline-first contact book for Windows, macOS, and Linux.</p>

[![CI](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml)
[![CodeQL](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)
</div>

## Why ContactCore

ContactCore keeps a useful address book on your own computer without mandatory accounts, telemetry, or cloud synchronization. It combines a modern Avalonia UI with a layered C# architecture and a transactional SQLite store suitable for a serious open-source portfolio project.

> **Made by the Sanskar**

## Features

- Create, edit, favorite, archive, and delete contact data.
- Multiple phone/email/address/organization domain fields plus birthday, notes, groups, and tags.
- Fast local search, favorites/archive filters, and alphabetical navigation.
- CSV and vCard 4.0 codecs with edge-case tests.
- Duplicate scoring and deterministic merge logic.
- SQLite migrations, transactions, indexed queries, and integrity-checked backups/restores.
- Optional fail-closed integration point for a maintained SQLCipher-compatible SQLite provider.
- Light/dark/system-ready Avalonia styling, keyboard shortcuts, visible focus, and accessibility-oriented form labels.
- Offline-first: no mandatory cloud, login, analytics, or advertising dependency.

## Screenshots

Real screenshots are intentionally deferred until the first verified desktop build. When added, they must use fictional sample contacts only. See `what_changed.md` for the exact release checkpoint.

## Supported platforms

| Platform | Target |
|---|---|
| Windows | x64 desktop |
| macOS | x64 and arm64 desktop |
| Linux | x64 desktop |

## Technology

- C# / .NET 10
- Avalonia 12.1.1
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Data.Sqlite 10.0.10
- MSTest 4 tests
- GitHub Actions, CodeQL, Dependabot

## Quick start

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.slnx
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Full setup and OS notes: [`docs/setup.md`](docs/setup.md).

## Development and testing

```bash
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

See [`docs/development.md`](docs/development.md) and [`docs/testing.md`](docs/testing.md).

## Build and release

Release tags matching `v*.*.*` trigger multi-platform self-contained publishing. The workflow creates Windows x64, Linux x64, macOS x64, and macOS arm64 artifacts. See [`docs/release.md`](docs/release.md).

## Architecture

The solution is a modular monolith with Domain, Application, Infrastructure, and Desktop layers. Business rules do not depend on Avalonia or SQLite; infrastructure implements application abstractions; the desktop project is the composition root. Read [`docs/architecture.md`](docs/architecture.md) and [`docs/adr/`](docs/adr/).

## Security and privacy

ContactCore stores contacts locally and contains no telemetry or mandatory cloud integration. Do not post real databases to public issues. Optional database encryption is deliberately fail-closed and requires a supported SQLCipher-compatible native provider; details are in [`docs/security.md`](docs/security.md), [`SECURITY.md`](SECURITY.md), and [`PRIVACY.md`](PRIVACY.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md), follow the Code of Conduct, add tests for behavior changes, and keep commits small and meaningful.

## License

MIT — see [`LICENSE`](LICENSE).

## Contact, support, and funding

- Business: **sanskarin@outlook.in**
- Business: **sanskarin.business@gmail.com**
- Support: **supportramsandesh@gmail.com**
- GitHub: https://github.com/sanskarIN
- Buy Me a Coffee: https://buymeacoffee.com/sanskarIN

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)

**Made by the Sanskar**
