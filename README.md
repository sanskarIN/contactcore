<div align="center">
  <img src="src/ContactCore.Desktop/Assets/logo.svg" width="128" alt="ContactCore logo" />
  <h1>ContactCore</h1>
  <p>A private, offline-first cross-platform contact manager for Windows, Linux, macOS, Android, iPhone/iPad, and WebAssembly browsers.</p>

[![Version](https://img.shields.io/badge/version-2.0.12-0969da.svg)](CHANGELOG.md)
[![CI](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/ci.yml)
[![CodeQL](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml/badge.svg)](https://github.com/sanskarIN/contactcore/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-sanskarIN-FFDD00?logo=buy-me-a-coffee&logoColor=000000)](https://buymeacoffee.com/sanskarIN)
</div>

## Current source version

**ContactCore 2.0.12**. Version metadata is centralized in `Directory.Build.props`, and release preflight rejects a semantic tag that does not match the source version.

> **Made by the Sanskar**

## Platform support

ContactCore now has deliberate application targets for desktop, mobile, and browser environments rather than treating “cross-platform” as a desktop-only label.

| Platform | Target | Persistence | Release/build posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | automated ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | automated ZIP release |
| Linux x64 | `linux-x64` | SQLite | automated tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | automated tar.gz release |
| macOS Intel | `osx-x64` | SQLite | automated tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | automated tar.gz release |
| Android | `net10.0-android` | SQLite | dedicated CI build target; production signing remains external |
| iPhone / iPad | `net10.0-ios` | SQLite | dedicated macOS CI build target; Apple signing/provisioning remains external |
| Browser / WebAssembly | `net10.0-browser` | IndexedDB + local browser preferences | automated browser ZIP release |
| ChromeOS | browser target; Android route on compatible devices | IndexedDB or SQLite according to route | no separate native ChromeOS package |

See **[`docs/platform-support.md`](docs/platform-support.md)** for the exact support, persistence, CI, signing, and validation boundaries.

## Why ContactCore

ContactCore keeps contact management local-first without requiring an account, telemetry service, advertising network, or cloud synchronization backend. Shared C# Domain/Application logic is reused across platforms. Native targets retain the hardened SQLite path; WebAssembly uses a browser-native IndexedDB repository behind the same application repository contract.

That means platform differences are explicit instead of hidden:

- native desktop/mobile can use SQLite-native backup/restore;
- browser data lives in browser-managed storage and uses export for portable copies;
- Android/iOS source is build-gated without committing private store-signing secrets;
- the same contact validation, duplicate/merge rules, import/export codecs, and rich aggregate model are shared.

## Current capabilities

- Create/edit names, nickname, birthday, notes, favorite state, and archive state.
- Add/edit/remove multiple phones and emails while preserving contact-owned record identity.
- Add/edit/remove postal addresses and organization affiliations with stable contact-owned IDs.
- Add/edit/remove groups and tags as independent shared dictionary assignments, including names containing commas/semicolons.
- Preserve root contact ID, creation timestamp, complete aggregate state, contact-owned child IDs, and unchanged group/tag shared identity through normal edits.
- Treat a true per-contact group/tag rename as safe reassignment rather than reusing one global dictionary ID with another name.
- Distinguish unsaved drafts from persisted contacts so discard does not become a permanent delete.
- Local search across names/phones/emails, favorites/archive filters, A-Z navigation, and race-safe debounced search.
- CSV and focused vCard 4.0 import/export with bounded text input, parser warnings, batch validation, and storage-consistent persistence.
- Duplicate scoring/review with evidence, preview, explicit survivor choice, destructive confirmation, and stale-safe merge behavior.
- Native SQLite schema migrations, foreign keys, indexes, aggregate transactions, literal wildcard escaping, and future-schema rejection.
- Native verified SQLite backups and staged restore with pre-restore snapshot/rollback safeguards.
- Optional fail-closed native SQLCipher-compatible integration point; runtime database key is not serialized into normal preferences.
- System/Light/Dark themes, reduced-motion preference, delete confirmation, keyboard shortcuts on keyboard-capable hosts, and responsive single-view UI for mobile/browser.
- Browser IndexedDB persistence with serialized writes and in-memory rollback when persistence fails.
- Cross-platform CI: three-OS core build/test plus separate WebAssembly, Android, and iOS Release builds.
- CodeQL analysis on the workload-free core solution.
- Version-checked release automation, six desktop architecture archives, browser WebAssembly package, mobile build gate, and SHA-256 checksum publication.

## Persistence model

### Native: desktop, Android, iOS/iPadOS

Native targets use:

```text
ContactCore.Application
  → IContactRepository
  → SqliteContactRepository
  → Microsoft.Data.Sqlite
```

They retain the existing SQLite migration, transactional merge/import, backup, restore, and database-key boundaries.

### Browser / WebAssembly

Browser builds do not pretend a native SQLite database exists in a web sandbox. They use:

```text
ContactCore.Application
  → IContactRepository
  → BrowserContactRepository
  → .NET/JavaScript interop
  → IndexedDB
```

Browser preferences use local browser storage with a session fallback when persistent preferences are blocked. Clearing site data/private-profile state or browser storage eviction can remove browser-local contacts, so export important portable copies.

## Important limitations and boundaries

Repeated rich fields support add/edit/remove, not drag-reordering. Groups/tags are editable per contact, but there is not yet a separate global taxonomy rename/delete/orphan-cleanup screen. A true per-contact rename is reassignment; unreferenced dictionary rows can remain until a future explicit cleanup feature defines deletion semantics.

Duplicate merge is destructive after confirmation. Native SQLite performs survivor update + secondary delete in one transaction. Browser storage performs the logical merge behind its repository write gate and restores the previous in-memory snapshot if IndexedDB persistence fails. There is no general-purpose undo stack.

CSV/vCard are **interchange formats, not full-fidelity backups**. CSV contains a limited scalar set plus first phone/email; focused vCard does not round-trip every possible vCard/custom/media/contact-identity field.

Native local-first does not automatically mean encrypted-at-rest: default `Microsoft.Data.Sqlite` is ordinary SQLite. `CONTACTCORE_DATABASE_KEY` fails closed unless compatible cipher support can actually be verified. Browser persistence has a different security/storage model and does not claim the native SQLite encryption capability.

Current downloadable desktop/browser artifacts are not represented as signed installers, notarized applications, package-manager packages, or store-certified binaries. Android/iOS production distribution requires maintainer-controlled signing/provisioning credentials that are intentionally not committed.

Manual device/browser/accessibility validation remains required before making stronger conformance claims.

## Solution structure

```text
ContactCore.Domain
ContactCore.Application
ContactCore.Infrastructure
ContactCore.UI
ContactCore.Native
ContactCore.Desktop
ContactCore.Android
ContactCore.iOS
ContactCore.Browser
```

`ContactCore.UI` is the portable Avalonia single-view layer. `ContactCore.Native` composes the existing SQLite services for native mobile heads. `ContactCore.Browser` supplies a browser repository/storage adapter instead of referencing native Infrastructure.

Two solution files exist intentionally:

- `ContactCore.slnx` — complete solution with every application head;
- `ContactCore.Core.slnx` — workload-free core/Desktop/test solution used by ordinary three-OS CI and CodeQL.

Read [`docs/architecture.md`](docs/architecture.md) for the dependency map and data flows.

## Technology

- ContactCore **2.0.12**
- C# / .NET 10 (`global.json`: SDK 10.0.100, `latestFeature` roll-forward)
- Avalonia 12.1.1
- Avalonia Desktop / Android / iOS / Browser packages 12.1.1
- CommunityToolkit.Mvvm 8.4.2
- Microsoft.Data.Sqlite 10.0.11 on native storage path
- MSTest 4.3.3 across the existing four behavioral test projects
- coverlet collector for CI coverage artifacts
- GitHub Actions, CodeQL, Dependabot
- IndexedDB + .NET JavaScript interop for browser persistence

Package versions are centralized in `Directory.Packages.props`; compiler/analyzer/version rules are in `Directory.Build.props`.

## Quick start: desktop/core

```bash
git clone https://github.com/sanskarIN/contactcore.git
cd contactcore
dotnet restore ContactCore.Core.slnx
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Core quality sequence:

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

## Build the browser target

```bash
dotnet workload install wasm-tools
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
dotnet publish src/ContactCore.Browser/ContactCore.Browser.csproj -c Release -o artifacts/browser
```

Serve published files through HTTP(S); direct `file://` loading is not the intended WebAssembly host model.

## Build Android

```bash
dotnet workload install android
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release --no-restore
```

Production Android distribution needs private signing configuration outside source control.

## Build iOS/iPadOS

On macOS with the required Apple toolchain:

```bash
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release --no-restore
```

Device/App Store distribution additionally needs Apple signing/provisioning credentials.

Full environment/workload notes: [`docs/setup.md`](docs/setup.md).

## Automated release packages

For v2.0.12 the release workflow is configured to produce:

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

Android/iOS Release builds are prerequisites for the final release job but signed mobile store packages are not automatically attached until a secure signing pipeline is deliberately configured.

## Native data location and configuration

Desktop/mobile native storage derives:

```text
contactcore.db
settings.json
backups/
```

under the platform local application-data directory. `CONTACTCORE_DATA_PATH` can override the directory where runtime environment-variable use is practical. `CONTACTCORE_DATABASE_KEY` requests keyed native SQLite behavior and deliberately fails when compatible cipher support cannot be verified.

Do not put real keys, signing credentials, databases, backups, exports, or contact screenshots into tracked/public files.

## Documentation

Start with **[`docs/README.md`](docs/README.md)**.

Key guides:

- [Platform support](docs/platform-support.md)
- [Setup](docs/setup.md)
- [User guide](docs/user-guide.md)
- [Architecture](docs/architecture.md)
- [Data model](docs/data-model.md)
- [Desktop UI](docs/desktop-ui.md)
- [Import/export](docs/import-export.md)
- [Storage, backup, and recovery](docs/storage-backup-recovery.md)
- [Security engineering](docs/security.md)
- [Testing](docs/testing.md)
- [CI/CD](docs/ci-cd.md)
- [Release](docs/release.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Maintainer guide](docs/maintainer-guide.md)
- [Repository file reference](docs/repository-reference.md)
- [Architecture decision records](docs/adr/)

## Screenshots

Real screenshots should be added only after verified builds are captured using clearly fictional sample contacts. Review images for private paths, notifications, usernames, addresses, and metadata before publication.

## Security and privacy

ContactCore contains no mandatory cloud synchronization/telemetry/account dependency. Native contacts remain in the local SQLite store unless the user explicitly exports/copies data. Browser contacts remain in browser-managed local storage for that origin/profile unless explicitly exported or browser policies clear/move them.

Do not post real databases, backups, exports, browser contact dumps, contact screenshots, encryption keys, or signing credentials to public issues.

See [`docs/security.md`](docs/security.md), [`SECURITY.md`](SECURITY.md), and [`PRIVACY.md`](PRIVACY.md).

## Contributing

Read [`CONTRIBUTING.md`](CONTRIBUTING.md), follow [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), preserve layer/storage-safety invariants, add regression coverage for behavior changes, and keep documentation synchronized with platform changes.

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
