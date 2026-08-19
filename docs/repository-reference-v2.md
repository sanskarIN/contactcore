# Repository Reference v2

This file supersedes `repository-reference.md` for the final 2026-08-19 documentation checkpoint. It exists because the repository gained additional test and documentation files after the first exhaustive reference was written.

## Files added after the original reference checkpoint

- `tests/ContactCore.Application.Tests/ContactServiceTests.cs` — focused fake-repository tests for ContactService normalization, whole-batch validation-before-write, deep-copy import behavior, shared import timestamps, and trimmed query forwarding.
- `docs/test-coverage-addendum.md` — checkpoint note that records the same post-reference test addition and exists as part of the granular documentation history.
- `docs/repository-reference-v2.md` — this superseding addendum/reference checkpoint.

## Files whose responsibilities changed after the original reference checkpoint

- `tests/ContactCore.Domain.Tests/ContactValidationTests.cs` — now also covers exact length boundaries, display-name fallback, deep-copy collection independence, and phone normalization.
- `tests/ContactCore.Infrastructure.Tests/SqliteRepositoryTests.cs` — now also covers rich aggregate round trip/replacement, literal SQL LIKE wildcard characters, tag/group filters, and family-first A–Z filtering.
- `tests/ContactCore.Infrastructure.Tests/BackupServiceTests.cs` — now also covers missing/same-active-path restore guards, valid unrelated SQLite rejection, schema-family tampering, and readable pre-restore snapshot contents.
- `tests/ContactCore.Infrastructure.Tests/JsonAppPreferencesTests.cs` — now also covers all valid themes, reduced-motion/delete-confirmation persistence, backward defaults, and successful temporary-file cleanup.
- `.github/ISSUE_TEMPLATE/bug_report.yml` — now requires privacy-safe public reports and blocks real contact data/secrets.
- `.github/ISSUE_TEMPLATE/feature_request.yml` — now requests privacy/offline/accessibility/data-compatibility analysis.
- `.github/pull_request_template.md` — now carries explicit CI, CodeQL, data-safety, aggregate-preservation, migration/recovery, and documentation gates.
- `.gitignore` — now ignores a wider set of local database, sidecar, backup/export/temp, and secret/signing-key artifacts.

For every other tracked file, `docs/repository-reference.md` remains the detailed file-by-file description. This v2 checkpoint plus that reference together cover the complete tracked repository state at this stage. `what_changed.md` is the operational handoff and verification record.
