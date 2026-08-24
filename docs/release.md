# Release

ContactCore uses semantic Git tags matching `v*.*.*` to trigger GitHub Actions publishing. A release must represent a verified repository state, not merely a tag that happens to create artifacts.

The prepared maintenance source version is **2.0.13** and the intended future maintenance tag is **`v2.0.13`**. The authoritative **2.0.12** integration must complete first; do not publish 2.0.13 directly from the preparation branch before that baseline is merged/reconciled.

For repeatable manual evidence, use [`release-smoke-test.md`](release-smoke-test.md). Copy it for each release candidate and bind the completed record to the exact tested SHA, artifacts, platform versions, and disposable test profile.

## Version source of truth

Application version metadata is centralized in `Directory.Build.props`:

```text
VersionPrefix        2.0.13
Version              2.0.13
AssemblyVersion      2.0.13.0
FileVersion          2.0.13.0
InformationalVersion 2.0.13
```

The release workflow resolves the desktop project's `Version` through MSBuild and requires:

```text
GITHUB_REF_NAME == "v" + Version
```

For this maintenance source tree, `v2.0.13` is accepted; a mismatched tag such as `v2.0.14` is rejected during preflight.

Application version and SQLite schema version remain separate concepts.

## 2.0.12 prerequisite

The branch `release/contactcore-2.0.13` was created from the exact authoritative 2.0.12 source head so maintenance preparation could continue without modifying PR #4. Before opening/merging the 2.0.13 PR:

1. complete the exact-head 2.0.12 CI + CodeQL gate;
2. merge PR #4 to `main` using history-preserving merge semantics;
3. reconcile the 2.0.13 branch with the resulting `main` merge commit;
4. verify the 2.0.13 PR diff contains only intended maintenance changes;
5. run the full exact-head matrix again on 2.0.13.

The 2.0.13 maintenance work must not be used as a shortcut around an incomplete 2.0.12 release gate.

## Release-support matrix

### Automated downloadable packages

| Platform | Target | Runner | Release package |
|---|---|---|---|
| Windows x64 | `win-x64` | `windows-latest` | `contactcore-v2.0.13-win-x64.zip` |
| Windows ARM64 | `win-arm64` | `windows-latest` | `contactcore-v2.0.13-win-arm64.zip` |
| Linux x64 | `linux-x64` | `ubuntu-latest` | `contactcore-v2.0.13-linux-x64.tar.gz` |
| Linux ARM64 | `linux-arm64` | `ubuntu-latest` | `contactcore-v2.0.13-linux-arm64.tar.gz` |
| macOS Intel | `osx-x64` | `macos-latest` | `contactcore-v2.0.13-osx-x64.tar.gz` |
| macOS Apple Silicon | `osx-arm64` | `macos-latest` | `contactcore-v2.0.13-osx-arm64.tar.gz` |
| Browser/WebAssembly | `net10.0-browser` | `ubuntu-latest` | `contactcore-v2.0.13-browser-wasm.zip` |

The final release job generates `SHA256SUMS.txt` for packaged release assets.

### Build-gated mobile targets

| Platform | Project | Runner | Release gate |
|---|---|---|---|
| Android | `ContactCore.Android` / `net10.0-android`, RID `android-arm64` | `ubuntu-latest` | install Android workload + Release build |
| iPhone/iPad | `ContactCore.iOS` / `net10.0-ios`, RID `iossimulator-arm64` | `macos-latest` | select compatible Xcode + install iOS workload + unsigned simulator Release build |

The GitHub iOS gate explicitly selects `/Applications/Xcode_26.0.app/Contents/Developer` before workload installation. The iOS project applies `TrimMode=copy` only for simulator runtime identifiers. Application-owned AOT/trim hazards remain removed in source through generated JSON metadata and typed compiled Avalonia bindings.

This simulator gate validates source/runtime integration. Production device trimming, signing, provisioning, TestFlight/App Store validation, and store credentials require a separate protected maintainer-controlled distribution pipeline.

## Workflow dependency posture for 2.0.13

The maintenance line refreshes workflow dependencies to:

- `actions/checkout@v7`;
- `actions/setup-dotnet@v6`;
- `github/codeql-action@v4`;
- `actions/upload-artifact@v7`;
- `actions/download-artifact@v8`;
- `softprops/action-gh-release@v3`.

`Microsoft.NET.Test.Sdk` is updated to **18.9.0**. These changes must pass the same cross-platform gate before 2.0.13 can merge.

## Trigger

A push of a tag matching:

```text
v*.*.*
```

starts `.github/workflows/release.yml`. Preflight rejects tags that do not equal the project version.

## Pre-release checklist for 2.0.13

Before creating `v2.0.13`:

1. 2.0.12 has completed its intended verified integration/release checkpoint.
2. `main` contains the intended 2.0.13 source and documentation after the maintenance PR merge.
3. The exact final 2.0.13 candidate has successful core CI on Ubuntu, Windows, and macOS.
4. Browser, Android, and iOS build jobs succeed for that exact candidate.
5. CodeQL for that exact candidate has no unresolved newly introduced actionable issue.
6. `Directory.Build.props` resolves project version `2.0.13`.
7. `CHANGELOG.md`, `README.md`, roadmap, CI/release docs, repository reference, and `what_changed.md` match the actual maintenance line.
8. Test SDK 18.9.0 and checkout/setup-dotnet action-generation changes are verified rather than assumed compatible.
9. No real contact data, database, backup, export, `.env`, key, certificate, keystore, provisioning profile, signing material, or private endpoint is tracked.
10. A copy of `docs/release-smoke-test.md` is completed for the exact candidate, or intentionally unexecuted sections are explicitly marked.
11. Manual/browser/device checks are not represented as completed unless actually executed.
12. Signing/notarization/store status is stated accurately.

