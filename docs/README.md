# ContactCore Documentation

This directory is the engineering and product documentation hub for ContactCore. The documents are written against the repository implementation on the active hardening branch and are intended for users, contributors, reviewers, release maintainers, and security auditors.

## Start here

- [Setup](setup.md) — prerequisites, clone/build/run instructions, data paths, and environment configuration.
- [User guide](user-guide.md) — everyday workflows: contacts, search, favorites, archive/delete, import/export, backup/restore, settings, and keyboard behavior.
- [Architecture](architecture.md) — layer boundaries, dependency direction, request/data flows, and design constraints.
- [Data model](data-model.md) — aggregate fields, SQLite schema, relationships, indexes, timestamps, and schema identity.
- [Storage, backup, and recovery](storage-backup-recovery.md) — local files, atomicity, backup verification, staged restore, rollback behavior, and recovery artifacts.
- [Import and export](import-export.md) — CSV/vCard formats, supported fields, validation, warnings, transactional import, and privacy considerations.
- [Desktop UI](desktop-ui.md) — Avalonia composition, view models, dialogs, settings, accessibility, and desktop actions.
- [Repository reference](repository-reference.md) — file-by-file reference for every tracked file in the repository.
- [Development](development.md) — coding rules and contribution workflow.
- [Testing](testing.md) — test projects, current coverage areas, CI commands, and test-design expectations.
- [CI/CD](ci-cd.md) — CI, CodeQL, release workflow, artifacts, and branch/check behavior.
- [Security](security.md) — threat model, SQLite/SQLCipher boundary, secret handling, input/data risks, and disclosure guidance.
- [Accessibility](accessibility.md) — keyboard, focus, labels, motion, themes, and manual verification expectations.
- [Performance](performance.md) — current performance characteristics, known scaling limits, and benchmark targets.
- [Troubleshooting](troubleshooting.md) — common setup, database, encryption, import, and recovery failures.
- [Release](release.md) — versioning/tagging, publishing targets, and release verification.
- [Maintainer guide](maintainer-guide.md) — repository ownership tasks, migrations, release hygiene, dependency updates, and documentation maintenance.
- [Architecture decision records](adr/) — durable decisions and tradeoffs.

## Documentation principles

1. **Code is authoritative.** Documentation must be corrected when implementation changes.
2. **Do not overclaim verification.** A feature may be implemented without being manually verified on every supported operating system.
3. **Privacy by default.** Examples must use fictional contacts and must not contain real databases, exports, keys, addresses, or personal data.
4. **Document failure paths.** Backup/restore, imports, migrations, encryption, and destructive actions must describe what happens when an operation fails.
5. **Keep file-level traceability.** When files are added, renamed, or removed, update `repository-reference.md` and this index where relevant.

## Repository-level documents

The repository root also contains `README.md`, `CHANGELOG.md`, `ROADMAP.md`, `SECURITY.md`, `PRIVACY.md`, `SUPPORT.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE`, and `what_changed.md`. Those documents cover project presentation, release history, roadmap, policies, contribution rules, legal terms, and the current work handoff.
