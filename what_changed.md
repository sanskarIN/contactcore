# ContactCore — Work Handoff

## Current milestone

**Phase 4 verification and hardening, with Phase 2/3 UX completion in progress.**

The repository now has a substantial end-to-end ContactCore implementation and an active verification pull request. The immediate priority is to let the real GitHub Actions .NET toolchain compile, format-check, test, and analyze the current branch, then fix every actionable failure before merging.

## Repository identity

- Repository: `https://github.com/sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Product: **ContactCore — Basic Contact Book**
- Primary stack: C# / .NET 10 / Avalonia / SQLite
- Target platforms: Windows, macOS, Linux
- License: MIT
- Git commit email confirmed on repository commits: `sanskarin@outlook.in`
- Required visible credit: **Made by the Sanskar**
- Business: `sanskarin@outlook.in`, `sanskarin.business@gmail.com`
- Support: `supportramsandesh@gmail.com`
- GitHub: `https://github.com/sanskarIN`
- Funding: `https://buymeacoffee.com/sanskarIN`

## Source prompt reconciliation

The uploaded master prompt for this work is the **ContactCore** master development prompt. It specifies a production-quality offline-first contact book using C#/.NET, Avalonia, and SQLite. The repository implementation is aligned with that product identity and architecture. Earlier handoff text that referred to an unrelated “LibraCore” prompt was inaccurate and has been replaced here.

## Active development branch and pull request

- Branch: `feature/phase-2-ux`
- Pull request: **#3 — `feat: complete ContactCore phase 2 desktop workflows`**
- PR base: `main`
- The branch has been synchronized with the latest `main` audit handoff before further verification work.

## Completed implementation

### Repository and engineering baseline

- `.editorconfig`, `.gitattributes`, `.gitignore`, `.env.example`, `global.json`.
- Central package management and common build settings.
- Strict nullable/warning configuration and deterministic builds.
- Solution split into Domain, Application, Infrastructure, Desktop, and test projects.
- MIT license, governance, contribution, security, privacy, support, changelog, roadmap, ADRs, and full documentation baseline.
- GitHub issue/PR templates, Dependabot, funding metadata, CI, CodeQL, and release workflows.

### Domain layer

- Contact aggregate with:
  - given/family/nickname fields
  - birthday and notes
  - favorite/archive state
  - multiple phone numbers
  - multiple email addresses
  - multiple postal addresses
  - multiple organizations
  - groups and tags
  - timestamps
- Contact validation for lengths, email syntax, and phone syntax.
- Unicode-aware search normalization.
- Phone-number normalization.
- Deep-copy support for safe edit/merge workflows.

### Application layer

- Repository, backup, and preferences abstractions.
- Contact create/update/delete/favorite/archive workflows.
- Search/filter query model.
- Bulk import service entry point.
- Duplicate scoring with deterministic reasons.
- Duplicate merge engine preserving the selected primary contact.
- Duplicate scanning optimized with normalized blocking indexes instead of comparing every unrelated contact pair.
- CSV import/export codec.
- vCard import/export codec.

### Infrastructure layer

- Cross-platform application-data paths.
- SQLite connection factory.
- Optional database-encryption hook that **fails closed** when a key is configured without a SQLCipher-compatible provider.
- Explicit versioned SQLite migrations.
- Pre/post migration integrity checks.
- Refusal to open a database created by a newer unsupported schema version.
- Complete transactional persistence for the contact aggregate and child records.
- Search filters for text, favorites, archived state, tags, groups, and alphabet navigation.
- Indexed contact-name, favorite/archive, phone, and email fields.
- Backup creation using SQLite's backup API.
- Backup integrity verification.
- Restore validation that rejects unrelated SQLite databases.
- Automatic pre-restore safety backup of the current database.
- Backup/restore encryption behavior preserved through the configured connection factory.
- Local JSON preferences with atomic replacement.
- Database keys excluded from persisted preferences.
- PII-redacted user-safe diagnostic text.

### Desktop UI

- Avalonia composition root and application bootstrap.
- Modern three-pane contact layout.
- Search box and keyboard shortcuts (`Ctrl+N`, `Ctrl+S`, `Ctrl+F`, `Esc`).
- All/Favorites/Archived navigation.
- Alphabet navigation.
- Empty, busy, status, and error-oriented UI states.
- First-run onboarding overlay.
- Full multi-value contact editor with tabs for:
  - general identity/birthday/notes
  - groups and tags
  - repeated phone numbers
  - repeated email addresses
  - repeated addresses
  - repeated organizations
- Favorite/archive controls.
- Permanent-delete confirmation flow.
- CSV/vCard import and export file pickers.
- Backup/restore file and folder pickers.
- Duplicate discovery and merge-preview overlay.
- Settings/About overlay with:
  - system/light/dark theme selection
  - reduced-motion preference
  - delete-confirmation preference
  - local data directory display
  - privacy explanation
  - project/license/contact details
  - GitHub and Buy Me a Coffee links
  - **Made by the Sanskar** credit