## Local core quality pass

```bash
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

Platform builds require their workload and explicit runtime identifier where CI uses one.

### Browser

```bash
dotnet workload install wasm-tools
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

### Android

```bash
dotnet workload install android
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj -r android-arm64
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release -r android-arm64 --no-restore
```

### iOS simulator

```bash
sudo xcode-select -s /Applications/Xcode_26.0.app/Contents/Developer
xcodebuild -version
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj -r iossimulator-arm64
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release -r iossimulator-arm64 --no-restore
```

Local success is useful but does not replace GitHub's exact-head synthetic-merge matrix.

## Tagging 2.0.13

Only after the verified 2.0.13 maintenance commit is on `main`:

```bash
git checkout main
git pull --ff-only
git tag -a v2.0.13 -m "ContactCore v2.0.13"
git push origin v2.0.13
```

Do not create the public release tag from the isolated preparation branch.

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

### 3. Browser publish

- install `wasm-tools`;
- publish `ContactCore.Browser` in Release;
- ZIP the full static output;
- upload `contactcore-v2.0.13-browser-wasm.zip`.

### 4. Mobile build gate

- Ubuntu installs Android workload and builds `ContactCore.Android` for `android-arm64`;
- macOS selects Xcode 26.0, installs iOS workload, and builds `ContactCore.iOS` for `iossimulator-arm64` using the simulator-only trim policy.

The final release depends on this gate even though mobile store packages are not attached automatically.

### 5. Final GitHub Release

After all build/publish gates succeed:

- download/merge packaged artifacts;
- generate `SHA256SUMS.txt`;
- publish generated release notes;
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

Checksums support byte-integrity comparison; they do not replace trusted platform code signing.

Use `release-smoke-test.md` to record exact artifact names, checksum evidence, platform versions, and pass/fail notes.

### Desktop smoke checks

With fictional/disposable data verify startup, database creation, rich create/edit/save, search/favorites/archive/A-Z, duplicate review, themes/settings, CSV/vCard import/export, backup/restore, and destructive-action safety.

### Browser smoke checks

Serve the browser publish over HTTP(S) and verify WebAssembly boot, IndexedDB persistence across reloads, edit/search/filter/duplicate flows, import/export, preferences, capability boundaries, and useful behavior when storage is blocked. Use a disposable browser profile.

### Android/iOS smoke checks

A compile gate is not device testing. Before public store distribution, use representative devices/simulators with fictional data and verify touch layout, input behavior, persistence, file pickers, orientation, accessibility, and lifecycle/background behavior as applicable.

## Signing and notarization

The public workflow does **not** implement or claim:

- Authenticode signing;
- Windows installer signing;
- Apple Developer ID signing/notarization;
- Linux package signing;
- Play Store production signing;
- iOS/iPadOS distribution signing/provisioning;
- App Store or Play Store certification.

If signing is added, keep all keys/certificates/profiles in an appropriate secret system, use least privilege, keep secrets unavailable to untrusted PR code, and document verification instructions.

## Browser security/deployment boundary

The browser target persists contacts in browser-managed local storage and does not add an account/cloud synchronization API. Static WebAssembly hosting still requires a web origin/server. Browser site data can be removed by clearing data, private-session teardown, policy, or eviction, so portable exports remain important.

## Version and schema compatibility

Application version **2.0.13** and native SQLite schema version are separate. ContactCore rejects native databases with a schema version newer than the running build. Browser persistence has its own serialized-document/storage-version boundary.

The 2.0.13 maintenance preparation does not introduce a schema migration.

## Release notes for 2.0.13

At minimum mention:

- maintenance nature of the release;
- Test SDK 18.9.0;
- checkout v7 and setup-dotnet v6 workflow refresh;
- unchanged cross-platform source/build support and persistence model;
- exact-head CI/CodeQL state;
- six desktop packages, Browser package, and checksums;
- unsigned/unnotarized/mobile-unprovisioned status;
- remaining manual device/browser/accessibility validation boundaries.

Do not imply that larger roadmap features were added to this patch release.

## Screenshots

Only publish screenshots from disposable profiles with clearly fictional contacts. Review the entire image for notifications, usernames, paths, email addresses, or other personal information.

## Failed or partial release

If preflight fails due tag/version mismatch, fix the version/tag plan rather than bypassing preflight. If a desktop, browser, Android, iOS, or CodeQL gate fails, fix the issue and verify a clean candidate rather than advertising the tag as fully verified.

Do not silently move a public semantic-version tag after users may have fetched it; prefer a corrected patch release where appropriate.

## Data rollback guidance

Application rollback and data rollback are separate. Retain verified native backups before incompatible schema changes. Browser rollback also requires deliberate storage compatibility.

## Post-release checks

After publishing 2.0.13:

- confirm six desktop archives, Browser ZIP, and `SHA256SUMS.txt`;
- verify checksum entries;
- confirm release notes/changelog/platform matrix are accurate;
- smoke-test representative desktop downloads;
- host/test the Browser artifact from a disposable origin/profile;
- record Android/iOS build status and device/store validation separately;
- archive the smoke-test record;
- document platform-specific issues;
- move roadmap/changelog/`what_changed.md` to the next milestone;
- never request public upload of a real contact database when diagnosing bugs.

See `ci-cd.md`, `platform-support.md`, and `release-smoke-test.md` for workflow, platform, and manual-verification details.
