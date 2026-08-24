# CI/CD

ContactCore uses GitHub Actions for core build/test checks, platform-specific Android/iOS/WebAssembly compilation, CodeQL analysis, and tag-driven release publishing. Workflows live under `.github/workflows/`.

The current maintenance source/application version is **2.0.13**, centralized in `Directory.Build.props`. The 2.0.13 line is being prepared separately while the authoritative 2.0.12 integration completes.

## Why CI is split

The complete `ContactCore.slnx` contains Android, iOS, Browser, Desktop, shared layers, and five behavioral test projects. Android/iOS/WebAssembly projects need .NET workloads that are not installed on every GitHub-hosted runner, and iOS compilation belongs on macOS.

For that reason:

- `ContactCore.Core.slnx` is the workload-free quality solution used for three-OS restore/format/build/test and CodeQL;
- platform heads are built by dedicated jobs that install exactly the needed workload;
- `ContactCore.slnx` remains the complete repository solution for IDE/source organization.

This prevents an ordinary Linux core job from failing merely because an Apple workload is absent while still making every platform head an explicit merge gate.

## CI workflow

`.github/workflows/ci.yml` runs on pushes to `main` and pull requests targeting `main`.

### `core-build-test`

Matrix:

- `ubuntu-latest`
- `windows-latest`
- `macos-latest`

`fail-fast: false` means one operating-system failure does not cancel the other matrix variants. Each matrix job:

1. checks out with `actions/checkout@v7`;
2. installs the SDK using `actions/setup-dotnet@v6` and `global.json`;
3. enables NuGet caching keyed from `Directory.Packages.props`;
4. restores `ContactCore.Core.slnx`;
5. runs `dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore`;
6. builds Release with `--no-restore`;
7. runs all five test projects with XPlat Code Coverage and `--no-build`;
8. uploads `TestResults` when present, including after failures.

Every test project references the centrally pinned `coverlet.collector`. The 2.0.13 maintenance line uses `Microsoft.NET.Test.Sdk` **18.9.0** with the existing MSTest/coverage stack.

Core test artifacts are named by runner OS and retained for 14 days.

### `browser-build`

Runner: `ubuntu-latest`.

The job:

```text
setup .NET from global.json
→ dotnet workload install wasm-tools --skip-manifest-update
→ restore ContactCore.Browser
→ Release build ContactCore.Browser
```

This is the compile gate for `net10.0-browser`, Avalonia.Browser, .NET/JavaScript interop, source-generated `System.Text.Json` metadata, typed compiled shared-UI bindings, and browser repository code. Trimming/AOT diagnostics are treated as real build signals rather than broadly suppressed.

### `android-build`

Runner: `ubuntu-latest`.

The job:

```text
setup .NET from global.json
→ dotnet workload install android --skip-manifest-update
→ restore ContactCore.Android -r android-arm64
→ Release build ContactCore.Android -r android-arm64
```

It verifies the Android application head and transitive shared/native SQLite composition. The explicit RID avoids host-RID leakage. The gate does not inject a private Android signing key.

### `ios-build`

Runner: `macos-latest`.

The current .NET iOS workload accepts the Xcode 26.0 toolchain, while the rolling GitHub macOS image can default to a newer Xcode. CI therefore makes the toolchain choice explicit before installing/building the workload:

```text
setup .NET from global.json
→ sudo xcode-select -s /Applications/Xcode_26.0.app/Contents/Developer
→ xcodebuild -version
→ dotnet workload install ios --skip-manifest-update
→ restore ContactCore.iOS -r iossimulator-arm64
→ Release build ContactCore.iOS -r iossimulator-arm64
```

macOS is used because iOS compilation requires the Apple development toolchain. The explicit simulator RID verifies source/runtime integration without inventing Apple signing/provisioning credentials.

`ContactCore.iOS.csproj` applies `TrimMode=copy` only to iOS simulator runtime identifiers. Application-owned trim hazards are still removed at source: native preferences use source-generated JSON metadata and the shared `MainView` uses typed compiled bindings. This scoped simulator policy is not a claim that a production device/App Store artifact has been trimmed, signed, provisioned, notarized, or certified.

### CI concurrency

CI uses a concurrency group based on pull-request number or Git ref with `cancel-in-progress: true`. A new commit on the same PR cancels obsolete in-progress CI so the merge signal converges on the current head.

