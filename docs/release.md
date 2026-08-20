# Release

ContactCore uses semantic Git tags matching `v*.*.*` to trigger GitHub Actions publishing. A release must represent a verified repository state, not merely a tag that happens to create artifacts.

The current source version is **2.0.12** and the intended release tag is **`v2.0.12`**.

## Version source of truth

Application version metadata is centralized in `Directory.Build.props`:

```text
VersionPrefix        2.0.12
Version              2.0.12
AssemblyVersion      2.0.12.0
FileVersion          2.0.12.0
InformationalVersion 2.0.12
```

The release workflow resolves the desktop project's `Version` through MSBuild and requires:

```text
GITHUB_REF_NAME == "v" + Version
```

For this source tree, `v2.0.12` is accepted; a mismatched tag such as `v2.0.13` is rejected during preflight.

Application version and SQLite schema version remain separate concepts.

## Release-support matrix

### Automated downloadable packages

| Platform | Target | Runner | Release package |
|---|---|---|---|
| Windows x64 | `win-x64` | `windows-latest` | `contactcore-v2.0.12-win-x64.zip` |
| Windows ARM64 | `win-arm64` | `windows-latest` | `contactcore-v2.0.12-win-arm64.zip` |
| Linux x64 | `linux-x64` | `ubuntu-latest` | `contactcore-v2.0.12-linux-x64.tar.gz` |
| Linux ARM64 | `linux-arm64` | `ubuntu-latest` | `contactcore-v2.0.12-linux-arm64.tar.gz` |
| macOS Intel | `osx-x64` | `macos-latest` | `contactcore-v2.0.12-osx-x64.tar.gz` |
| macOS Apple Silicon | `osx-arm64` | `macos-latest` | `contactcore-v2.0.12-osx-arm64.tar.gz` |
| Browser/WebAssembly | `net10.0-browser` | `ubuntu-latest` | `contactcore-v2.0.12-browser-wasm.zip` |

The final release job generates `SHA256SUMS.txt` for packaged release assets.

### Build-gated mobile targets

| Platform | Project | Runner | Release gate |
|---|---|---|---|
| Android | `ContactCore.Android` / `net10.0-android` | `ubuntu-latest` | install Android workload + Release build |
| iPhone/iPad | `ContactCore.iOS` / `net10.0-ios` | `macos-latest` | install iOS workload + Release build |

Mobile source/build compatibility is a release requirement, but this public workflow does not attach production Android/iOS store packages because store/device distribution requires private signing/provisioning credentials.

That boundary is intentional. Never add a real keystore, certificate, provisioning profile, signing password, or private key to source merely to make a public workflow produce a mobile store binary.

## Trigger

A push of a tag matching:

```text
v*.*.*
```

starts `.github/workflows/release.yml`. Preflight rejects tags that do not equal the project version.

## Pre-release checklist for 2.0.12

Before creating `v2.0.12`:

1. `main` contains the intended 2.0.12 source and documentation.
2. The exact final commit has successful `core-build-test` CI on Ubuntu, Windows, and macOS.
3. The exact final commit has successful Browser, Android, and iOS build jobs.
4. CodeQL for that exact commit has no unresolved newly introduced actionable issue.
5. `Directory.Build.props` resolves project version `2.0.12`.
6. `CHANGELOG.md` contains the 2.0.12/cross-platform release-preparation changes.
7. `README.md`, `docs/README.md`, `platform-support.md`, setup, architecture, data/storage/security/testing/CI/release docs match actual code.
8. `what_changed.md` records the exact final verification state rather than an older green commit.
9. No real contact data, database, backup, export, `.env`, key, certificate, keystore, provisioning profile, signing material, or private endpoint is tracked.
10. Native schema changes, if any, have upgrade tests and restore compatibility review.
11. Import/export changes have malformed-input/privacy/regression coverage.
12. Rich-contact editing has been smoke-tested with fictional data on representative UI targets where practical.
13. Both duplicate-survivor directions and confirmation/cancellation behavior have been exercised with fictional data.
14. Native backup creation/restore has been tested against a disposable profile.
15. Browser import/export and IndexedDB persistence behavior has been tested in a disposable browser profile when preparing a user-facing browser release.
16. Known platform/signing/accessibility limitations are present in release notes.

