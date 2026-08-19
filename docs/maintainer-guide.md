# Maintainer Guide

This guide covers the recurring engineering responsibilities for ContactCore maintainers. It complements `CONTRIBUTING.md`; contributors should follow the contribution rules, while maintainers are additionally responsible for preserving data compatibility, release quality, privacy promises, and repository hygiene.

## Core invariants

The following properties should be treated as product invariants unless an explicit architecture decision changes them:

1. ContactCore remains useful without a mandatory account, cloud service, telemetry service, or advertising service.
2. Domain rules do not depend on Avalonia or SQLite.
3. User-controlled SQL values remain parameterized.
4. Contact aggregate writes and bulk imports remain transactional.
5. Database schema upgrades are versioned and forward-only; unsupported future schemas are rejected.
6. Restore validates before replacement and retains a verified pre-restore recovery path.
7. Requested database encryption fails closed when a compatible provider is unavailable.
8. Runtime database keys are not serialized into normal preferences.
9. Destructive desktop actions do not silently bypass a configured confirmation requirement.
10. Documentation does not overclaim platform, accessibility, encryption, signing, or test verification.

## Branch and review workflow

Use a branch based on the latest `main`. Keep commits small enough that reviewers can understand one conceptual change at a time. Commit messages should explain the intent using a consistent prefix such as `feat:`, `fix:`, `test:`, `docs:`, `ci:`, `refactor:`, or `chore:`.

