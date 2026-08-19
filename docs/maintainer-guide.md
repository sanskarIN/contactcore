# Maintainer Guide

This guide covers recurring engineering responsibilities for ContactCore maintainers. It complements `CONTRIBUTING.md`; maintainers are additionally responsible for data compatibility, release quality, privacy claims, destructive-operation safety, and repository/documentation hygiene.

## Core invariants

Treat these as product invariants unless an explicit reviewed architecture decision changes them:

1. ContactCore remains useful without a mandatory account, cloud, telemetry, or advertising service.
2. Domain rules do not depend on Avalonia or SQLite.
3. User/data SQL values remain parameterized; literal search wildcards remain escaped.
4. A `Contact` repository write represents the complete desired aggregate.
5. Existing repeated-field identities survive ordinary editor changes unless that row is intentionally removed/recreated.
6. Single-contact aggregate writes, bulk imports, and destructive duplicate merges remain transactional.
7. An unsaved draft is never treated as a persisted record merely because it has a generated GUID.
8. Duplicate detection stays advisory; no score automatically performs a destructive merge.
9. Duplicate merge requires explicit survivor choice and confirmation and must update the survivor/delete the secondary atomically.
10. Database upgrades are versioned/forward-only; unsupported future schemas are rejected.
11. Restore validates before replacement and retains a verified pre-restore recovery path.
12. Requested database encryption fails closed when a compatible provider is unavailable.
13. Runtime database keys are not serialized into ordinary preferences.
14. Destructive desktop actions do not silently bypass required confirmation.
15. Documentation does not overclaim platform, accessibility, encryption, signing, performance, or verification status.

## Branch and review workflow

Base work on the latest intended integration branch. Keep commits conceptually small and use clear prefixes such as `feat:`, `fix:`, `test:`, `docs:`, `ci:`, `refactor:`, or `chore:`.

Before merge:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

Then verify CI and CodeQL on the **final branch head**. A green superseded commit is not verification of a newer head.

## Changing contact fields

When modifying `Contact` or a child record:

1. decide scalar/contact-owned/shared-many-to-many semantics;
2. update validation and normalization where appropriate;
3. append a schema migration if persistence changes;
4. update repository load/write behavior;
5. update `DeepCopy` and merge semantics;
6. update import/export only where deliberately supported;
7. update the full desktop editor or explicitly preserve any newly unsupported field;
8. add regression tests at the lowest useful layers;
9. update `data-model.md`, `desktop-ui.md`, testing docs, changelog, roadmap, and file reference.

## Complete-aggregate editor invariant

The current editor directly represents all repeated collections in the present domain model: phones, emails, addresses, organizations, groups, and tags.

Maintain these properties:

- editing an existing row retains its child ID;
- removing one row removes only that intended row/link;
- blank newly added rows do not create meaningless children;
- legacy representable values such as a label-only address remain preservable;
- group/tag names are independent rows and may contain commas or semicolons exactly;
- duplicate group/tag names in one draft collapse case-insensitively while preserving the first row identity;
- the root contact ID and `CreatedAt` survive editing;
- draft editing does not mutate the source aggregate before save.

Repeated-field reordering is not currently exposed. If reorder support is added, define whether order is persisted before presenting a visual order as durable.

## Unsaved draft semantics

A newly created `Contact` already has a GUID. Never infer persistence from `Id != Guid.Empty`.

`ContactDraftViewModel.IsPersisted` is the current explicit boundary. Unsaved **Delete / discard** closes the draft without a repository delete or permanent-delete confirmation. After successful persistence, the draft is reloaded as persisted.

Any alternate editor/new-contact flow must preserve an equivalent distinction.

## Duplicate handling

Duplicate detection must remain explainable and non-destructive until the user confirms.

Current workflow:

1. load contacts, including archived records;
2. score candidate pairs with `DuplicateDetector`;
3. show score/reasons and side-by-side summaries;
4. let the user choose which record survives;
5. confirm the destructive action;
6. call `ContactService.MergeAsync`;
7. persist survivor update + secondary deletion in one repository transaction.

`SqliteContactRepository.MergeAsync` requires exactly one secondary row to be deleted. If the secondary disappeared, it throws and rolls back the attempted survivor update.

When changing merge logic, test overlapping/distinct phones, emails, addresses, organizations, groups, tags, notes, flags, IDs, self-merge rejection, and missing-secondary rollback. Never convert a heuristic score into automatic deletion.

There is no general-purpose undo stack. Verified backups remain the recovery mechanism for destructive cleanup.

## Database migrations

`DatabaseMigrator` is the schema authority. New migrations use monotonically increasing integer versions and should be deterministic, one-way, transactional where SQLite permits, upgrade-tested, and compatible with previously supported databases.

Do not delete/renumber released migrations. A schema-family identity change requires explicit architecture/security review because backup recognition depends on it.

## Backup and restore

Treat `BackupService` as high-risk code. Preserve this safety ordering:

**verify source → snapshot current → stage → migrate/verify stage → switch → verify active → rollback if necessary**.

Keep coverage for invalid input, unrelated SQLite, identity tampering, future schema, older-schema migration, missing/self restore sources, retained pre-restore snapshot, and unique artifacts. High-value remaining failure injection is forced post-switch verification failure plus cleanup failures at each stage.