### CI permissions

The workflow requests only:

```text
contents: read
```

## CodeQL

`.github/workflows/codeql.yml` runs on pushes to `main`, pull requests targeting `main`, and weekly Monday at `03:23` UTC (`23 3 * * 1`).

The sequence is:

1. checkout v7;
2. initialize CodeQL v4 for C#;
3. setup .NET v6 from `global.json`;
4. restore `ContactCore.Core.slnx`;
5. Release build `ContactCore.Core.slnx`;
6. run CodeQL analysis.

CodeQL deliberately uses the workload-free core solution. Android/iOS/WebAssembly compilation is already enforced by CI and should not force CodeQL's Linux analysis job to install unrelated workloads.

Permissions are limited to:

```text
contents: read
security-events: write
```

CodeQL uses the same obsolete-run cancellation principle as CI.

## Release workflow

`.github/workflows/release.yml` triggers on tags matching `v*.*.*`. The pattern alone is not sufficient to publish; version preflight must succeed.

The 2.0.13 maintenance branch uses:

- `actions/checkout@v7`;
- `actions/setup-dotnet@v6`;
- `actions/upload-artifact@v7`;
- `actions/download-artifact@v8`;
- `github/codeql-action@v4` in CodeQL;
- `softprops/action-gh-release@v3` for final release creation.

## Release preflight

The `preflight` job:

1. checks out the tagged commit;
2. installs the SDK from `global.json`;
3. resolves `ContactCore.Desktop`'s `Version` through MSBuild;
4. verifies the tag exactly equals `v<Version>`;
5. exposes the resolved version to downstream jobs.

For the prepared maintenance source tree the intended future tag is:

```text
v2.0.13
```

Do not publish that tag until 2.0.12 has been integrated/released through its verified checkpoint and the 2.0.13 branch has passed its own exact-head gate. A mismatched tag such as `v2.0.14` fails before publishing.

## Desktop publish matrix

`desktop-publish` produces six native desktop archives:

| Runner | Runtime identifier | Package |
|---|---|---|
| Windows | `win-x64` | `.zip` |
| Windows | `win-arm64` | `.zip` |
| Ubuntu | `linux-x64` | `.tar.gz` |
| Ubuntu | `linux-arm64` | `.tar.gz` |
| macOS | `osx-x64` | `.tar.gz` |
| macOS | `osx-arm64` | `.tar.gz` |

Each matrix job restores the core solution, runs core tests, publishes the Desktop RID as self-contained/single-file-targeted output, packages it, and uploads the archive. Unix output is packaged in tar.gz before upload so executable metadata is retained.

Expected names for 2.0.13:

```text
contactcore-v2.0.13-win-x64.zip
contactcore-v2.0.13-win-arm64.zip
contactcore-v2.0.13-linux-x64.tar.gz
contactcore-v2.0.13-linux-arm64.tar.gz
contactcore-v2.0.13-osx-x64.tar.gz
contactcore-v2.0.13-osx-arm64.tar.gz
```

## Browser publishing

`browser-publish` installs `wasm-tools`, publishes `ContactCore.Browser` in Release, ZIPs the full static output, and uploads:

```text
contactcore-v2.0.13-browser-wasm.zip
```

The browser ZIP is static-hosting output, not a hosted website by itself.

## Mobile release gate

`mobile-build-gate` contains:

- Android on Ubuntu, `android` workload, `android-arm64`, `ContactCore.Android` Release build;
- iOS on macOS, `ios` workload, `iossimulator-arm64`, `ContactCore.iOS` Release build.

The iOS matrix entry selects `/Applications/Xcode_26.0.app/Contents/Developer` before workload installation and uses the simulator-only trim policy documented above. Production device trimming/signing remains outside this unsigned source gate.

The final GitHub Release depends on the mobile gate.

### Why mobile packages are not attached automatically

Production Android and Apple distribution requires private maintainer-controlled signing material. The repository intentionally does not contain fake, example-as-production, or real signing credentials.

Therefore Android/iOS remain build-gated source targets while desktop/browser artifacts are the automatically attachable unsigned packages.

## Final release job

After `preflight`, `desktop-publish`, `browser-publish`, and `mobile-build-gate` succeed, `release` downloads the packaged artifacts, generates `SHA256SUMS.txt`, prints the checksum list, creates/updates the GitHub Release with generated notes, and attaches the desktop/browser packages plus checksums.

