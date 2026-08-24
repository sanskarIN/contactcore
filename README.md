# ContactCore

**ContactCore** is a production-oriented, local-first contact manager built with C#/.NET 10 and Avalonia. The current **2.0.12** source line supports desktop, Android, iPhone/iPad, and Browser/WebAssembly application heads while keeping native contact data local by default.

> Made by the Sanskar

## Highlights

- private/local-first contact storage with no mandatory account, cloud sync, or telemetry dependency;
- rich contacts with names, nickname, birthday, notes, favorite/archive state, multiple phones/emails/addresses/organizations, groups, and tags;
- stable root/contact-owned identities through ordinary edits;
- safe shared group/tag reassignment and exact delimiter-containing names;
- debounced/cancellation-safe search and A–Z/favorite/archive filters;
- duplicate scoring, evidence/review, explicit survivor choice, confirmation, stale-safe atomic merge, and conservative country-code-aware phone equivalence;
- CSV/focused-vCard import/export with hardened parsing and explicit spreadsheet-safety boundaries;
- verified native SQLite backup/restore with staging, integrity/schema checks, recovery snapshot, and rollback path;
- runtime-only database-key boundary with fail-closed requested cipher verification;
- responsive portable Avalonia UI for Android/iOS/Browser plus the mature desktop shell;
- Browser persistence through IndexedDB, with native SQLite backup/encryption capabilities explicitly unavailable there;
- strict warnings-as-errors, five behavioral test projects, three-OS core CI, Browser/Android/iOS build gates, and CodeQL;
- tag-driven release automation for six desktop runtime archives plus Browser WebAssembly ZIP and SHA-256 checksums.

## Platforms

| Platform | Source target / RID | Persistence | Current verification/distribution posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | core CI + ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | ZIP release |
| Linux x64 | `linux-x64` | SQLite | core CI + tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | tar.gz release |
| macOS Intel | `osx-x64` | SQLite | core CI + tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | core CI + tar.gz release |
| Android | `net10.0-android`, CI RID `android-arm64` | SQLite | dedicated workload/Release build gate; production signing separate |
| iPhone/iPad | `net10.0-ios`, CI RID `iossimulator-arm64` | SQLite | dedicated unsigned simulator Release build gate; production device signing/provisioning separate |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated WASM Release build + static ZIP |
| ChromeOS | Browser route; Android where supported | IndexedDB/SQLite by route | no false separate native ChromeOS target |

See [`docs/platform-support.md`](docs/platform-support.md) for the full support definition and limitations.

## Cross-platform architecture

The complete solution is `ContactCore.slnx`. The workload-free quality solution is `ContactCore.Core.slnx`.

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

