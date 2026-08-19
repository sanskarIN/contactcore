# ContactCore — Work Handoff

## Current milestone

**Phase 4 / release-candidate audit** — the repository now contains a complete local-first ContactCore implementation baseline. The current task is to compile/test it through GitHub Actions, fix every discovered build/test/static-analysis defect, reconcile documentation with the real code, and then continue the remaining roadmap items in small, reviewable commits.

## Repository identity

- Repository: `https://github.com/sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Audited base commit: `49786b9d4491ce96675e0a91d74dae0bf8602916`
- Base commit message: `docs: add changelog and delivery roadmap`
- Confirmed Git author/committer email on repository commits: `sanskarin@outlook.in`
- Primary stack: C# / .NET 10 / Avalonia / SQLite
- Product: private, offline-first desktop contact manager
- License: MIT
- Required visible credit: **Made by the Sanskar**

## Uploaded-prompt reconciliation

The uploaded master prompt supplied to this session is titled **LibraCore** and describes a Java/Spring/React library-management product, while the explicitly requested destination repository is **ContactCore** and already contains an established .NET/Avalonia contact-management architecture. The master prompt also instructs the coding agent to inspect existing repositories and preserve useful working history rather than replacing working code.

For this repository, the safe interpretation is therefore:

1. Preserve ContactCore's existing product identity and .NET/Avalonia architecture.
2. Apply the prompt's transferable quality requirements: complete implementation, layered architecture, security/privacy, tests, accessibility, CI, documentation, release engineering, and granular meaningful commits.
3. Do **not** replace ContactCore with an unrelated LibraCore library-management application.

## Important concurrency note

During this session another repository-writing continuation advanced `main` substantially while a separate phase branch was being prepared. That concurrent `main` work is now the authoritative base because it contains a broader, already-integrated implementation.

A parallel pull request, **PR #1 (`phase1/contactcore-core-20260819`)**, contains overlapping implementation and must not be merged blindly. Its unique ideas should only be reapplied selectively after comparing them with current `main`; otherwise it should be closed as superseded to avoid duplicate/conflicting code.

A fresh audit branch was created directly from the current main base:

- `audit/contactcore-20260819`

All further fixes should be made on that branch (or a successor based on the latest `main`) rather than on the stale overlapping PR branch.

## Completed implementation on current main

### Domain

- Contact aggregate and repeating contact field models.
- Validation for core contact fields.
- Unicode-aware text normalization.
- Phone normalization.

### Application

- Repository/preferences/backup abstractions.
- Contact workflows and validation boundary.
- Duplicate scoring and deterministic merge logic.
- CSV import/export codec.
- vCard import/export codec.

### Infrastructure

- Cross-platform application data paths.
- SQLite connection factory.
- Versioned SQLite migrations.
- Complete contact aggregate persistence.
- Indexed local search/filtering.
- Transactional writes and foreign-key relationships.
- Integrity-checked backup/restore.
- Local JSON preferences.
- PII-redacted diagnostic logging support.

### Desktop application

- Avalonia application bootstrap/composition root.
- Main desktop window and styling.
- Contact list/search workspace.
- Contact editing workflows.
- Favorites/archive actions.
- Import/export and backup-oriented actions.
- Theme/accessibility-oriented UI structure.
- Editable repository branding assets.

### Tests

- Domain validation/normalization coverage.
- Application duplicate/import-export coverage.
- SQLite aggregate integration coverage.

### GitHub/release engineering

- CI workflow.
- CodeQL/security workflow.
- Cross-platform release publishing workflow.
- Dependabot configuration.
- Issue templates.
- Pull-request template.
- Funding metadata.

### Documentation/governance

The repository now contains the required documentation baseline, including README, contribution/governance/security/privacy/support documents, threat/security guidance, architecture and ADRs, setup/development/testing/release/troubleshooting/accessibility/performance guides, changelog, and roadmap.

## Most recent meaningful main commits at audit start

- `49786b9` — `docs: add changelog and delivery roadmap`
- `e1595f5` — `docs: add release accessibility performance and recovery guides`
- `3ed6c3c` — `docs: add setup development and testing guides`
- `8e87c43` — `docs: document architecture storage and encryption decisions`
- `c542317` — `docs: add governance security privacy and support policies`
- `af171ae` — `ci: add cross-platform release publishing`
- `5483ed6` — `ci: add cross-platform quality and security checks`
- `5f23040` — `chore(github): add contribution automation and funding`
- `87114b4` — `test(storage): add SQLite aggregate integration coverage`
- `51f7ab0` — `test(application): cover duplicate and interchange workflows`
- `6068fa8` — `test(domain): cover validation and Unicode normalization`
- `3972256` — `feat(ui): wire contact workflows search and desktop actions`
- `6c8304f` — `feat(ui): add accessible three-pane contact experience`
- `ca4d48d` — `feat(ui): bootstrap Avalonia desktop application`
- `fda6f47` — `feat: add local preferences and PII-redacted diagnostics`
- `696efc0` — `feat: add integrity-checked backup and restore`
- `1bf185d` — `feat(storage): persist complete contact aggregates in SQLite`
- `9c4b6f3` — `feat(storage): add SQLite initialization and migrations`
- `de2b7e2` — `feat: add CSV and vCard import export codecs`
- `f2f1230` — `feat: add duplicate detection and merge engine`

## Verification status

### Local execution limitation

The coding environment available in this chat does not provide the .NET SDK/compiler, so the following commands cannot be truthfully reported as locally executed:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
dotnet build ContactCore.slnx -c Release
dotnet test ContactCore.slnx -c Release
```

