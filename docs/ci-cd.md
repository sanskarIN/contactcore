# CI/CD

ContactCore uses GitHub Actions for cross-platform build/test checks, CodeQL analysis, and tag-driven release publishing. The workflows live under `.github/workflows/`.

The current source/application version is **2.0.12**, centralized in `Directory.Build.props`.

## CI workflow

`.github/workflows/ci.yml` runs on pushes to `main` and pull requests targeting `main`.

The `build-test` job uses a three-OS matrix:

- `ubuntu-latest`
- `windows-latest`
- `macos-latest`

`fail-fast: false` means one operating-system failure does not cancel the other matrix variants. Each job has a 25-minute timeout.

The workflow:

1. checks out the repository;
2. installs the SDK from `global.json`;
3. enables the setup-dotnet package cache keyed through `Directory.Packages.props`;
4. restores `ContactCore.slnx`;
5. runs `dotnet format ... --verify-no-changes --no-restore`;
6. builds Release with `--no-restore`;
7. runs the complete solution tests with coverage collection and `--no-build`;
8. uploads `TestResults` as an artifact even when earlier test execution fails, when files are present.

Test-result artifacts are named by operating system and retained for 14 days.

### Concurrency

CI uses a concurrency group based on pull-request number or Git ref and `cancel-in-progress: true`. A newer commit on the same PR cancels an obsolete in-progress CI run so feedback converges on the latest branch head.

### Permissions

CI requests only `contents: read`.

## CodeQL

`.github/workflows/codeql.yml` runs:

- on pushes to `main`;
- on pull requests targeting `main`;
- weekly on Monday at `03:23` UTC (`23 3 * * 1`).

It analyzes C# on Ubuntu with a 30-minute timeout.

The job checks out code, initializes CodeQL, installs the SDK from `global.json`, restores the solution, performs a Release build, and invokes CodeQL analysis.

Permissions are limited to `contents: read` and `security-events: write`, the latter being required to publish analysis results.

Like CI, CodeQL cancels obsolete runs for the same PR/ref.

## Release workflow

`.github/workflows/release.yml` runs when a tag matches `v*.*.*`, but syntactic tag matching alone is no longer enough to publish.

### Release preflight

The `preflight` job:

1. checks out the tagged commit;
2. installs the SDK from `global.json`;
3. resolves `ContactCore.Desktop`'s `Version` through MSBuild;
4. verifies that the pushed tag exactly equals `v<Version>`;
5. exposes the resolved version to downstream jobs.

For the current source tree the only correct release tag is:

```text
v2.0.12
```

A tag such as `v2.0.13` fails preflight rather than publishing mismatched binaries.

### Publish matrix

The publish matrix targets:

| Runner | Runtime identifier | Package |
|---|---|---|
| Windows | `win-x64` | `.zip` |
| Ubuntu | `linux-x64` | `.tar.gz` |
| macOS | `osx-x64` | `.tar.gz` |
| macOS | `osx-arm64` | `.tar.gz` |

Each matrix job:

1. checks out the tag;
2. installs the SDK from `global.json`;
3. restores the solution;
4. runs the full solution tests in Release;
5. publishes `ContactCore.Desktop` as a self-contained, single-file-targeted application for the matrix RID;
6. packages Windows output as a ZIP and Linux/macOS output as a tar.gz archive;
7. uploads the packaged archive as a GitHub Actions artifact.

Packaging Unix output before `actions/upload-artifact` preserves executable metadata inside the tar archive instead of depending on Actions artifact permission preservation.