Never remove a restore verification stage solely to improve benchmark numbers.

## SQLite encryption boundary

The repository does not bundle a guaranteed SQLCipher provider. `CONTACTCORE_DATABASE_KEY` is meaningful only when a compatible provider is actually active.

Current behavior reads the runtime key even before a first `settings.json` exists, applies it through the connection factory, and fails closed when `PRAGMA cipher_version` cannot verify cipher support.

Any production encryption integration must preserve verification, avoid logging/interpolating the original secret, document provider licensing/native packaging, add platform integration tests, and avoid committing keys/license material/proprietary binaries contrary to policy.

## Preferences

`settings.json` stores non-secret local preferences. Missing new fields need conservative defaults; malformed JSON must continue to degrade safely. Writes use a temporary file + replacement strategy.

`DatabaseKey` remains runtime-only. `ConfirmPermanentDelete = true` is the conservative default.

## Import boundary

Imports are untrusted input. Maintain:

- bounded desktop text input;
- predictable termination/warnings for malformed input;
- Unicode/escaping tests;
- complete-batch normalization/validation before persistence;
- one-transaction batch writes;
- privacy-preserving error/warning text;
- fictional fixtures only.

CSV with no recognized ContactCore header currently imports zero contacts with a warning. Duplicate headers use the first occurrence and warn. Formula-like text is preserved and warned about; do not claim spreadsheet-formula neutralization.

Focused vCard support handles supported escaped delimiters/newlines and common `TYPE` values but is not a full RFC ecosystem implementation. Invalid birthday warnings intentionally do not echo the supplied value.

## Export boundary

CSV and vCard are interchange formats, not complete backups. CSV exports selected scalar fields plus only the first phone/email. Focused vCard omits many ContactCore fields and external vCard properties.

Use verified SQLite backup for full database recovery. Expanding an interchange format requires a field-support matrix and round-trip tests for every newly claimed field.

## Search changes

Preserve parameterized SQL, literal `LIKE` escaping, favorites/archive semantics, deterministic ordering, and debounce cancellation.

Current root + per-contact child loading and leading-wildcard search should be benchmarked before scale claims. FTS5, pagination, or list projections require measured justification; FTS5 additionally deserves an ADR covering migration/tokenization/index synchronization.

## Performance-sensitive duplicate detection

`DuplicateDetector.Find` is pairwise, approximately `n(n-1)/2`. If optimizing candidate generation, preserve candidate-quality tests and explain blocking rules. Do not silently reduce recall for speed without a product decision.

## Accessibility maintenance

For UI changes verify keyboard reachability, visible focus, meaningful labels/names, logical tab order, scaling, non-color-only cues, theme behavior, and reduced-motion handling for custom animation.

The current full editor and duplicate-review pane increase control density; include their add/remove rows, both survivor buttons, confirmation dialog, scrolling, and minimum-window behavior in manual accessibility checks.

Do not claim formal accessibility conformance without the relevant audit.

## Dependency updates

Versions are centralized in `Directory.Packages.props`. Review official release/security notes, framework compatibility, Avalonia XAML/source-generator effects, SQLite native behavior, MSTest/coverage changes, licensing, and the final cross-platform checks.

Dependabot is an input to review, not automatic approval.

## GitHub Actions maintenance

Keep permissions minimal, retain sensible timeouts/concurrency, and treat workflow changes as production/release-integrity code. Pull-request checks must not expose secrets to untrusted code.

## Release process

Before a version tag:

1. merge only a fully reviewed/verified intended head;
2. update `CHANGELOG.md` and version-facing docs;
3. verify all user-facing behavior docs match the shipped code;
4. inspect CI and CodeQL on that exact head;
5. decide/document unsigned artifact policy;
6. create the intended semantic `v*.*.*` tag;
7. inspect all runtime artifacts;
8. smoke-test supported RIDs with fictional data;
9. include rich editing, both duplicate survivor directions, import/export, backup/restore, settings, and delete safety in release smoke tests;
10. publish known limitations plainly.

Do not claim signing/notarization if the workflow does not perform it.

## Documentation maintenance

For every meaningful behavior change check README, docs index, user guide, architecture, data model, desktop UI, import/export, storage/security/testing/performance/troubleshooting, changelog, roadmap, repository reference, and `what_changed.md` as applicable.

A documentation mismatch is a release defect when it could cause data loss, unsafe restore/merge behavior, misunderstood encryption/privacy, or unsupported expectations.

## Repository hygiene

Before merge/release scan for accidental:

- live databases/WAL/SHM/backups/recovery files/exports;
- `.env` secrets;
- API tokens/passwords/database keys;
- signing certificates/private keys;
- real contact fixtures/screenshots;
- build output;
- IDE/user-specific files;
- temporary restore/preferences artifacts.

Update `.gitignore` when introducing a new generated or sensitive artifact type.

## Deprecation policy

When removing observable behavior/data support, document old/new behavior, preserve migration where reasonable, never silently discard fields, update changelog, consider compatibility periods for interchange formats, and record architecture/storage/privacy/security changes in an ADR when appropriate.
