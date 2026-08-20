# CI/CD

ContactCore uses GitHub Actions for core build/test checks, platform-specific Android/iOS/WebAssembly compilation, CodeQL analysis, and tag-driven release publishing. Workflows live under `.github/workflows/`.

The current source/application version is **2.0.12**, centralized in `Directory.Build.props`.

## Why CI is split

The complete `ContactCore.slnx` contains Android, iOS, Browser, Desktop, shared layers, and tests. Android/iOS/WebAssembly projects need .NET workloads that are not installed on every GitHub-hosted runner, and iOS compilation belongs on macOS.

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

1. checks out with `actions/checkout@v6`;
2. installs the SDK using `actions/setup-dotnet@v5` and `global.json`;
3. enables NuGet caching keyed from `Directory.Packages.props`;
4. restores `ContactCore.Core.slnx`;
5. runs `dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore`;
6. builds Release with `--no-restore`;
7. runs tests with XPlat Code Coverage and `--no-build`;
8. uploads `TestResults` when present, including after failures.

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

This is the compile gate for `net10.0-browser`, Avalonia.Browser, .NET/JavaScript interop, shared UI references, and browser repository code.

### `android-build`

Runner: `ubuntu-latest`.

The job:

```text
setup .NET from global.json
→ dotnet workload install android --skip-manifest-update
→ restore ContactCore.Android
→ Release build ContactCore.Android
```

It verifies the Android application head and transitive shared/native SQLite composition. It does not inject a private Android signing key.

### `ios-build`

Runner: `macos-latest`.

The job:

```text
setup .NET from global.json
→ dotnet workload install ios --skip-manifest-update
→ restore ContactCore.iOS
→ Release build ContactCore.iOS
```

macOS is used because iOS compilation requires the Apple development toolchain. The build gate does not invent Apple signing/provisioning credentials.

### CI concurrency

CI uses a concurrency group based on pull-request number or Git ref with `cancel-in-progress: true`. A new commit on the same PR cancels obsolete in-progress CI so the merge signal converges on the current head.

### CI permissions

The workflow requests only:

```text
contents: read
```

## CodeQL

`.github/workflows/codeql.yml` runs:

- on pushes to `main`;
- on pull requests targeting `main`;
- weekly Monday at `03:23` UTC (`23 3 * * 1`).

It analyzes C# on Ubuntu.

The sequence is:

1. checkout v6;
2. initialize CodeQL v4 for C#;
3. setup .NET v5 from `global.json`;
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

`.github/workflows/release.yml` triggers on tags matching:

```text
v*.*.*
```

The pattern alone is not sufficient to publish; version preflight must succeed.

## Release preflight

The `preflight` job:

1. checks out the tagged commit;
2. installs the SDK from `global.json`;
3. resolves `ContactCore.Desktop`'s `Version` through MSBuild;
4. verifies the tag exactly equals `v<Version>`;
5. exposes the resolved version to downstream jobs.

For this source tree the intended tag is:

```text
v2.0.12
```

A mismatched tag such as `v2.0.13` fails before publishing.

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

Each matrix job:

1. checks out the tag;
2. sets up .NET through `global.json`;
3. restores `ContactCore.Core.slnx`;
4. runs core tests in Release;
5. publishes `ContactCore.Desktop` for the matrix RID as self-contained/single-file-targeted output;
6. packages Windows output as ZIP or Linux/macOS output as tar.gz;
7. uploads the archive as a workflow artifact.

Unix output is packaged in tar.gz before upload so executable metadata is retained inside the archive.

