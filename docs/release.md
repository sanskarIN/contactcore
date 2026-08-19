# Release

ContactCore uses semantic-looking Git tags matching `v*.*.*` to trigger GitHub Actions publishing. A release should represent a verified repository state, not merely a tag that causes artifacts to be produced.

## Current automated targets

The release workflow publishes `ContactCore.Desktop` as a self-contained, single-file application for:

| Platform | RID | Runner |
|---|---|---|
| Windows x64 | `win-x64` | `windows-latest` |
| Linux x64 | `linux-x64` | `ubuntu-latest` |
| macOS Intel | `osx-x64` | `macos-latest` |
| macOS Apple Silicon | `osx-arm64` | `macos-latest` |

The workflow does not currently publish Windows ARM64, Linux ARM64, installers, app-store packages, or platform-specific package-manager formats.

## Trigger

A push of a tag matching:

```text
v*.*.*
```

starts `.github/workflows/release.yml`.

Use normal semantic-version intent such as `v2.0.0`, `v2.0.1`, or `v2.1.0`. Avoid creating a tag until the changelog and release scope are agreed.

## Pre-release checklist

Before tagging:

1. `main` contains the intended release changes.
2. The final `main` commit has successful CI on Windows, Ubuntu, and macOS.
3. CodeQL has no unresolved newly introduced actionable issue.
4. `CHANGELOG.md` is updated.
5. `README.md`, `docs/README.md`, user guide, architecture, data/storage/security/testing docs match actual code.
6. `what_changed.md` does not falsely list already completed work as pending.
7. No real contact data, database, backup, export, `.env`, key, certificate/private signing material, or private endpoint is tracked.
8. Any schema migration has upgrade tests and restore compatibility review.
9. Import/export format changes have round-trip/malformed-input tests.
10. Destructive/restore UI behavior has been manually smoke-tested with fictional data.
11. Known limitations are written into release notes.

## Local quality pass

From a clean checkout when possible:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build
```

Local success is valuable but does not replace the GitHub cross-platform matrix.

## Tagging

Example:

```bash
git checkout main
git pull --ff-only
git tag -a v2.0.0 -m "ContactCore v2.0.0"
git push origin v2.0.0
```

Use the actual intended version, not the example blindly.

## Automated publish sequence

Each RID job:

1. checks out the tag;
2. installs .NET 10.x;
3. restores `ContactCore.slnx`;
4. runs solution tests in Release;
5. publishes the desktop project with:
   - Release configuration;
   - target RID;
   - self-contained runtime;
   - single-file publish;
6. uploads the target directory as an Actions artifact.

The release job waits for all publish jobs, downloads their artifacts, prints the discovered files, and invokes `softprops/action-gh-release@v2` with generated release notes and all downloaded files.

## SDK consistency note

Normal development/CI uses `global.json` (`10.0.100`, `latestFeature` roll-forward). The release workflow currently requests `10.0.x` directly. These are intended to resolve to compatible .NET 10 SDKs, but maintainers should consider using `global-json-file` in release automation too so all pipelines share one explicit SDK policy.

## Artifact inspection

Do not assume a successful `dotnet publish` means a usable end-user release. Inspect each artifact for:

- expected executable/bundle files;
- unexpected debug/sensitive/generated files;
- startup on its target architecture;
- logo/branding/resources;
- ability to create a disposable local database;
- basic create/edit/search/filter actions;
- Settings theme change;
- import/export with fictional CSV/vCard;
- verified backup creation;
- restore to a disposable profile;
- permanent-delete confirmation.

Record what was actually smoke-tested.

## Signing and notarization

The current workflow does **not** document:

- Authenticode signing for Windows;
- Apple Developer ID signing;
- macOS notarization/stapling;
- Linux package signing;
- signed installers.

Therefore release notes and README must not claim that artifacts are signed/notarized. Users may encounter operating-system reputation/security prompts.

If signing is added later:

- keep private keys/certificates in an appropriate secret system, never Git;
- use least-privilege workflow permissions;
- prevent untrusted PR code from accessing signing secrets;
- document certificate identity and verification instructions;
- add an ADR or release-security design note for the signing pipeline.

## Version and schema compatibility

Application version and SQLite schema version are different concepts. A patch/minor application release can still introduce a migration if policy permits, but the migration must be forward-compatible from all supported previous database versions.

ContactCore rejects a database whose schema version is newer than the running build. Users who open data with a newer app should therefore be warned before attempting to run an older build against the upgraded database.

Backup restore can migrate an older supported backup in staging before making it active.

## Release notes

Include:

- user-visible features/fixes;
- data/schema changes;
- import/export changes;
- privacy/security changes;
- accessibility improvements/known gaps;
- supported artifact RIDs;
- explicit signing/notarization status;
- known editor limitation for rich repeated contact fields if still present;
- known duplicate UI limitation if still present;
- backup/restore compatibility notes;
- upgrade instructions when necessary.

Never include real user data in screenshots/examples.

## Screenshots

Only publish screenshots made from a disposable profile with clearly fictional contacts. Review the entire image for OS notifications, usernames, paths, email addresses, or other accidental personal information.

## Failed/partial release

If one matrix leg fails, do not present the release as fully cross-platform. Fix the cause and choose a clean release/tag strategy rather than silently omitting the failed platform without documenting it.

If a GitHub Release already contains partial assets, inspect/delete/replace them according to the release plan and maintain a clear audit trail in notes/changelog.

Do not move an existing public semantic version tag to unrelated code after users may have fetched it. Prefer a new corrected version.

## Rollback guidance

Application rollback and **data rollback** are separate.

Because newer releases can migrate the SQLite schema, installing an older binary may be rejected by the future-schema check. The safe data rollback path is usually to restore a verified backup created before the incompatible schema upgrade using a build that supports that backup.

Release notes for migration-heavy versions should tell users to create/retain a verified backup before upgrading.

## Post-release checks

After publishing:

- confirm all intended assets are attached;
- verify generated release notes/changelog accuracy;
- check artifact download/startup on representative targets;
- monitor repository issues for reproducible regressions;
- update `what_changed.md`/roadmap for the next milestone;
- never request users upload a real contact database publicly when reporting defects.

## Release automation details

See `ci-cd.md` for workflow permissions, concurrency, test artifacts, and troubleshooting.