## Release permissions

Workflow default:

```text
contents: read
```

Only the final release-creation job receives:

```text
contents: write
```

The mobile build gate does not receive repository write permission or signing secrets by default.

## Release concurrency

Release uses a tag-ref concurrency group with `cancel-in-progress: false`.

## Important release claims

Desktop archives are not claimed as signed installers or notarized applications. Browser output is not a hosted service by itself. Android/iOS targets are not claimed as Play Store/App Store-certified packages. `SHA256SUMS.txt` provides byte-integrity comparison, not trusted platform code-signing identity.

## SDK/package consistency

Development, CI, CodeQL, and release automation use `global.json` with SDK 10.0.100 plus `latestFeature` roll-forward and prereleases disabled.

Central package management remains in `Directory.Packages.props`. The 2.0.13 maintenance-specific test dependency is:

```text
Microsoft.NET.Test.Sdk 18.9.0
```

Do not add independent project-level package versions unless deliberately documented.

## Versioning policy

`Directory.Build.props` on the maintenance branch defines:

```text
VersionPrefix        2.0.13
Version              2.0.13
AssemblyVersion      2.0.13.0
FileVersion          2.0.13.0
InformationalVersion 2.0.13
```

Keep release preflight as the guard against tag/project divergence.

## Dependency automation

`.github/dependabot.yml` tracks configured package/workflow ecosystems. Automated update discovery is not compatibility approval. Dependabot PR #11 (Test SDK 18.9.0), #6 (checkout v7), and #7 (setup-dotnet v6) have been manually reconciled into the isolated 2.0.13 branch as separate reviewable commits; they should be closed as superseded only after the corresponding maintenance line is safely established.

## Pull-request merge gate

For changes touching production code/workflows, require the **exact final head** to have:

- core restore success on Ubuntu/Windows/macOS;
- core format success;
- core Release build success;
- all five behavioral test projects passing with coverage collection;
- Browser Release build success after `wasm-tools` installation;
- Android `android-arm64` Release build success;
- iOS `iossimulator-arm64` Release build success using compatible Xcode/simulator policy;
- CodeQL with no unresolved newly introduced actionable finding;
- documentation aligned with the code;
- no real contact data, databases, exports, credentials, signing material, or private endpoints committed.

A green run for an older commit does not verify a newer head.

## Diagnosing failures

### Core restore

Check `global.json`, central package versions, package availability, and project references.

### Format

```bash
dotnet format ContactCore.Core.slnx
```

### Coverage collector/test SDK

If coverage collection fails, verify every test project still references `coverlet.collector` and confirm Test SDK 18.9.0 compatibility before weakening the shared test command.

### Android workload/build

Confirm the .NET 10 SDK, Android workload/toolchain, and explicit `android-arm64` RID. Do not delete the Android gate to hide a compile error.

### iOS workload/build

Inspect `xcodebuild -version`, `xcode-select -p`, workload compatibility, and the simulator-only `TrimMode=copy` boundary. Application-owned trim hazards must still be fixed in source rather than broadly suppressed. Distinguish simulator integration failures from production signing/device-pipeline work.

### Browser workload/build

Check `wasm-tools`, `[JSImport]` generation, generated JSON metadata, compiled XAML bindings, trimming/AOT diagnostics, host assets, and Avalonia Browser references.

### CodeQL

Determine whether a finding is actionable from its actual data/control path. Prefer code fixes over broad suppression.

### Release preflight

Correct version/tag divergence instead of weakening preflight.

### Packaging/checksums

If an expected package is absent or checksum generation sees no files, fix upstream artifact generation/naming rather than publishing a partial release.

## Workflow-change checklist

When changing GitHub Actions:

- preserve least-privilege permissions;
- keep maintained action major versions under review;
- preserve sensible timeouts/concurrency;
- keep source version tied to release tags;
- do not expose secrets to untrusted pull-request code;
- keep mobile signing credentials out of source;
- preserve Unix executable metadata;
- publish checksums for downloadable archives;
- keep generated artifacts free of user data;
- treat Android/iOS/Browser gates as first-class;
- retain explicit mobile RIDs and compatible Xcode selection unless deliberately updated;
- update `platform-support.md`, `release.md`, `README.md`, `CHANGELOG.md`, and `what_changed.md` when behavior changes.
