# ContactCore Documentation

This directory is the engineering and product documentation hub for ContactCore. The documents are written against the repository implementation on the active hardening branch and are intended for users, contributors, reviewers, release maintainers, and security auditors.

## Start here

- [Platform support](platform-support.md) — Windows, Linux, macOS, Android, iOS/iPadOS, WebAssembly, ChromeOS routes, persistence differences, workloads, CI, and distribution boundaries.
- [Setup](setup.md) — prerequisites, clone/build/run instructions, platform workloads, data paths, and environment configuration.
- [User guide](user-guide.md) — everyday workflows: contacts, search, favorites, archive/delete, import/export, backup/restore, settings, and keyboard behavior.
- [Architecture](architecture.md) — layer boundaries, platform heads, native/browser persistence split, dependency direction, request/data flows, and design constraints.
- [Data model](data-model.md) — aggregate fields, SQLite schema, relationships, indexes, timestamps, and schema identity.
- [Storage, backup, and recovery](storage-backup-recovery.md) — native local files, atomicity, backup verification, staged restore, rollback behavior, browser-storage boundary, and recovery artifacts.
- [Import and export](import-export.md) — CSV/vCard formats, supported fields, validation, warnings, transactional import, and privacy considerations.
- [Desktop UI](desktop-ui.md) — mature desktop Avalonia shell, view models, dialogs, settings, accessibility, and desktop actions.
- [Repository reference](repository-reference.md) — tracked-file reference and responsibilities.
- [Development](development.md) — coding rules and contribution workflow.
- [Testing](testing.md) — test projects, current coverage areas, platform build gates, CI commands, and test-design expectations.
- [CI/CD](ci-cd.md) — three-OS core CI, Android/iOS/WebAssembly builds, CodeQL, release workflow, artifacts, and branch/check behavior.
- [Security](security.md) — threat model, SQLite/SQLCipher boundary, browser-storage boundary, secret handling, input/data risks, and disclosure guidance.
- [Accessibility](accessibility.md) — keyboard, focus, labels, motion, themes, responsive UI, and manual verification expectations.
- [Performance](performance.md) — current performance characteristics, known scaling limits, and benchmark targets.
- [Troubleshooting](troubleshooting.md) — common setup, workload, database, encryption, import, browser-storage, and recovery failures.
- [Release](release.md) — versioning/tagging, desktop/browser publishing, mobile build/signing boundaries, and release verification.
- [Maintainer guide](maintainer-guide.md) — repository ownership tasks, migrations, release hygiene, dependency/workload updates, and documentation maintenance.
- [Architecture decision records](adr/) — durable decisions and tradeoffs.

## Documentation principles

1. **Code is authoritative.** Documentation must be corrected when implementation changes.
2. **Do not overclaim verification.** A target can exist and compile without every release platform/device/browser combination having been manually verified or store-certified.
3. **Privacy by default.** Examples must use fictional contacts and must not contain real databases, exports, keys, addresses, or personal data.
4. **Document failure paths.** Backup/restore, browser persistence, imports, migrations, encryption, and destructive actions must describe what happens when an operation fails.
5. **Keep file-level traceability.** When files are added, renamed, or removed, update `repository-reference.md` and this index where relevant.
6. **Separate native and browser persistence claims.** Native targets use SQLite; WebAssembly uses browser-managed IndexedDB and must not be described as having native SQLite backup/restore.
7. **Separate build support from distribution signing.** Android/iOS build targets do not imply committed store signing identities or completed store certification.

## Repository-level documents

The repository root also contains `README.md`, `CHANGELOG.md`, `ROADMAP.md`, `SECURITY.md`, `PRIVACY.md`, `SUPPORT.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE`, and `what_changed.md`. Those documents cover project presentation, release history, roadmap, policies, contribution rules, legal terms, and the current work handoff.
