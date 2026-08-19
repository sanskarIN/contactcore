# Support

For normal usage/development questions, start with the repository documentation before sharing diagnostic data. ContactCore can contain personal information, so a good support request is both reproducible and privacy-minimized.

## Start here

- [`docs/README.md`](docs/README.md) — full documentation index.
- [`docs/setup.md`](docs/setup.md) — SDK/build/run/environment/data-path setup.
- [`docs/user-guide.md`](docs/user-guide.md) — application workflows and current limitations.
- [`docs/troubleshooting.md`](docs/troubleshooting.md) — failure diagnosis and safe recovery guidance.
- [`docs/security.md`](docs/security.md) — encryption/data-safety details.
- [`docs/storage-backup-recovery.md`](docs/storage-backup-recovery.md) — backup/restore behavior.

## Contact channels

- Business: **sanskarin@outlook.in**
- Business: **sanskarin.business@gmail.com**
- Support: **supportramsandesh@gmail.com**
- GitHub profile: https://github.com/sanskarIN
- Buy Me a Coffee: https://buymeacoffee.com/sanskarIN

For ordinary reproducible bugs/features, use the repository issue templates when appropriate. For an undisclosed security vulnerability, **do not open a public issue**; follow `SECURITY.md`.

## Good bug report

Include:

- ContactCore release/commit;
- operating system/version and CPU architecture;
- whether you built from source or used a release artifact;
- `dotnet --info` for source-build problems (review/redact paths first);
- exact steps to reproduce;
- expected behavior;
- actual behavior;
- sanitized error/status text;
- whether `CONTACTCORE_DATA_PATH` is set;
- whether `CONTACTCORE_DATABASE_KEY` or a custom SQLite provider is involved;
- minimal fictional input file/database when truly needed.

Use the bug-report template so important context is not missed.

## Never post/send casually

Do not attach or paste:

- your real `contactcore.db`;
- `-wal`/`-shm` database sidecars;
- real backups or `pre-restore-*`/`failed-restore-*` files;
- real CSV/vCard contact exports;
- encryption keys;
- `.env` secrets;
- passwords/tokens;
- signing private keys/certificates;
- screenshots containing real contacts/private notifications;
- another person's personal data.

If a data-shaped reproducer is required, create a tiny disposable ContactCore profile with clearly fictional records.

## Sanitizing diagnostics

ContactCore performs limited defense-in-depth redaction for common email/phone/long-number patterns in desktop error messages, but this is **not** a complete privacy scrubber.

Before posting diagnostic text manually check for:

- names;
- addresses;
- email/phone formats that redaction missed;
- usernames/home-directory paths;
- backup/data paths;
- environment variables;
- keys/tokens;
- organization/private notes.

## Database/restore problems

Do not delete or overwrite recovery files merely to make the app start.

If a restore problem involves important data:

1. stop repeated destructive attempts;
2. preserve the active database and `backups/` directory;
3. read `docs/troubleshooting.md` and `docs/storage-backup-recovery.md`;
4. reproduce the issue separately with fictional data if possible;
5. share only sanitized structural/error information publicly.

The restore service can retain `pre-restore-*` and `failed-restore-*` files specifically so recovery remains possible after a failed switch.

## Encryption setup problems

If `CONTACTCORE_DATABASE_KEY` is set without a SQLCipher-compatible provider, ContactCore intentionally refuses to proceed after `cipher_version` verification fails. This is not solved safely by suppressing the check.

If encryption is not intended, remove the environment variable. If encryption is intended, consult `docs/security.md` and ADR `docs/adr/0003-encryption-provider.md`.

## Rich editor data-integrity problems

ContactCore 2.0.12 directly exposes all repeated collections in the current persisted aggregate: phones, emails, addresses, organizations, groups, and tags. Existing repeated rows are expected to retain their IDs through ordinary edits, exact group/tag names must survive unchanged, and removing one row must not remove unrelated values.

If a current 2.0.12 build reproducibly loses or changes an unchanged rich field, treat that as a **data-integrity regression**, not an accepted editor limitation:

1. stop repeated edits to the affected contact;
2. preserve the active database and verified backups;
3. record the exact release/commit;
4. reproduce with a disposable profile and fictional data when possible;
5. report the smallest privacy-safe reproduction.

The current intentional rich-editor limitations are narrower: repeated fields support add/edit/remove but not drag/drop reordering, and groups/tags do not yet have a separate global taxonomy-management screen.

## Duplicate merge problems

Duplicate detection is advisory until the user explicitly chooses a survivor and confirms the destructive merge. The repository requires both reviewed contacts to still exist inside the merge transaction.

If either reviewed record disappeared before commit, the merge should be rejected rather than partially committed or recreating a stale primary. If that invariant is violated, preserve the database/backups and report a fictional reproduction.

There is no general-purpose undo stack for an already confirmed merge; use verified backups for recovery when appropriate.

## Feature requests

A useful feature request explains:

- the user problem;
- why current behavior is insufficient;
- expected workflow;
- privacy/offline implications;
- platform needs;
- whether it changes the data model/import/export format.

Avoid presenting a proposed implementation as the only acceptable solution when the underlying user need could be solved more safely/simply.

## Development support

For contributor questions include the failing command and focused diff/context. Run:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build
```

Read `CONTRIBUTING.md` and `docs/development.md` before proposing a broad architecture/dependency change.

## Security reports

Follow [`SECURITY.md`](SECURITY.md). Keep undisclosed vulnerability details private until coordinated disclosure is appropriate.
