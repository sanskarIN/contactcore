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

The release workflow resolves the desktop project's `Version` through MSBuild before any matrix publishing begins. It then requires:

```text
GITHUB_REF_NAME == "v" + Version
```

For this source tree, `v2.0.12` is accepted; a mismatched tag such as `v2.0.13` is rejected during preflight.

Application version and SQLite schema version remain separate concepts.

## Current automated targets

The release workflow publishes `ContactCore.Desktop` as a self-contained, single-file-targeted application for:

| Platform | RID | Runner | Release package |
|---|---|---|---|
| Windows x64 | `win-x64` | `windows-latest` | `contactcore-v2.0.12-win-x64.zip` |
| Linux x64 | `linux-x64` | `ubuntu-latest` | `contactcore-v2.0.12-linux-x64.tar.gz` |
| macOS Intel | `osx-x64` | `macos-latest` | `contactcore-v2.0.12-osx-x64.tar.gz` |
| macOS Apple Silicon | `osx-arm64` | `macos-latest` | `contactcore-v2.0.12-osx-arm64.tar.gz` |

The final release job also creates `SHA256SUMS.txt` containing SHA-256 checksums for all packaged archives.

The workflow does not currently publish Windows ARM64, Linux ARM64, installers, app-store packages, or platform-specific package-manager formats.

## Trigger

A push of a tag matching:

```text
v*.*.*
```

starts `.github/workflows/release.yml`.

The preflight version check then rejects any syntactically matching tag that does not equal the project version.

## Pre-release checklist for 2.0.12

Before creating `v2.0.12`:

1. `main` contains the intended 2.0.12 changes.
2. The exact final `main` commit has successful CI on Windows, Ubuntu, and macOS.
3. CodeQL for that exact commit has no unresolved newly introduced actionable issue.
4. `Directory.Build.props` resolves project version `2.0.12`.
5. `CHANGELOG.md` contains the 2.0.12 release section.
6. `README.md`, `docs/README.md`, user guide, architecture, UI, data/storage/security/testing/release docs match actual code.
7. `what_changed.md` records the exact final verification state rather than an older green commit.
8. No real contact data, database, backup, export, `.env`, key, certificate/private signing material, or private endpoint is tracked.
9. Any schema migration has upgrade tests and restore compatibility review.
10. Import/export changes have round-trip/malformed-input/privacy tests.
11. Full rich-contact editing has been smoke-tested with fictional data.
12. Both duplicate-survivor directions and cancellation/confirmation behavior have been manually exercised with fictional contacts.
13. Backup creation and restore have been tested against a disposable profile.
14. Permanent-delete and unsaved-draft discard behavior have been checked.
15. Known limitations are present in the changelog/release notes.

## Local quality pass