Before merging:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build
```

Then verify the GitHub CI matrix and CodeQL result on the final branch head. Do not treat an old green commit as verification of a newer head.

## Adding or changing domain fields

When modifying `Contact` or one of its child records:

1. decide whether the value is scalar, contact-owned repeated data, or a shared many-to-many concept;
2. update domain validation/normalization if needed;
3. add a new schema migration rather than rewriting historical migration meaning;
4. update repository load/write code;
5. update deep-copy and merge behavior where relevant;
6. update import/export formats only when backwards-compatible or explicitly versioned;
7. update desktop draft/editor behavior or document that the field is not currently editable;
8. add domain, repository, and desktop regression tests as appropriate;
9. update `data-model.md`, `desktop-ui.md`, and this repository's file reference.

## Database migrations

`DatabaseMigrator` is the schema authority. New migrations should have monotonically increasing integer versions.

A migration should be:

- deterministic;
- safe to run exactly once in version order;
- transactionally applied when SQLite supports the statements involved;
- compatible with databases created by previous supported versions;
- followed by any required integrity/identity checks;
- covered by upgrade-path tests using a database representing the previous schema.

Do not delete or renumber already released migration versions. Do not implement automatic downgrade migrations unless there is a separately reviewed design for them.

If a schema-family change is ever required, create a formal ADR because backup identity and restore safety depend on it.

## Backup/restore changes

Treat `BackupService` as high-risk code. Changes should preserve this safety order:

**verify source → snapshot current → stage → migrate/verify stage → switch → verify active → rollback if necessary**.

Tests should cover:

- corrupt SQLite input;
- valid non-ContactCore SQLite input;
- unsupported future schema;
- valid older supported schema migration;
- active-database snapshot creation;
- final verification failure and rollback;
- staging cleanup;
- recovery artifact preservation;
- keyed-database behavior when supported/testable.

Never optimize restore by removing verification stages without a documented threat/data-loss analysis.

## SQLite encryption boundary

The repository does not bundle a guaranteed SQLCipher provider. `CONTACTCORE_DATABASE_KEY` is only meaningful when the process actually uses a compatible native SQLite implementation.

Any encryption integration must:

- retain post-key cipher verification;
- avoid logging/interpolating the original secret;
- clearly document native provider licensing/distribution requirements;
- add platform-specific build/runtime tests;
- avoid committing proprietary native binaries or keys unless their licensing and repository policy explicitly permit it.

A UI checkbox that merely stores a key is not encryption and must never be presented as such.

## Preferences

`settings.json` is for non-secret local preferences. Persisted models should be backwards-tolerant: missing new fields should receive safe defaults. Writes should remain atomic enough to avoid obvious partial-file corruption.

If a preference can reduce data loss or security risk, choose a conservative default. `ConfirmPermanentDelete = true` is an example.

## Import formats

Import is an untrusted-input boundary. Changes should consider:

- parser termination on malformed input;
- bounded resource use;
- Unicode edge cases;
- validation after parsing;
- transactional batch persistence;
- warning/error distinction;
- formula/macro risks in downstream spreadsheet consumers;
- test fixtures that contain only fictional data.

If increasing the desktop's current 5,000,000-character limit, document the memory/performance reasoning and add large-input tests.

## Export formats

Be explicit about fidelity. CSV currently loses repeated/advanced fields and must not be marketed as a full backup. vCard support is also a deliberate subset.

When expanding a format, preserve previously exported data interpretation where possible and add round-trip tests for every newly supported field.

## Desktop editor changes

The current draft editor exposes only one phone and one email even though the model supports more. This is a particularly important maintenance hazard because repository saves replace the contact's child collections with the aggregate passed in.

Before presenting rich existing contacts as fully editable, expand `ContactDraftViewModel` to preserve/edit all child collections and add regression tests that prove opening/saving a rich contact does not drop unexposed data.

Until then, documentation must continue to state this limitation clearly.

## Search changes

Preserve:

- parameterized SQL;
- escaping of LIKE wildcard characters;
- archived/favorites semantics;
- cancellation in debounced desktop search;
- deterministic ordering.

Benchmark rather than guessing when introducing full-text search or pagination. If adopting FTS5, add an ADR covering schema/migration, tokenizer behavior, Unicode expectations, and index synchronization.

## Duplicate handling

Keep duplicate detection explainable and deterministic. A score change can alter user-visible candidate ordering, so accompany scoring changes with focused tests.

Merge logic must not reuse child primary keys from a secondary aggregate when that can collide with persisted rows. Test merges with overlapping and distinct phones, emails, addresses, organizations, groups, and tags.

The application layer currently has a merge engine while the main window lacks a full merge-review UI. Do not label the interactive merge workflow complete until that surface exists and is tested.

## Accessibility maintenance

For UI changes verify:

- keyboard reachability;
- visible focus;
- labels associated with controls where the framework/accessibility bridge supports it;
- sensible tab order;
- usable high-DPI/text scaling;
- non-color-only status cues;
- Light/Dark/System theme behavior;
- reduced-motion preference for any new custom motion.

Do not claim WCAG or platform accessibility certification without an actual audit appropriate to that claim.

## Dependency updates

Package versions are centralized in `Directory.Packages.props`. Before merging an update:

- read the dependency's official release/security notes;
- check target-framework compatibility;
- run the full CI matrix;
- inspect breaking changes involving Avalonia XAML/bindings, SQLite native behavior, MSTest, and coverage tooling;
- update documentation when behavior or prerequisites change.

Dependabot proposals are starting points, not automatic approval.

## GitHub Actions maintenance

Keep actions permissions minimal and retain job timeouts/concurrency. Be cautious with pull-request workflows and secrets. Workflow changes should be reviewed like production code because they can affect release integrity.

See `ci-cd.md` for the current workflow contract.

## Release process

Before creating a version tag:

1. merge only a verified `main` head;
2. update `CHANGELOG.md` and version-facing documentation;
3. confirm all user-visible docs match the shipped code;
4. run/inspect CI and CodeQL;
5. decide whether unsigned artifacts are acceptable for the release;
6. create a semantic tag matching `v*.*.*`;
7. inspect all four runtime artifacts;
8. smoke-test supported platform artifacts with fictional data;
9. verify create/edit/search/import/export/backup/restore/settings basics;
10. publish release notes that list known limitations instead of hiding them.

Do not claim signing/notarization if the workflow does not perform it.

## Security reports

Follow `SECURITY.md`. Do not ask reporters to post sensitive vulnerabilities or real contact data publicly. Minimize reproduction data and use fictional samples where possible.

## Documentation maintenance

For every meaningful behavior change ask:

- Does the README need to change?
- Does `docs/README.md` still route readers correctly?
- Does `repository-reference.md` list every tracked file?
- Do user, architecture, data, security, testing, release, or troubleshooting docs need changes?
- Does `what_changed.md` accurately describe the current continuation checkpoint?
- Does `CHANGELOG.md` need an entry?

A documentation mismatch is a release defect when it can cause users to lose data, misunderstand encryption/privacy, or rely on an unsupported workflow.

## Repository hygiene

Before merge/release, scan for accidental additions of:

- `contactcore.db`, WAL/SHM files, backups, exports;
- `.env` files with secrets;
- API tokens/keys/passwords;
- signing certificates/private keys;
- real contact fixtures/screenshots;
- build output (`bin`, `obj`, artifacts);
- IDE/user-specific files;
- temporary restore/preferences files.

Update `.gitignore` when a new generated/sensitive artifact type is introduced.

## Deprecation policy

When removing an externally observable behavior or data format:

- document the old behavior and replacement;
- preserve data migration where reasonable;
- avoid silently discarding fields;
- add changelog notes;
- consider a compatibility period for exported/imported formats;
- use an ADR when the change alters architecture, storage, privacy, or security promises.
