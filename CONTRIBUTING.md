# Contributing to ContactCore

Thank you for contributing to ContactCore. Changes should be focused, testable, privacy-preserving, accessible, and easy to review. ContactCore stores personal contact data, so persistence/import/backup/editor changes deserve extra care even when the code change appears small.

## Before contributing

Read:

- `README.md` for current product scope and known limitations;
- `docs/README.md` for the documentation map;
- `docs/architecture.md` for layer boundaries;
- `docs/development.md` for engineering workflow;
- `docs/testing.md` for current test coverage and expectations;
- `docs/security.md` before changing input, database, backup, encryption, secrets, or diagnostics;
- `docs/maintainer-guide.md` for long-lived repository invariants.

By participating, follow `CODE_OF_CONDUCT.md`.

## Toolchain

Use a stable .NET 10 SDK compatible with `global.json`. From the repository directory:

```bash
dotnet --version
dotnet restore ContactCore.slnx
```

Package versions are centrally managed in `Directory.Packages.props`; shared compiler/analyzer rules are in `Directory.Build.props`.

## Branch workflow

1. Fork or clone the repository.
2. Start from the latest intended base branch, normally `main`.
3. Create a focused branch for one feature/fix/documentation area.
4. Keep unrelated formatting/dependency/refactor changes out of the same branch where practical.
5. Commit small coherent changes with descriptive messages.
6. Push and open a pull request using the repository template.

Conventional-commit style is encouraged, for example:

- `feat: add ...`
- `fix: prevent ...`
- `test: cover ...`
- `docs: explain ...`
- `ci: harden ...`
- `refactor: simplify ...`
- `chore: update ...`

## Architecture rules

Place changes at the lowest appropriate layer:

- `ContactCore.Domain` — pure contact model/validation/normalization.
- `ContactCore.Application` — use cases, contracts, duplicate/merge policy, codecs.
- `ContactCore.Infrastructure` — SQLite/filesystem/preferences/backup/native concerns.
- `ContactCore.Desktop` — Avalonia presentation, platform pickers/dialogs, composition.

Do not make Domain depend on Avalonia or SQLite. Do not make Application depend on a concrete Infrastructure implementation.

## Data-integrity requirements

A contact is persisted as a complete aggregate. Repository saves replace contact-owned child/link collections with the supplied aggregate state.

This has an important current UI implication: the rich domain/database supports more repeated fields than the current desktop draft exposes. If you modify contact editing, add tests proving existing unedited rich fields are preserved; do not accidentally turn a UI enhancement into data loss.

For SQLite changes:

- parameterize user/data values;
- preserve `LIKE` wildcard escaping where user search text should be literal;
- keep foreign keys enabled;
- keep single/bulk aggregate writes transactional;
- add a new ordered migration for schema changes;
- keep future-schema rejection;
- update backup/restore identity/compatibility rules and tests when schema changes.

## Import/export requirements

Treat imported text as untrusted input.

New/changed formats should have:

- supported-field documentation;
- escaping/round-trip tests;
- malformed-input tests;
- bounded resource behavior at an appropriate entry point;
- domain validation after parsing;
- atomic persistence for batches;
- privacy/security notes.

CSV/vCard are interchange formats, not a substitute for verified SQLite backup.

## Backup/restore requirements

Changes to `BackupService` must preserve equivalent safety to:

**verify source → snapshot active DB → stage → migrate/verify staging → switch → verify active → rollback if final verification fails**.

Use disposable fictional databases for tests. Never test destructive restore code against your real contact profile.

## Secrets and personal data

Never commit or post:

- real `contactcore.db` databases;
- WAL/SHM files;
- backups or recovery snapshots containing real contacts;
- real CSV/vCard exports;
- `.env` files with secrets;
- database encryption keys;
- passwords/tokens/API keys;
- signing private keys/certificates;
- screenshots containing real contacts, private notifications, or sensitive paths;
- private data copied into tests/issues/PRs.

Use fictional contacts and reserved/example domains in tests/documentation.

## UI and accessibility

UI changes should preserve:

- keyboard reachability;
- visible focus;
- clear text labels;
- sensible tab order;
- theme-aware resources;
- text/scaling usability;
- non-color-only status cues;
- destructive-action confirmation guarantees;
- reduced-motion preference for new custom animations.

Do not claim accessibility certification/conformance without the required audit evidence.

## Tests

Add a regression test at the lowest meaningful layer for behavior changes.

Run the full quality sequence before opening/updating a PR:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

CI repeats restore/format/build/test on Ubuntu, Windows, and macOS. CodeQL also runs on pull requests targeting `main`.

A local pass is not a substitute for the final PR head's GitHub checks.

## Documentation

Update documentation in the same PR as behavior changes. In particular:

- `README.md` for public capability/limitation changes;
- `CHANGELOG.md` for notable user-visible changes;
- relevant `docs/*.md` guides;
- ADRs for durable architecture/storage/security decisions;
- `docs/repository-reference.md` whenever tracked files are added/removed/renamed;
- `ROADMAP.md` when a planned item is completed/re-scoped;
- `what_changed.md` for continuation/handoff state where this repository workflow uses it.

Documentation must describe what the code actually does, including failure paths and known limitations.

## Pull-request description

Explain:

- problem and intended behavior;
- important design/layer choices;
- data/schema/backward-compatibility implications;
- security/privacy implications;
- tests added/updated;
- manual verification performed;
- documentation updated;
- known limitations or follow-up work.

Use the PR template rather than hiding important risk in commit messages only.

## Review expectations

Reviewers should pay special attention to:

- possible loss of unexposed contact child data;
- transaction/migration/restore rollback behavior;
- SQL/query construction;
- import parser boundaries;
- secret/PII leakage;
- platform-specific file behavior;
- accessibility/keyboard regressions;
- release workflow permissions/signing claims;
- whether documentation overclaims verification.

## Formatting-only changes

If changing repository-wide formatting policy, make that a dedicated change where practical. Broad formatting mixed with logic makes review and regression archaeology harder.

## Dependency changes

Change versions in `Directory.Packages.props`, read official release/security notes, run the full cross-platform checks, and consider native/licensing implications. Dependabot PRs require the same scrutiny as manual dependency updates.

## Security reports

Do not use a public issue/PR to disclose an undisclosed security vulnerability. Follow `SECURITY.md`.

## Licensing

By submitting a contribution, you agree it can be distributed under the repository's MIT license and that you have the right to submit the contributed material. Do not copy code/assets into the repository if their license is incompatible or unclear.