From a clean checkout when possible:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build
```

Local success is valuable but does not replace the GitHub cross-platform matrix.

## Tagging 2.0.12

After the verified 2.0.12 commit is on `main`:

```bash
git checkout main
git pull --ff-only
git tag -a v2.0.12 -m "ContactCore v2.0.12"
git push origin v2.0.12
```

Do not create the release tag from the audit branch before the intended commit is merged to `main` unless the project deliberately chooses a branch-based release policy.

## Automated publish sequence

### Preflight job

1. checks out the tag;
2. installs the SDK using `global.json`;
3. resolves the desktop project `Version` with MSBuild;
4. fails if the tag does not equal `v<Version>`.

### Per-RID publish jobs

Each matrix job:

1. checks out the tag;
2. installs the SDK using `global.json`;
3. restores `ContactCore.slnx`;
4. runs solution tests in Release;
5. publishes the desktop project with Release configuration, target RID, self-contained runtime, and single-file publishing enabled;
6. packages Windows output as `.zip` or Linux/macOS output as `.tar.gz`;
7. uploads the packaged archive as an Actions artifact.

Unix output is tarred **before** `actions/upload-artifact`, preserving executable metadata inside the tar archive instead of relying on Actions artifact file-mode preservation.

### Final GitHub Release job

The final job:

1. waits for all four target jobs;
2. downloads and merges the packaged Actions artifacts into one release-assets directory;
3. generates SHA-256 checksums with `sha256sum`;
4. attaches all archives plus `SHA256SUMS.txt` to the GitHub Release;
5. asks `softprops/action-gh-release@v2` to generate release notes.

## Workflow permissions

The workflow defaults to:

```text
contents: read
```

Only the final GitHub Release job receives:

```text
contents: write
```

This reduces write permission exposure during checkout/test/publish jobs.

## SDK consistency

Development, CI, CodeQL build preparation, and the 2.0.12 release workflow should resolve .NET using `global.json`. The release workflow now uses `actions/setup-dotnet` with `global-json-file: global.json` rather than a separate hard-coded `10.0.x` policy.

This removes one source of release-versus-CI SDK drift.

## Artifact verification

Checksums provide transport/integrity verification, not authenticity comparable to signed/notarized binaries.

After download, a user/maintainer can compute SHA-256 and compare it with `SHA256SUMS.txt`. The exact command varies by OS/tooling.

Maintainers should additionally inspect each archive for:

- expected executable/bundle files;
- no unexpected debug/private/generated files;
- startup on its target architecture;
- logo/branding/resources;
- ability to create a disposable local database;
- full rich contact create/edit/save behavior;
- search/favorite/archive/A–Z navigation;
- duplicate review and both survivor choices;
- Settings theme behavior;
- fictional CSV/vCard import/export;
- verified backup creation;
- restore to a disposable profile;
- unsaved-draft discard;
- permanent-delete confirmation.

Record what was actually smoke-tested.

## Signing and notarization

The current workflow does **not** implement or claim:

- Authenticode signing for Windows;
- Apple Developer ID signing;
- macOS notarization/stapling;
- Linux package signing;
- signed installers.

SHA-256 checksums detect changed bytes relative to the published checksum file but do not replace trusted code signing/notarization.

If signing is added later:

- keep private keys/certificates in an appropriate secret system, never Git;
- use least-privilege workflow permissions;
- prevent untrusted PR code from accessing signing secrets;
- document certificate identity and verification instructions;
- add an ADR or release-security design note for the signing pipeline.

## Version and schema compatibility

Application version 2.0.12 and the SQLite schema version are different concepts. A patch application release can still contain migrations if project policy permits, but migrations must remain forward-compatible from supported previous database versions.

ContactCore rejects a database whose schema version is newer than the running build. Users who open data with a newer app should therefore keep a verified pre-upgrade backup before attempting to use an older build.

Restore can migrate an older supported backup in staging before making it active.

## Release notes for 2.0.12

At minimum mention:

- full repeated-field contact editor and identity-preservation behavior;
- explicit unsaved-draft safety;
- interactive duplicate review, survivor choice, and atomic merge/delete;
- CSV/vCard parser hardening and interchange limitations;
- verified backup/restore hardening;
- first-run runtime database-key fix and fail-closed encryption-provider boundary;
- literal search wildcard behavior;
- cross-platform test/CodeQL status of the exact release commit;
- packaged RIDs and `SHA256SUMS.txt`;
- unsigned/unnotarized status;
- lack of global taxonomy UI, field reordering, general undo, full vCard fidelity, default encryption-at-rest, and high-scale duplicate optimization;
- manual accessibility/platform validation status.

Never include real user data in examples/screenshots.

## Screenshots

Only publish screenshots made from a disposable profile with clearly fictional contacts. Review the entire image for OS notifications, usernames, paths, email addresses, or other accidental personal information.

## Failed or partial release

If preflight fails because the tag does not match the project version, correct the version/tag plan rather than bypassing the check.

If one matrix leg fails, do not present 2.0.12 as fully cross-platform. Fix the cause and use a clean follow-up release strategy rather than silently omitting the failed platform.

If a GitHub Release already contains partial assets, inspect/delete/replace them according to project policy and preserve a clear audit trail.

Do not move an existing public semantic version tag to unrelated code after users may have fetched it. Prefer a new corrected patch version.

## Rollback guidance

Application rollback and **data rollback** are separate.

Because newer releases can migrate the SQLite schema, installing an older binary may be rejected by the future-schema check. The safe data rollback path is usually to restore a verified backup created before an incompatible schema upgrade using a build that supports that backup.

## Post-release checks

After publishing 2.0.12:

- confirm all four intended archives plus `SHA256SUMS.txt` are attached;
- verify checksum file entries match the released archives;
- verify generated release notes/changelog accuracy;
- smoke-test downloads/startup on representative targets;
- record any platform-specific limitations;
- monitor repository issues for reproducible regressions;
- move roadmap/changelog/`what_changed.md` to the next milestone;
- never request users upload a real contact database publicly when reporting defects.

## Release automation details

See `ci-cd.md` for workflow permissions, concurrency, quality matrices, artifacts, and troubleshooting.