- Editable SVG application logo asset.
- Focus styling and accessibility-oriented labels/automation names.

## Automated tests added

### Domain tests

- Valid/invalid contact validation.
- Unicode normalization.

### Application tests

- Duplicate scoring.
- Duplicate blocking on large sets of unrelated contacts.
- Birthday-only low-threshold candidate behavior.
- Invalid duplicate thresholds.
- Merge de-duplication.
- ContactService merge behavior and secondary deletion.
- Import validation boundary.
- CSV round-trip with quotes, commas, and newlines.
- vCard round-trip.
- Deterministic randomized Unicode parser smoke coverage.

### Infrastructure tests

- SQLite aggregate persistence round-trip.
- Cascading delete.
- Backup/restore round-trip.
- Encryption fail-closed behavior with ordinary SQLite.
- Refusal of future schema versions.
- Rejection of unrelated SQLite restore files.
- Preferences persistence without database-key persistence.
- Corrupted preference fallback.
- PII diagnostic redaction and output bounding.

## Meaningful commits from the current continuation

The current continuation deliberately uses small, reviewable Conventional-Commit-style changes. Recent branch commits include:

- `3e06e2a` — `feat(application): add bulk import merge and onboarding state`
- `df09c30` — `feat(ui): add multi-value contact editor models`
- `6e50513` — `feat(ui): add onboarding data tools duplicate merge and settings state`
- `5dd759f` — `feat(ui): add file pickers theme switching and design surfaces`
- `a4475fe` — `feat(ui): complete multi-field editor onboarding settings and merge preview`
- `7533565` — `fix(ui): use infrastructure namespace in desktop file handlers`
- `935bda8` — `fix(storage): preserve encryption and integrity across backups`
- `bfbce98` — `fix(storage): fail safely on corrupt or newer schemas`
- `8cd5881` — `perf: block duplicate scans by normalized identity keys`
- `e4e47f6` — `test(application): cover scalable duplicate scans and merge service`
- `ba3cece` — `test(storage): cover backup encryption migrations preferences and redaction`
- `fbfbff1` — `fix(test): make duplicate pair assertion order-independent`

## Verification status

### Local toolchain limitation

The current coding environment does not have the `dotnet` SDK installed. Therefore these commands cannot be truthfully claimed as executed locally in this session:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

### GitHub Actions verification

PR #3 exists specifically to provide the authoritative .NET verification environment.

Observed workflow runs for the first PR head included:

- CI run `32240433384`
- CodeQL run `32240433292`

They were queued when last checked. A later test-fix commit advanced the PR head, so verification must be read from the newest PR-head workflow runs before the milestone is declared passing.

**Do not claim the project is build-clean or test-clean until the newest CI/CodeQL jobs have completed successfully.**

## Known limitations / remaining work

1. GitHub Actions still needs to compile the newest branch head and surface any C#/Avalonia/XAML/API mistakes.
2. UI accessibility needs manual platform checks with keyboard-only operation and real screen readers before any claim of full conformance.
3. Real screenshots must be captured only after a verified desktop build and must contain fictional contacts only.
4. Release signing/notarization is not configured and must never be claimed until protected signing credentials and platform processes exist.
5. Large SQLite result materialization should be benchmarked with representative datasets before publishing a hard performance claim.
6. CSV/vCard interoperability can still be expanded to preserve every extended ContactCore field in every external format; vCard currently has better natural support for repeated values.
7. Dedicated group/tag management screens can improve usability beyond the current comma-separated editor.
8. A maintained SQLCipher-compatible native provider still needs platform packaging/integration validation before encrypted builds can be distributed as a supported release option.
9. Strings are not yet fully externalized for shipping additional locales; the architecture/documentation should continue toward internationalization readiness.

## Next exact tasks

1. Read the newest PR #3 CI and CodeQL job results.
2. Fix every compiler, XAML, formatting, test, and static-analysis failure in small commits.
3. Re-run the quality gates until green on the newest head.
4. Re-check the repository for TODO/FIXME placeholders, tracked secrets, accidental databases, generated artifacts, and documentation drift.
5. Improve remaining import/export fidelity and large-list persistence/query performance where verification shows value.
6. Update this file with exact successful/failed commands represented by CI, job IDs, fixes, and commit hashes.
7. Merge PR #3 only after it is up to date with `main` and its required quality gates are satisfactory.
8. Continue remaining roadmap work from the merged state rather than rewriting completed modules.

## Release-note draft

ContactCore now provides a substantial private, offline-first desktop contact-management experience with layered C#/.NET architecture, transactional SQLite persistence, multi-value contact editing, search/filtering, groups/tags, CSV/vCard interchange, duplicate detection/merge preview, integrity-checked backup/restore, theme/settings/onboarding UX, privacy/security hardening, automated tests, and GitHub CI/release engineering. The current milestone is verification and defect removal before a release candidate is declared ready.