This is an environment limitation, not evidence that the project passes or fails.

### Verification strategy

A pull request from `audit/contactcore-20260819` must be used to run the real GitHub Actions quality gates against the latest integrated implementation. Compiler, test, format, CodeQL, and workflow failures must be fixed before calling the milestone verified.

## Audit findings to verify/fix

These are audit targets, not yet claims of confirmed defects:

1. Validate CSV/vCard parser edge cases and all `DateOnly.TryParseExact` usages against the actual .NET 10 compiler.
2. Verify Avalonia XAML resource names/bindings and generated MVVM commands compile against the pinned Avalonia/CommunityToolkit versions.
3. Verify SQLite migration/transaction APIs compile cleanly with Microsoft.Data.Sqlite 10.0.10.
4. Confirm backup restore behavior cannot overwrite the only good copy after a failed post-restore migration.
5. Confirm the optional encryption configuration fails closed rather than silently accepting a key with plaintext SQLite.
6. Confirm search/filter refresh cannot lose a user query while another async UI operation is busy.
7. Check CSV spreadsheet-formula behavior and document/implement a safe export mode if spreadsheet-oriented export is exposed.
8. Verify release workflow packaging commands on Windows, Linux, macOS Intel, and macOS Apple Silicon runners.
9. Check all README/documentation claims against current code and actual CI results.
10. Confirm no real secrets, databases, exported personal data, signing material, or private endpoints are tracked.

## Known limitations / remaining roadmap

- Build/test status is not yet verified in this chat environment; GitHub Actions is required.
- Desktop UI still needs deeper manual accessibility/platform verification before claiming full conformance.
- Large-result SQLite materialization and UI virtualization should be benchmarked before claiming high-scale performance.
- Parser fuzz/property tests remain desirable for CSV/vCard inputs.
- Release artifacts are not to be described as signed/notarized unless signing is actually configured.
- Real screenshots must use fictional sample contacts only.

## Next exact tasks

1. Inspect current `main` source/tests/workflows file-by-file for likely compile/runtime defects.
2. Commit only incremental audit fixes on `audit/contactcore-20260819`.
3. Open a fresh audit PR into `main` to trigger CI/CodeQL.
4. Read failed job steps/logs and fix every actionable failure with small commits.
5. Re-run failed jobs until quality gates pass.
6. Close stale overlapping PR #1 as superseded once unique useful changes have been compared/reapplied.
7. Update this file with exact CI results, fixes, commit hashes, and the next unfinished roadmap tasks.
8. Merge the audit PR only when repository checks are satisfactory and the branch is up to date with `main`.

## Release-note draft

ContactCore has progressed from repository bootstrap to a complete local-first desktop contact-management baseline with layered architecture, transactional SQLite persistence, import/export, duplicate handling, backup/restore, Avalonia UI, automated tests, security/privacy documentation, and GitHub CI/release automation. The current milestone is verification and hardening rather than feature-count expansion.
