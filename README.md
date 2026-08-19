<div align="center">
  <img src="src/ContactCore.Desktop/Assets/logo.svg" width="128" alt="ContactCore logo" />
  <h1>ContactCore</h1>
  <p>A private, offline-first desktop contact manager for Windows, macOS, and Linux.</p>

[![CI](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml)
[![CodeQL](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)
</div>

## Why ContactCore

ContactCore keeps a useful address book on your own computer without mandatory accounts, telemetry, advertising, or cloud synchronization. It combines an Avalonia desktop UI with a layered C# architecture, transactional SQLite persistence, atomic imports, and verified backup/restore safeguards.

> **Made by the Sanskar**

## Current capabilities

- Create and edit contact basics: names, birthday, phone, email, notes, favorite, and archived state.
- Domain/storage model for multiple phones, emails, addresses, organizations, groups, and tags.
- Compact desktop editing that preserves additional/unexposed rich child fields when an existing contact is saved.
- Local search across names, phones, and emails; favorites/archive filters; A–Z navigation.
- CSV and focused vCard 4.0 import/export codecs.
- Whole-batch import validation plus one-transaction persistence.
- Duplicate scoring and deterministic application-layer merge logic.
- SQLite schema migrations, foreign keys, indexed queries, aggregate transactions, and future-schema rejection.
- SQLite-native backups with integrity/schema-identity verification.
- Staged restore with pre-restore recovery snapshots, migration/verification before switch, and rollback handling.
- Optional fail-closed integration point for a maintained SQLCipher-compatible SQLite provider.
- Runtime-only database key handling; normal JSON preferences do not serialize the key.
- System/Light/Dark themes, visible keyboard focus, shortcuts, local safety preferences, and reduced-motion preference.
- Permanent-delete confirmation enabled by default; restore always requires desktop confirmation.
- Cross-platform CI on Windows, Ubuntu, and macOS plus CodeQL analysis.
- Offline-first: no mandatory account, cloud service, analytics, or advertising dependency.

## Important current UI limitations

The rich `Contact` domain/database model is still ahead of the current desktop editor. Today, the main editor exposes **one phone and one email** and does not yet expose addresses, organizations, groups, tags, or additional repeated phone/email rows.

The compact draft now starts from a deep copy of the complete loaded aggregate, changes only the scalar/visible primary phone/email fields, and preserves additional phones/emails plus addresses, organizations, groups, and tags during ordinary edit/save operations. Clearing the visible primary phone/email removes only that primary value while retaining remaining values. Regression tests cover this preservation behavior.

This prevents the prior hidden-field data-loss risk, but it is **not** a claim of full rich-field editing: additional values can be preserved yet still cannot be edited from the current main editor. See [`docs/desktop-ui.md`](docs/desktop-ui.md).

The **Find duplicates** button currently reports candidate count/highest score. The application layer contains merge logic, but a complete interactive pair-review/merge screen is not yet present.

## Documentation

Start with the complete documentation index: **[`docs/README.md`](docs/README.md)**.

Key guides:

- [User guide](docs/user-guide.md)
- [Setup](docs/setup.md)
- [Architecture](docs/architecture.md)
- [Data model](docs/data-model.md)
- [Desktop UI](docs/desktop-ui.md)
- [Import/export](docs/import-export.md)
- [Storage, backup, and recovery](docs/storage-backup-recovery.md)
- [Security engineering](docs/security.md)
- [Testing](docs/testing.md)
- [Performance](docs/performance.md)
- [CI/CD](docs/ci-cd.md)
- [Release](docs/release.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Maintainer guide](docs/maintainer-guide.md)
- [Repository file reference](docs/repository-reference.md)
- [Architecture decision records](docs/adr/)

## Screenshots

Real screenshots are intentionally deferred until a verified desktop build is captured. When added, they must use fictional sample contacts only and be reviewed for private paths/notifications/metadata before publication.

## Supported release targets

Current release automation publishes these runtime identifiers:

| Platform | Target |
|---|---|
| Windows | `win-x64` |
| Linux | `linux-x64` |
| macOS Intel | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |

These artifacts are currently documented as self-contained/single-file builds, **not** as signed installers or notarized applications.

## Technology

- C# / .NET 10 (`global.json`: SDK 10.0.100 with `latestFeature` roll-forward)
- Avalonia 12.1.1
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Data.Sqlite 10.0.10
- MSTest 4.3.3 across **4 test projects**
- coverlet collector for CI coverage artifacts
- GitHub Actions, CodeQL, Dependabot

Package versions are centralized in `Directory.Packages.props`; shared compiler/analyzer rules are in `Directory.Build.props`.

## Quick start

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.slnx
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Full setup, SDK, environment, data-directory, and OS notes: [`docs/setup.md`](docs/setup.md).

## Quality commands

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

CI performs restore/format/build/test on Windows, Ubuntu, and macOS. See [`docs/testing.md`](docs/testing.md) and [`docs/ci-cd.md`](docs/ci-cd.md).

## Data location and configuration

By default ContactCore stores local data under the operating system's local application-data location in a `ContactCore` directory. The application derives:

```text
contactcore.db
settings.json
backups/
```

`CONTACTCORE_DATA_PATH` overrides the **directory**. `CONTACTCORE_DATABASE_KEY` requests keyed SQLite behavior but deliberately fails when no SQLCipher-compatible provider can be verified. Do not put real keys into tracked files.

See [`docs/storage-backup-recovery.md`](docs/storage-backup-recovery.md) and [`docs/security.md`](docs/security.md).

## Import/export vs backup

CSV and vCard are interoperability formats, not full-fidelity ContactCore backups. CSV currently exports only the first phone/email and a limited field set; vCard support is intentionally focused.

Use the verified SQLite backup workflow for full database recovery. The desktop importer bounds selected text at 5,000,000 characters.

## Architecture

The solution is a modular monolith with Domain, Application, Infrastructure, and Desktop layers. Business rules do not depend on Avalonia or SQLite; infrastructure implements application abstractions; Desktop is the composition root/platform adapter.

Read [`docs/architecture.md`](docs/architecture.md) and the [`docs/adr/`](docs/adr/) records.

## Security and privacy

ContactCore stores contacts locally and contains no mandatory cloud/telemetry integration. Local-first does not automatically mean encrypted-at-rest: the default ordinary SQLite provider is plaintext unless a compatible encryption provider is deliberately integrated.

Do not post real databases, backups, exports, contact screenshots, or encryption keys to public issues. Details: [`docs/security.md`](docs/security.md), [`SECURITY.md`](SECURITY.md), and [`PRIVACY.md`](PRIVACY.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md), follow [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), add regression tests for behavior changes, preserve layer boundaries/data-safety invariants, and keep commits small and meaningful.

Maintainers should also read [`docs/maintainer-guide.md`](docs/maintainer-guide.md).

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