## Local core quality pass

The workload-free quality sequence is:

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build
```

Platform builds require their workload:

```bash
dotnet workload install wasm-tools
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release
```

```bash
dotnet workload install android
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release
```

On macOS:

```bash
dotnet workload install ios
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release
```

Local success is useful but does not replace the GitHub matrix on the exact final head.

## Tagging 2.0.12

After the verified 2.0.12 commit is on `main`:

```bash
git checkout main
git pull --ff-only
git tag -a v2.0.12 -m "ContactCore v2.0.12"
git push origin v2.0.12
```

Do not create the intended public release tag from an unmerged audit branch unless the project explicitly adopts a branch-based release policy.

## Automated release sequence

### 1. Preflight

- checkout tag;
- setup .NET from `global.json`;
- resolve `ContactCore.Desktop` version;
- fail if tag != `v<Version>`.

### 2. Desktop publish matrix

For each of the six RIDs:

- restore `ContactCore.Core.slnx`;
- run core tests in Release;
- publish `ContactCore.Desktop` self-contained and single-file-targeted;
- package Windows as ZIP or Linux/macOS as tar.gz;
- upload the package as an Actions artifact.

Unix output is tarred before `actions/upload-artifact` to retain executable metadata inside the archive.

### 3. Browser publish

- install `wasm-tools`;
- `dotnet publish` `ContactCore.Browser` in Release;
- ZIP the full static output;
- upload `contactcore-v2.0.12-browser-wasm.zip`.

The browser artifact is deployable static web content. It is not a hosted website until a maintainer deploys it to an appropriate HTTP(S) server.

### 4. Mobile build gate

In parallel matrix entries:

- Ubuntu installs Android workload and Release-builds `ContactCore.Android`;
- macOS installs iOS workload and Release-builds `ContactCore.iOS`.

The final release depends on this job. Broken mobile source should therefore block the tag release even though mobile store packages are not attached automatically.

### 5. Final GitHub Release

After preflight, desktop publish, browser publish, and mobile build gate succeed:

- download/merge packaged artifacts;
- generate SHA-256 checksum file;
- publish release notes;
- attach desktop/browser archives and checksums.

## Workflow permissions

Default:

```text
contents: read
```

Only the final GitHub Release job receives:

```text
contents: write
```

Build jobs do not need repository write permission.

## Artifact verification

Checksums support byte-integrity comparison against the published checksum list; they do not provide the identity/authenticity guarantees of trusted platform code signing.

### Desktop smoke checks

For representative native packages verify with fictional/disposable data:

- startup;
- database creation;
- rich contact create/edit/save;
- search/favorites/archive/A-Z;
- duplicate review and both survivor directions;
- theme/settings;
- CSV/vCard import/export;
- verified backup creation and restore;
- unsaved-draft discard and permanent-delete confirmation.

### Browser smoke checks

After serving the browser publish over HTTP(S), verify:

- WebAssembly boot;
- IndexedDB persistence across reloads;
- rich edit/search/filter/duplicate flows;
- import/export through the browser storage picker path;
- theme/preferences behavior;
- correct absence of native SQLite backup/restore claims;
- useful failure behavior if browser storage is blocked.

Do not test destructive storage behavior in a browser profile containing real contacts.

### Android/iOS smoke checks

A compile gate is not equivalent to device testing. Before public store distribution, use representative devices/simulators with fictional data and verify touch layout, keyboard/input behavior, local persistence, file-picker availability, orientation, accessibility, and lifecycle/background/restore behavior as applicable.

## Signing and notarization

The current workflow does **not** implement or claim:

- Authenticode signing for Windows;
- Windows installer signing;
- Apple Developer ID signing/notarization for macOS;
- Linux package signing;
- Play Store production signing;
- iOS/iPadOS distribution signing/provisioning;
- App Store or Play Store certification.

If signing is added:

- keep keys/certificates/profiles in an appropriate secret system, never Git;
- use least-privilege workflow permissions;
- prevent untrusted PR code from receiving signing secrets;
- document signing identity and verification instructions;
- prefer a dedicated release/signing design review or ADR.

## Browser security/deployment boundary

The browser target is local-first in the sense that contacts are persisted to browser-managed local storage by the application. Hosting the static WebAssembly assets still involves a web origin/server for application delivery.

The current ContactCore source does not add an account/cloud synchronization API. A future web deployment must not silently add telemetry, remote contact upload, or analytics and continue claiming the same privacy posture without explicit documentation/review.

Browser data can be removed by site-data clearing, private-mode teardown, policy, or storage eviction. Release notes should encourage explicit exports for important portable copies.

## Version and schema compatibility

Application version 2.0.12 and native SQLite schema version are separate. ContactCore rejects native databases with a schema version newer than the running build. Users moving between versions should retain verified native backups before incompatible schema changes.

Browser persistence has its own serialized-document/storage-version boundary and should be migrated deliberately if its representation changes in the future.

## Release notes for 2.0.12

At minimum mention:

- Windows x64/ARM64, Linux x64/ARM64, macOS Intel/Apple Silicon desktop targets;
- Android and iOS/iPadOS application heads and build-gate status;
- browser/WebAssembly target and IndexedDB persistence;
- native SQLite vs browser persistence/backup distinction;
- shared responsive UI for mobile/browser;
- rich repeated-field contact editor and identity behavior;
- unsaved-draft safety;
- duplicate review/survivor choice/stale-safe merge;
- CSV/vCard hardening/limitations;
- native verified backup/restore hardening;
- native database-key fail-closed encryption-provider boundary;
- exact-head CI/CodeQL state;
- browser package, six desktop packages, and checksums;
- unsigned/unnotarized/mobile-unprovisioned status;
- remaining manual device/browser/accessibility validation boundaries.

Never include real user data in screenshots/examples.

## Screenshots

Only publish screenshots made from disposable profiles with clearly fictional contacts. Review the entire image for OS notifications, usernames, paths, email addresses, or other personal information.

## Failed or partial release

If preflight fails due tag/version mismatch, correct the version/tag plan instead of bypassing preflight.

If a desktop, browser, Android, or iOS release gate fails, do not advertise that exact tag as fully verified across the platform matrix. Fix the issue and use a clean release strategy.

If a GitHub Release already contains partial assets, preserve a clear audit trail. Do not silently move an existing public semantic-version tag to unrelated code after users may have fetched it; prefer a corrected patch release where appropriate.

## Data rollback guidance

Native application rollback and **native data rollback** are separate. An older binary can reject a database migrated to a newer schema. The safe native data rollback is usually a verified backup created before an incompatible upgrade and a build that supports that backup.

Browser rollback likewise needs deliberate storage compatibility. Do not assume older browser code understands a future browser document version.

## Post-release checks

After publishing 2.0.12:

- confirm six desktop archives, browser ZIP, and `SHA256SUMS.txt` are attached;
- verify checksum entries;
- confirm generated release notes/changelog/platform matrix are accurate;
- smoke-test representative desktop downloads;
- host/test the browser artifact from a disposable origin/profile;
- record Android/iOS build status and any device/store validation performed separately;
- document platform-specific issues instead of hiding them;
- move roadmap/changelog/`what_changed.md` to the next milestone;
- never request public upload of a real contact database when diagnosing bugs.

See `ci-cd.md` and `platform-support.md` for workflow and platform details.