The resulting 2.0.12 package names are:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
```

### Final release job

After all publish jobs succeed, the `release` job:

1. downloads and merges the four packaged artifacts into one directory;
2. generates `SHA256SUMS.txt` with SHA-256 hashes of `contactcore-*` archives;
3. prints the checksums in the job output;
4. creates/updates the GitHub Release with generated release notes;
5. attaches the four archives and checksum file.

### Release permissions

The workflow defaults to:

```text
contents: read
```

Only the final `release` job receives:

```text
contents: write
```

This follows least privilege more closely than granting write permission to test/publish jobs.

### Release concurrency

The release workflow uses a tag-ref concurrency group with `cancel-in-progress: false`. A release that has started is not intentionally cancelled merely because another event for the same ref appears.

## Important release claims

The workflow creates self-contained/single-file-targeted publishes and packages them for download, but repository documentation must **not** describe them as code-signed, notarized, installer-packaged, store-certified, or cryptographically authenticated binaries unless those steps are explicitly added and verified.

`SHA256SUMS.txt` provides byte-integrity checking relative to the published checksum file; it is not a substitute for trusted code signing/notarization.

## SDK consistency

Development, CI, CodeQL build preparation, and release automation all use the repository SDK policy in `global.json`. This removes the previous release-only `10.0.x` selector and reduces CI/release SDK drift.

The current baseline is SDK `10.0.100` with `latestFeature` roll-forward and prereleases disabled.

## Versioning policy

`Directory.Build.props` currently defines:

```text
VersionPrefix        2.0.12
Version              2.0.12
AssemblyVersion      2.0.12.0
FileVersion          2.0.12.0
InformationalVersion 2.0.12
```

When preparing the next release, update this metadata and the changelog/docs before tagging. The release preflight should remain the guard preventing tag/project-version divergence.

## Dependency automation

`.github/dependabot.yml` tracks dependency updates for the package/workflow ecosystems configured there. Dependency PRs must still pass the same normal review and CI rules; automated version discovery is not equivalent to compatibility approval.

## Pull-request quality gate

Before merging code-changing work, reviewers should require at minimum:

- restore succeeds;
- format verification succeeds;
- Release build succeeds;
- all solution tests pass on the supported CI matrix;
- CodeQL reports no unresolved newly introduced actionable finding;
- documentation is updated for changed behavior;
- no real contact data, databases, exports, credentials, signing material, or private endpoints were committed.

For release-preparation work, additionally verify the exact final branch/main commit after version/documentation/workflow edits. A green result for an earlier commit is not evidence for a newer head.

## Diagnosing failures

### Restore failure

Check package versions, NuGet availability, SDK compatibility, and project references. Because versions are centrally managed, inspect `Directory.Packages.props` before adding package-version attributes to individual project files.

### Format failure

Run:

```bash
dotnet format ContactCore.slnx
```

Then inspect and commit only intended formatting changes.

### Build failure on one OS

Treat it as a platform-compatibility issue until shown otherwise. Avalonia/storage/native SQLite behavior can differ by runner. Avoid suppressing one matrix leg merely to obtain a green badge.

### Test failure

Use the uploaded OS-specific `TestResults` artifacts where available. Reproduce locally with the same Release configuration and, when relevant, the same operating system.

### CodeQL finding

Follow the data/control path to determine whether the finding is exploitable, false-positive, or defense-in-depth. Prefer code changes over blanket suppressions. Any necessary suppression should be narrow, justified in code/review, and revisited later.

### Release preflight failure

If the message says the tag does not match the project version:

- inspect `Directory.Build.props`;
- inspect the pushed tag;
- decide which version is actually intended;
- correct the release preparation instead of weakening/removing the check.

For 2.0.12, the tag must be exactly `v2.0.12`.

### Packaging failure

On Windows, inspect the `Compress-Archive` step and publish-directory contents. On Linux/macOS, inspect the `tar` step, path, and file permissions. Do not switch Unix packaging to an approach that loses executable metadata without understanding the consequence.

### Checksum/release failure

The final job expects all publish jobs to succeed and all packaged files to be downloaded. If `sha256sum contactcore-*` finds no files, investigate artifact naming/download rather than publishing a checksum-less partial release silently.

### Partial release

Do not retag over an ambiguous public partial release without understanding what assets were produced. Fix workflow/code, verify normal CI, and use a clean version/tag policy consistent with the changelog.

## Workflow-change checklist

When changing GitHub Actions:

- use least-privilege permissions;
- pin to maintained major action versions and review upstream release/security notices;
- preserve sensible timeouts;
- preserve or improve concurrency controls;
- keep release tags tied to source version metadata;
- avoid exposing secrets to untrusted pull-request code;
- package Unix executables in a permission-preserving format;
- publish checksums for release archives;
- keep generated artifacts free of real user data;
- document newly added signing/notarization requirements honestly;
- update this file, `docs/release.md`, `README.md`, `CHANGELOG.md`, and `what_changed.md`.
