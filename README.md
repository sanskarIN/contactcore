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

ContactCore keeps a useful address book on your own computer without mandatory accounts, telemetry, advertising, or cloud synchronization. It combines an Avalonia desktop UI with a layered C# architecture, transactional SQLite persistence, atomic imports and duplicate merges, plus verified backup/restore safeguards.

> **Made by the Sanskar**

## Current capabilities

- Create and edit names, nickname, birthday, notes, favorite state, and archive state.
- Add, edit, and remove **multiple** phone numbers and email addresses while preserving each child record identity.
- Add, edit, and remove postal addresses and organization affiliations.
- Add, edit, and remove groups and tags as independent rows, including names containing commas or semicolons.
- Preserve contact IDs, creation timestamps, repeated-field IDs, and complete aggregate state through the editor.
- Distinguish unsaved drafts from persisted contacts so discarding a new draft never invokes permanent database deletion.
- Local search across names, phones, and emails; favorites/archive filters; A–Z navigation; race-safe debounced search.
- CSV and focused vCard 4.0 import/export codecs with bounded desktop import, parser warnings, whole-batch validation, and atomic persistence.
- Duplicate scoring with an interactive candidate list, matching evidence, side-by-side record preview, explicit survivor choice, destructive confirmation, and one-transaction merge/delete persistence.
- SQLite schema migrations, foreign keys, indexed queries, aggregate transactions, literal wildcard escaping, and future-schema rejection.
- SQLite-native backups with integrity/schema-identity verification.
- Staged restore with pre-restore recovery snapshots, migration/verification before switch, and rollback handling.
- Optional fail-closed integration point for a maintained SQLCipher-compatible SQLite provider.
- Runtime-only database key handling; normal JSON preferences do not serialize the key.
- System/Light/Dark themes, visible keyboard focus, shortcuts, local safety preferences, and reduced-motion preference.
- Permanent-delete confirmation enabled by default; restore and duplicate merges require desktop confirmation.
- Cross-platform CI definitions for Windows, Ubuntu, and macOS plus CodeQL analysis.
- Offline-first: no mandatory account, cloud service, analytics, or advertising dependency.

## Current limitations and boundaries

The contact editor now exposes the full persisted aggregate used by the current data model, but repeated fields are **add/edit/remove** rather than drag-reorderable. Groups and tags are editable per contact; there is not yet a separate global taxonomy-management screen.

Duplicate merge is intentionally destructive after confirmation. The selected survivor keeps its identity and preferred existing scalar values while unique child values are combined; the secondary record is removed in the same SQLite transaction. There is no general-purpose undo stack. Use verified backups for recovery needs.

CSV and vCard are **interchange formats, not full-fidelity backups**. CSV writes a limited scalar field set plus the first phone/email. vCard support is a focused subset and does not round-trip every vCard property, address, organization, group, tag, media field, custom extension, or ContactCore identity. CSV formula-like text is preserved rather than spreadsheet-neutralized, so treat exports as data and use care when opening untrusted contact text in spreadsheet software.

The default `Microsoft.Data.Sqlite` provider is ordinary SQLite. Setting `CONTACTCORE_DATABASE_KEY` fails closed unless a SQLCipher-compatible provider can actually report cipher support; this repository does not claim encryption-at-rest in the default build.

Release artifacts are not described as signed or notarized, and desktop accessibility/platform behavior still requires manual release validation before any conformance claim. See the documentation index for exact implementation boundaries.

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

Real screenshots are intentionally deferred until a verified desktop build is captured. When added, they must use fictional sample contacts only and be reviewed for private paths, notifications, and metadata before publication.

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

The CSV importer rejects files with no recognized ContactCore columns instead of creating meaningless contacts, warns on duplicate recognized headers, and warns when imported text starts with a spreadsheet formula character. The vCard importer handles supported escaped delimiters/newlines, maps common `TYPE` parameters, and avoids echoing invalid birthday values in warnings.

Use the verified SQLite backup workflow for full database recovery. The desktop importer bounds selected text at 5,000,000 characters.

## Architecture

The solution is a modular monolith with Domain, Application, Infrastructure, and Desktop layers. Business rules do not depend on Avalonia or SQLite; Infrastructure implements Application abstractions; Desktop is the composition root/platform adapter.

Destructive duplicate merge crosses the same boundaries deliberately: Application computes and validates the merged aggregate, then the repository updates the survivor and deletes the secondary record in one SQLite transaction.

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
