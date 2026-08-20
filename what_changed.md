# ContactCore — Work Handoff

## Current milestone

**Phase 5 / secure release hardening** — ContactCore has a working local-first .NET/Avalonia implementation baseline, and the current continuation is repairing the previously failing verification pipeline, hardening data safety, and making release output reproducible before any further feature expansion.

## Repository identity

- Repository: `https://github.com/sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Primary stack: C# / .NET 10 / Avalonia / SQLite
- Product: private, offline-first desktop contact manager
- License: MIT
- Required visible credit: **Made by the Sanskar**
- Preferred commit email: `sanskarin@outlook.in`

## Authoritative continuation branch

Current branch:

- `hardening/ci-security-20260820`

Current pull request:

- **PR #13 — `fix: restore secure CI and modernize quality gates`**
- Base: `main`
- PR URL: `https://github.com/sanskarIN/contactcore/pull/13`

Do not add unrelated feature expansion to this branch until CI and CodeQL are green. Incremental hardening fixes and regression tests belong here.

## Why this continuation exists

The previous audit PR was merged even though CI and CodeQL were failing during `dotnet restore`. The concrete blocker was:

- `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 flagged by NuGet as a high-severity vulnerable dependency.

The earlier pipeline also stopped matrix execution after the first platform failure, used older GitHub Action generations, and did not preserve per-platform test evidence.

## Completed work on the current hardening branch

### Dependency and restore security

- Updated `Microsoft.Data.Sqlite` from 10.0.10 to 10.0.11.
- This raises the SQLitePCLRaw dependency floor past the vulnerable 2.1.11 native package used by the failing restore.
- Kept warnings-as-errors enabled; the vulnerability warning is fixed rather than suppressed.

Commit:

- `3010a54` — `fix(deps): update SQLite package chain past vulnerable native library`

### Cross-platform CI hardening

- Updated checkout to `actions/checkout@v5`.
- Updated .NET setup to `actions/setup-dotnet@v6`.
- Disabled matrix fail-fast so Windows, Linux, and macOS all report diagnostics.
- CI now uses `global.json` instead of independently selecting a floating SDK configuration.
- Added NuGet cache configuration based on `Directory.Packages.props`.
- Added workflow concurrency cancellation for superseded runs.
- Added job timeouts.
- Tests now emit TRX plus XPlat coverage into `TestResults`.
- Test results are uploaded per operating system even when a later step fails.

Commits:

- `5d05e86` — `ci: modernize build actions and keep full platform diagnostics`
- `450d248` — `ci: make cross-platform verification reproducible and preserve test evidence`

### CodeQL hardening

- Updated CodeQL actions from v3 to v4.
- Updated checkout/setup-dotnet generations.
- Added `global.json`-based SDK selection.
- Added NuGet caching, concurrency cancellation, and a bounded timeout.
- Retained explicit restore/build before CodeQL analysis.

Commits:

- `056a661` — `ci(security): move CodeQL workflow to current action generations`
- `b066cea` — `ci(security): make CodeQL reproducible and bounded`

### Release engineering

- Updated release checkout/setup-dotnet actions.
- Added matrix fail-fast protection and timeouts.
- Added reproducible SDK selection and NuGet caching.
- Release publishing now fails if expected artifacts are missing.
- Windows output is packaged as a `.zip` archive.
- Linux/macOS outputs are packaged as `.tar.gz` archives.
- Release creation downloads only the packaged assets and fails when no files match.
- Debug symbols are disabled for release packages.

Commits:

- `ad08c9c` — `ci(release): harden cross-platform publishing diagnostics`
- `42751da` — `ci(release): package clean per-platform archives for GitHub releases`

### Import/export correctness

- Replaced unsupported/ambiguous `DateOnly.TryParseExact` usage with the explicit invariant-culture overload.
- Made CSV birthday formatting invariant.
- Made vCard birthday formatting/parsing invariant.
- Made string replacement intent explicit with `StringComparison` where appropriate.

Commit:

- `6da05ea` — `fix(import): use invariant DateOnly parsing and formatting APIs`

### Desktop search correctness

- Fixed the same `DateOnly.TryParseExact` issue in the editor draft model.
- Made birthday display formatting invariant.
- Added refresh versioning so an older asynchronous search result cannot overwrite a newer query result.

Commit:

- `f394fa4` — `fix(ui): make birthday parsing deterministic and prevent stale search refreshes`

### Backup/restore safety

Restore behavior now protects the last known-good database:

1. Integrity-check the requested backup before touching the live database.
2. Copy the candidate into a staging file.
3. Clear pooled SQLite connections and remove stale WAL/SHM sidecars.
4. Preserve the existing database as a rollback copy.
5. Replace the database with the staged candidate.
6. Run schema migrations on the restored database.
7. Run a second SQLite integrity check after migration.
8. Restore the previous database automatically if migration or verification fails.
9. Clean staging/rollback temporary files in all paths.

Backup filenames now include milliseconds to reduce collision risk.

Commit:

- `20a360d` — `fix(backup): make restore transactional with rollback verification`

### Backup regression coverage

Added infrastructure tests for:

- successful backup/restore round-trip;
- invalid non-SQLite backup rejection without replacing live data;
- automatic rollback when a structurally valid backup causes post-restore migration failure;
- cleanup of `.restore` and `.pre-restore` files after failure.

Commit:

- `4a4dbcc` — `test(backup): cover restore integrity and rollback guarantees`

## Previous stale branch status

The old overlapping PR #1 has already been closed without merge as superseded. Do not reopen or merge it. Useful release-workflow ideas from that branch have been selectively reapplied to the current hardening branch rather than merging conflicting history.

## Verification status

### Confirmed previous failure

The earlier CI/CodeQL failure was real and occurred during restore because NuGet treated the vulnerable SQLitePCLRaw 2.1.11 advisory as an error.

### Current status

GitHub Actions verification for PR #13 has been triggered repeatedly as the hardening commits were added. At this handoff, the final current head still requires a complete fresh CI + CodeQL result before merge.

Do **not** claim that the project is build-clean, test-clean, format-clean, or CodeQL-clean until the workflows on the final PR head finish successfully.

### Required quality gates

The authoritative gates are:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

plus CodeQL analysis on GitHub Actions.

## Next exact tasks

1. Wait only on the GitHub runner result in the sense of checking the already-triggered workflow; do not create a release or merge early.
2. Read every failed CI/CodeQL job log on the newest PR #13 head.
3. Fix compiler, formatter, analyzer, test, XAML, SQLite, or workflow errors with small focused commits.
4. Re-run or allow the PR workflow to rerun after each fix until Windows, Linux, macOS, and CodeQL all pass.
5. Verify the new backup tests pass on all supported CI operating systems.
6. Check the release workflow syntax and package commands through GitHub Actions before describing release artifacts as verified.
7. Audit CSV spreadsheet-formula behavior and add a safe spreadsheet-export policy/test without silently corrupting normal round-trip data.
8. Add parser edge/fuzz-style tests for malformed CSV and vCard input.
9. Audit large-result search materialization/UI virtualization before making high-scale performance claims.
10. Reconcile README/release/testing documentation with the exact final CI result.
11. Update this file again with final workflow run IDs, conclusions, any additional fix commits, and merge commit.
12. Merge PR #13 only after the final head is satisfactory.

## Known limitations that must remain accurately documented

- Release artifacts are not signed or notarized unless signing/notarization is actually configured.
- Desktop accessibility still requires manual platform verification in addition to code review.
- Performance at very large contact counts is not yet benchmark-verified.
- Screenshots, tests, sample exports, and bug reports must use fictional contact data only.
- Optional database encryption must continue to fail closed when SQLCipher is unavailable; never imply standard SQLite is encrypted merely because a key was supplied.

## Product baseline already present

ContactCore currently includes:

- contact aggregate models and validation;
- Unicode-aware normalization;
- duplicate scoring and merge behavior;
- CSV and vCard interchange;
- SQLite migrations and complete aggregate persistence;
- indexed local search;
- local preferences;
- PII-redacted diagnostics;
- backup/restore;
- Avalonia desktop UI;
- domain/application/infrastructure tests;
- CI, CodeQL, Dependabot, release automation, issue templates, PR template, and funding metadata;
- architecture, privacy, security, support, contribution, testing, release, troubleshooting, accessibility, performance, changelog, and roadmap documentation.

The current priority is **verification and hardening**, not adding unverified feature count.