Expected names for 2.0.12:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-win-arm64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-linux-arm64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
```

## Browser publishing

`browser-publish` runs on Ubuntu:

1. setup .NET;
2. install `wasm-tools`;
3. publish `ContactCore.Browser` in Release;
4. ZIP the complete static WebAssembly output;
5. upload `contactcore-v2.0.12-browser-wasm.zip`.

The browser ZIP is a static-hosting artifact. It must be deployed to a suitable HTTP(S) web host; it is not a desktop executable.

## Mobile release gate

`mobile-build-gate` has two matrix entries:

- Android on Ubuntu, `android` workload, `ContactCore.Android` Release build;
- iOS on macOS, `ios` workload, `ContactCore.iOS` Release build.

The final GitHub Release depends on this gate, so a tag should not publish desktop/browser assets while the mobile source heads are broken.

### Why mobile packages are not attached automatically

Production Android and Apple distribution requires private maintainer-controlled signing material. The repository intentionally does not contain fake, example-as-production, or real signing credentials.

Therefore v2.0.12 release automation treats Android/iOS as **build-gated source targets**, while desktop/browser artifacts are the automatically attachable unsigned packages.

When a secure secret/signing policy is added later, signed mobile packaging can be layered on top without weakening this boundary.

## Final release job

After `preflight`, `desktop-publish`, `browser-publish`, and `mobile-build-gate` succeed, `release`:

1. downloads and merges packaged workflow artifacts;
2. generates `SHA256SUMS.txt` using SHA-256 over `contactcore-*` packages;
3. prints the checksum list in workflow output;
4. creates/updates the GitHub Release with generated release notes;
5. attaches packaged desktop/browser artifacts plus the checksum file.

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

Release uses a tag-ref concurrency group with `cancel-in-progress: false`. An already-started release is not intentionally cancelled merely because another event appears for the same ref.

## Important release claims

Current automation must not be described as doing work it does not perform.

Desktop archives are not claimed as signed installers or notarized applications. Browser output is not a hosted service by itself. Android/iOS targets are not claimed as Play Store/App Store-certified packages. `SHA256SUMS.txt` provides byte-integrity comparison against the published checksum file; it is not a replacement for trusted platform code signing/notarization.

## SDK/package consistency

Development, CI, CodeQL, and release automation use `global.json` with SDK 10.0.100 plus `latestFeature` roll-forward and prereleases disabled.

Avalonia/mobile/browser package versions are centrally managed in `Directory.Packages.props`, including:

```text
Avalonia
Avalonia.Desktop
Avalonia.Android
Avalonia.iOS
Avalonia.Browser
Avalonia.Themes.Fluent
```

Do not add independent project-level package versions unless there is a deliberate, documented exception.

## Versioning policy

`Directory.Build.props` defines:

```text
VersionPrefix        2.0.12
Version              2.0.12
AssemblyVersion      2.0.12.0
FileVersion          2.0.12.0
InformationalVersion 2.0.12
```

When preparing another release, update source version metadata and release documentation before tagging. Keep preflight as the guard against tag/project divergence.

## Dependency automation

`.github/dependabot.yml` tracks configured package/workflow ecosystems. Automated update discovery is not compatibility approval; dependency PRs must pass the same review and platform CI gates.

## Pull-request merge gate

For changes touching production code/workflows, require the **exact final head** to have:

- core restore success on Ubuntu/Windows/macOS;
- core format success;
- core Release build success;
- all current core tests passing;
- Browser Release build success after `wasm-tools` installation;
- Android Release build success after Android workload installation;
- iOS Release build success on macOS after iOS workload installation;
- CodeQL with no unresolved newly introduced actionable finding;
- documentation aligned with the code;
- no real contact data, databases, exports, credentials, signing material, or private endpoints committed.

A green run for an older commit does not verify a newer documentation/code/workflow head.

## Diagnosing failures

### Core restore

Check `global.json`, central package versions, package availability, and project references. Inspect `Directory.Packages.props` before adding package-version attributes to individual projects.

### Format

```bash
dotnet format ContactCore.Core.slnx
```

Inspect resulting changes before committing them.

### Android workload/build

Confirm the stable .NET 10 SDK resolves, `dotnet workload list` includes Android after installation, and the Android SDK/toolchain is available. Do not “fix” a compile error by silently deleting the Android CI gate.

### iOS workload/build

Use the macOS job logs. Check .NET iOS workload/toolchain compatibility and Apple tooling. Distinguish compilation/toolchain failures from signing/provisioning failures; the current gate is not intended to perform store signing.

### Browser workload/build

Check `wasm-tools`, WebAssembly SDK errors, `[JSImport]` source generation, JavaScript host assets, and Avalonia Browser references. A browser compile failure is a first-class platform regression.

### Test failure

Use uploaded OS-specific `TestResults` where present. Reproduce with Release configuration and the relevant host OS when possible.

### CodeQL

Follow the data/control path to determine whether a finding is actionable. Prefer code changes over broad suppression. Any unavoidable suppression should be narrow and justified.

### Release preflight

If tag/version mismatch occurs, inspect `Directory.Build.props` and the pushed tag. Correct the release version/tag instead of weakening preflight.

### Desktop packaging

On Windows inspect `Compress-Archive` and publish output. On Linux/macOS inspect `tar`, paths, and executable metadata.

### Browser packaging

Ensure `dotnet publish` produced the expected static output and ZIP packaging starts from the publish directory rather than accidentally omitting `_framework` or `wwwroot` assets.

### Checksum/final release

If `sha256sum contactcore-*` sees no files, investigate upstream artifact naming/download. Do not publish a silently incomplete release.

## Workflow-change checklist

When changing GitHub Actions:

- preserve least-privilege permissions;
- keep maintained action major versions under review;
- preserve sensible timeouts/concurrency;
- keep source version tied to release tags;
- do not expose secrets to untrusted pull-request code;
- keep mobile signing credentials out of source;
- keep Unix executable packaging permission-safe;
- publish checksums for downloadable archives;
- keep generated artifacts free of user data;
- treat Android/iOS/Browser gates as first-class rather than optional decoration;
- update `platform-support.md`, `release.md`, `README.md`, `CHANGELOG.md`, and `what_changed.md` when platform behavior changes.