ContactCore.Domain.Tests
ContactCore.Application.Tests
ContactCore.Infrastructure.Tests
ContactCore.UI.Tests
ContactCore.Desktop.Tests
```

### Layer responsibilities

- **Domain** — contact aggregate, field kinds, validation, Unicode/search normalization, phone comparison.
- **Application** — repository/service abstractions, contact workflows, duplicate detection/merge, CSV/vCard codecs.
- **Infrastructure** — native paths/preferences, SQLite connection/migrations/repository, backup/restore, redacting diagnostics.
- **UI** — portable Avalonia application/view models/shared single-view surface for mobile/browser heads.
- **Native** — native SQLite composition shared by Android/iOS.
- **Desktop** — mature Avalonia desktop composition, native pickers/dialogs, desktop-specific UX.
- **Android / iOS** — platform application hosts for the portable UI/native services.
- **Browser** — WebAssembly host, IndexedDB repository, browser preferences/storage interop; no native SQLite dependency.

## Data model

A contact includes:

- given/family name and nickname;
- optional birthday;
- notes;
- favorite/archive flags;
- multiple phones;
- multiple emails;
- multiple addresses;
- multiple organizations;
- groups;
- tags;
- creation/update timestamps;
- stable IDs needed by persistence/editor semantics.

The native SQLite repository persists complete aggregates transactionally. Browser persists the equivalent logical aggregate through IndexedDB-backed serialized state.

## Editing and identity safety

The current desktop and portable editors support the full rich aggregate. For contact-owned repeated rows, surviving IDs are retained through ordinary edits. Shared group/tag identities are handled differently: unchanged/case-equivalent assignments keep their canonical identity, while a true per-contact rename is a reassignment rather than an accidental global rename.

New blank rich rows are ignored on save. Legacy label-only address records remain preservable. Repeated-field drag/drop reordering is not yet implemented.

## Search and duplicate handling

Search is accent-insensitive/lowercase-normalized at the domain boundary. Native SQLite search escapes `%`, `_`, and backslash so user input remains literal rather than becoming accidental SQL `LIKE` wildcard syntax.

Portable search is debounced and cancellation-safe: stale asynchronous results cannot replace a newer query.

Phone duplicate comparison is deliberately conservative. Exact digits-only normalized equality matches immediately. A country-code-style suffix match requires the shorter representation to contain at least ten digits and the longer representation to differ by no more than three leading digits. Duplicate scoring and merge de-duplication share the same rule so destructive merge behavior does not diverge from candidate detection.

Duplicate review shows evidence and both survivor directions. Merge reloads the reviewed records, requires confirmation, and performs survivor update plus secondary deletion atomically on native SQLite. Missing/stale reviewed records cancel rather than being resurrected from UI state.

## Import/export

Supported interchange paths:

- CSV;
- focused vCard 4.0 subset.

These are interchange formats, **not** native full-fidelity backups.

Import behavior includes validation-before-write, bounded desktop reads, controlled malformed-input warnings, safer duplicate/unsupported CSV header handling, deterministic vCard escaping, common TYPE mapping, and privacy-conscious warning text.

Formula-like CSV values are preserved; ContactCore warns about spreadsheet interpretation but does not claim automatic formula neutralization.

See [`docs/import-export.md`](docs/import-export.md).

## Native storage and backup

Desktop/Android/iOS native composition uses SQLite.

Native startup:

1. resolve local application paths;
2. load safe preferences/runtime database-key request;
3. open SQLite through the central connection factory;
4. initialize/migrate schema;
5. verify supported schema/version boundaries;
6. expose repository/service workflows.

Backup creation uses SQLite's native backup path plus integrity/schema-family/version verification.

Restore:

1. verifies the selected source before active data changes;
2. stages and validates/migrates the candidate;
3. creates/verifies a pre-restore recovery snapshot;
4. switches active data only after preflight succeeds;
5. verifies the switched database;
6. attempts rollback from the recovery snapshot if final verification fails.

See [`docs/storage-backup-recovery.md`](docs/storage-backup-recovery.md).

## Browser persistence

`ContactCore.Browser` does **not** reference the native SQLite Infrastructure project. `BrowserContactRepository` implements the existing `IContactRepository` contract and persists contact state through IndexedDB using .NET/JavaScript interop.

Browser behavior includes:

- full aggregate load/save;
- malformed-state and duplicate-root-ID rejection;
- deep-copy repository boundaries;
- serialized writes;
- stale-safe duplicate merge;
- in-memory rollback if IndexedDB persistence fails;
- source-generated JSON metadata for trimming/AOT safety;
- typed compiled shared-UI bindings for trimming/AOT safety;
- browser-local preferences;
- explicit capability reporting that native database backup/encryption is unavailable.

A real-browser automated IndexedDB harness and cross-tab conflict handling remain future work.

## Preferences, privacy and encryption boundary

Preferences include System/Light/Dark theme, reduced-motion, and permanent-delete confirmation. Native preferences use source-generated JSON metadata and temp-file replacement. Malformed preferences fall back to safe defaults.

`CONTACTCORE_DATABASE_KEY` is runtime-only and is deliberately not written to ordinary settings. If a key is requested, ContactCore checks `cipher_version` and fails closed when the active SQLite provider cannot prove SQLCipher-compatible behavior. The ordinary public build does not claim encrypted-at-rest SQLite unless such a provider is deliberately integrated and verified.

Read [`PRIVACY.md`](PRIVACY.md), [`SECURITY.md`](SECURITY.md), and [`docs/security.md`](docs/security.md) before changing storage/security claims.

## Build prerequisites

- .NET 10 SDK compatible with [`global.json`](global.json);
- platform workloads as needed;
- Apple development toolchain on macOS for iOS compilation.

Check SDK:

```bash
dotnet --info
```

Restore/build the workload-free core solution:

```bash
dotnet restore ContactCore.Core.slnx
dotnet build ContactCore.Core.slnx -c Release --no-restore
```

Run all core tests with coverage collection:

```bash
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

Verify formatting:

```bash
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
```

## Desktop

Run the desktop application:

```bash
dotnet run --project src/ContactCore.Desktop/ContactCore.Desktop.csproj
```

Desktop uses local SQLite paths resolved by `AppPaths` and exposes native import/export/backup pickers.

## Browser/WebAssembly

Install the workload:

```bash
dotnet workload install wasm-tools
```

Build:

