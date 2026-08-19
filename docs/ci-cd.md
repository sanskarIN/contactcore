# CI/CD

ContactCore uses GitHub Actions for cross-platform build/test checks, CodeQL analysis, and tag-driven release publishing. The workflows live under `.github/workflows/`.

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

`.github/workflows/release.yml` runs when a tag matches `v*.*.*`.

The publish matrix targets:

| Runner | Runtime identifier |
|---|---|
| Windows | `win-x64` |
| Ubuntu | `linux-x64` |
| macOS | `osx-x64` |
| macOS | `osx-arm64` |

For each target the workflow:

1. checks out the tag;
2. installs .NET 10.x;
3. restores the solution;
4. runs the full solution tests in Release;
5. publishes `ContactCore.Desktop` as a self-contained, single-file application for the matrix RID;
6. uploads the publish directory as a GitHub Actions artifact.

After all publish jobs succeed, the `release` job downloads all artifacts and creates/updates the GitHub Release using generated release notes and the produced files.

The release workflow requires `contents: write` so it can create release assets.

## Important release claims

The workflow currently creates self-contained files, but repository documentation must **not** describe them as code-signed, notarized, installer-packaged, or store-certified unless those steps are explicitly added and verified. A single-file binary can still trigger operating-system reputation/security warnings when unsigned.

## SDK consistency

Development and CI use the repository SDK policy in `global.json`. The release workflow currently requests `10.0.x` directly rather than `global-json-file`. Maintainers should keep those compatible and preferably converge the release workflow on the same pinned SDK policy when practical.

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

If repository branch-protection settings do not formally require these checks, maintainers should still apply this policy manually.

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

### Release failure

Do not retag over an ambiguous partial release without understanding what assets were produced. Fix the workflow/code, verify normal CI, and use a clean version/tag policy consistent with the changelog.

## Workflow-change checklist

When changing GitHub Actions:

- use least-privilege permissions;
- pin to maintained major action versions and review upstream release/security notices;
- preserve timeouts;
- preserve or improve concurrency controls;
- avoid exposing secrets to untrusted pull-request code;
- keep generated artifacts free of real user data;
- document newly added release/signing requirements;
- update this file, `docs/release.md`, and `what_changed.md`.
