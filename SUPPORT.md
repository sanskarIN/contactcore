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

## Rich editor limitation and preservation

The current desktop editor exposes one visible phone and one visible email, while the underlying model/database can contain additional phones/emails plus addresses, organizations, groups, and tags.

Current branch code preserves those unexposed values when a contact is opened and saved through the compact editor. The draft begins from a deep copy of the complete aggregate and regression tests verify preservation when the visible primary phone/email are edited or cleared.

The remaining limitation is that those additional values cannot yet be directly edited from the main UI. If a current build reproducibly drops them, treat that as a data-integrity regression: stop repeated edits, preserve the database/backups, record the exact version/commit, and reproduce with fictional data before reporting it.

For older builds created before the preservation fix, `docs/troubleshooting.md` includes recovery guidance.

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