```bash
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

Browser CI keeps trimming/AOT diagnostics active. Application-owned JSON/binding reflection paths have been removed rather than broadly suppressing linker diagnostics.

For hosting/deployment details see [`docs/platform-support.md`](docs/platform-support.md) and [`docs/release.md`](docs/release.md).

## Android

Install workload and build the same explicit CI runtime identifier:

```bash
dotnet workload install android
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj -r android-arm64
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release -r android-arm64 --no-restore
```

This source build is not a Play Store production signing workflow.

## iPhone/iPad

On macOS, select the Xcode line used by the current CI/workload, then build the simulator RID:

```bash
sudo xcode-select -s /Applications/Xcode_26.0.app/Contents/Developer
xcodebuild -version
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj -r iossimulator-arm64
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release -r iossimulator-arm64 --no-restore
```

The iOS project uses `TrimMode=copy` only for simulator RIDs. The public simulator gate verifies source/runtime integration and is intentionally **not** represented as production device/App Store trimming, signing, provisioning, or certification. Real distribution credentials belong in a future protected signing pipeline, never in source.

## CI / security gate

The latest PR merge candidate must pass:

- core restore/format/Release build/tests on Ubuntu;
- core restore/format/Release build/tests on Windows;
- core restore/format/Release build/tests on macOS;
- Browser/WebAssembly Release build;
- Android `android-arm64` Release build;
- iOS `iossimulator-arm64` Release build using compatible Xcode/simulator policy;
- CodeQL.

CI validates GitHub's synthetic PR merge. Do not merge based on an older green run after the PR head changes.

See [`docs/ci-cd.md`](docs/ci-cd.md).

## Releases

The current source version is **2.0.12**. The intended public tag after verified merge is:

```text
v2.0.12
```

The release workflow version-checks the tag and produces:

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

Android/iOS source builds are mandatory release gates but production mobile store packages are not automatically attached because signing/provisioning requires real maintainer-controlled secrets.

Before release, complete [`docs/release-smoke-test.md`](docs/release-smoke-test.md) against the exact candidate/artifacts or explicitly mark unavailable manual checks.

## Testing

Five current behavioral test projects cover:

- Domain validation/normalization/phone comparison;
- Application services, duplicate logic, CSV/vCard parsing;
- Infrastructure paths/preferences/redaction/SQLite/backup/restore;
- Portable UI debounce/cancellation and destructive-action/restore confirmation;
- Desktop draft identity/rich-field behavior.

See [`docs/testing.md`](docs/testing.md).

## Documentation

Start at [`docs/README.md`](docs/README.md). Important references include:

- [`docs/platform-support.md`](docs/platform-support.md)
- [`docs/setup.md`](docs/setup.md)
- [`docs/development.md`](docs/development.md)
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/data-model.md`](docs/data-model.md)
- [`docs/desktop-ui.md`](docs/desktop-ui.md)
- [`docs/user-guide.md`](docs/user-guide.md)
- [`docs/import-export.md`](docs/import-export.md)
- [`docs/storage-backup-recovery.md`](docs/storage-backup-recovery.md)
- [`docs/security.md`](docs/security.md)
- [`docs/accessibility.md`](docs/accessibility.md)
- [`docs/performance.md`](docs/performance.md)
- [`docs/testing.md`](docs/testing.md)
- [`docs/troubleshooting.md`](docs/troubleshooting.md)
- [`docs/ci-cd.md`](docs/ci-cd.md)
- [`docs/release.md`](docs/release.md)
- [`docs/release-smoke-test.md`](docs/release-smoke-test.md)
- [`docs/maintainer-guide.md`](docs/maintainer-guide.md)
- [`docs/repository-reference.md`](docs/repository-reference.md)
- [`what_changed.md`](what_changed.md)

The canonical repository inventory currently contains **132 tracked files**.

## Remaining roadmap

Current non-blocking future work includes:

- drag/reorder controls for repeated fields;
- global group/tag taxonomy management;
- general undo/recovery UX;
- real IndexedDB browser automation and cross-tab conflict handling;
- deeper native restore failure/cleanup injection;
- representative accessibility/lifecycle automation;
- scale benchmarks and duplicate-candidate optimization;
- production SQLCipher/OS-secret-store integration if selected;
- signed/notarized/store distribution pipelines;
- additional installer/package-manager formats.

See [`ROADMAP.md`](ROADMAP.md). These are intentionally not represented as completed capabilities.

## Contributing and support

Read [`CONTRIBUTING.md`](CONTRIBUTING.md), [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md), [`SUPPORT.md`](SUPPORT.md), and [`SECURITY.md`](SECURITY.md).

When reporting issues, do not upload a real contact database, backup, export, credentials, signing material, or screenshots containing personal information. Use fictional/minimized reproductions.

## License

MIT — see [`LICENSE`](LICENSE).
