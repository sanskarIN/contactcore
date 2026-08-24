# Privacy

ContactCore is designed as a **local-first** desktop contact manager. Normal contact management does not require a cloud account, remote synchronization service, telemetry endpoint, advertising SDK, or analytics dependency.

This document describes the repository's current privacy behavior. If future versions add optional networking/synchronization, this policy must be updated before release.

## What contact data can contain

The underlying ContactCore model can store:

- given/family names and nicknames;
- phone numbers;
- email addresses;
- postal addresses;
- birthdays;
- notes;
- organizations/job information;
- favorite/archive state;
- groups and tags;
- local created/updated timestamps.

The current desktop editor exposes only a subset of those rich fields; storage may still contain richer data imported/created by other application paths or future versions.

## Where data is stored

By default, ContactCore stores application data in the operating system's local application-data location under a `ContactCore` directory. The application derives:

- `contactcore.db` — active SQLite database;
- `settings.json` — non-secret local preferences;
- `backups/` — verified/pre-restore/failed-restore database artifacts created by backup/recovery workflows.

`CONTACTCORE_DATA_PATH` can override the local data **directory**.

The Settings screen shows the resolved local data directory.

## Network behavior

The current application does not require a remote service for normal contact operations and does not contain a mandatory telemetry/analytics/advertising pipeline.

The repository itself uses GitHub for source code, CI, releases, issues, and update/dependency development workflows; that is separate from normal runtime handling of a user's contact database.

If you download dependencies/releases or interact with GitHub/support channels, those external services have their own privacy practices.

## How data leaves ContactCore

Contact information can leave the active local database when the user explicitly performs actions such as:

- CSV export;
- vCard export;
- verified database backup;
- choosing a destination managed/synchronized by the operating system or a third-party storage provider;
- copying/screenshotting displayed information outside the app;
- sending diagnostic/reproduction information to another person/service.

ContactCore cannot control what a user-selected external folder/provider does with an exported/backup file.

## Backups

Database backups are complete sensitive data copies. The backup/recovery directory may also contain:

- `pre-restore-*.db` verified snapshots of the database before restore;
- `failed-restore-*.db` copies retained when a switched restore fails final verification.

Protect these files with the same care as the active database. Deleting a contact from the active database does not delete that contact from older backups or exports.

## CSV/vCard exports

Exports are portability/interoperability files and may be easier for other applications to read than the SQLite database.

CSV currently contains a limited field set including first phone/email, while vCard is a focused subset. Neither should be treated as automatically encrypted.

Be careful when opening CSV containing untrusted contact text in spreadsheet software; the current exporter performs standard CSV quoting but does not implement spreadsheet-specific formula neutralization.

## Database encryption

The default open-source runtime uses ordinary SQLite and should be considered plaintext at rest unless a compatible SQLCipher-style provider is deliberately integrated.

`CONTACTCORE_DATABASE_KEY` requests keyed SQLite behavior. ContactCore then checks `PRAGMA cipher_version`; if a compatible provider is not active, startup/database open fails instead of silently proceeding with plaintext while implying encryption.

The runtime database key is not serialized into normal `settings.json` preferences.

Encryption does not protect against every local threat: a process that can read the user's process memory/environment may still access a runtime key or decrypted data.

See `docs/security.md` and `docs/adr/0003-encryption-provider.md`.

## Preferences

`settings.json` currently stores non-secret preferences such as:

- theme;
- reduced-motion preference;
- confirm-before-permanent-delete preference.

Malformed preference JSON falls back to safe defaults. The database key is intentionally excluded from the serialized model.

## Temporary files

When a platform storage picker provides a backup as a stream rather than a local path, the desktop layer may create a unique temporary `.db` copy for restore and attempt to delete it after the operation.

Abrupt termination or filesystem/permission errors can leave temporary data behind. Such files should be treated as sensitive if discovered.

## Diagnostics

Caught desktop workflow errors are passed through a defense-in-depth sanitizer that replaces common email-shaped and long phone/number-shaped text and truncates long messages.

This is not a complete personal-data classifier. Names, addresses, unusual identifiers, paths, or secrets that do not match those patterns can still appear if upstream exception messages include them. Public bug reports should therefore be manually reviewed/redacted.

## Public repository and support

Never attach a real ContactCore database, backup, export, encryption key, or other person's personal information to a public GitHub issue/discussion/pull request.

For support, prefer:

- application version/commit;
- OS/architecture;
- sanitized error text;
- minimal reproduction using fictional contacts;
- exact steps/commands.

See `SUPPORT.md`.

## Screenshots

Project/release screenshots should use clearly fictional contacts. Review images for private notifications, usernames, full local paths, or other unintended metadata/content before posting.

## Data deletion

Permanent delete removes a contact from the active SQLite database and cascades its active child/link rows. It does not retroactively erase copies from:

- earlier database backups;
- pre-restore/failed-restore recovery copies;
- CSV/vCard exports;
- user-selected external/cloud-synced folders;
- screenshots/messages sent elsewhere.

Users are responsible for deleting those separate copies when appropriate.

## No sale/advertising use in the current app

The current runtime has no advertising dependency and no code path intended to sell contact data. If the product direction changes, privacy documentation and architecture/security review must change before release rather than silently altering this promise.

## Developer privacy requirements

Contributors must:

- use fictional data in tests/docs/screenshots;
- keep databases/backups/exports ignored;
- avoid sensitive values in exception messages/logs;
- preserve runtime-key non-persistence;
- document any new external network/data flow;
- treat import files as untrusted;
- update this policy when data collection/storage/transmission changes.

For the deeper technical threat model, see [`docs/security.md`](docs/security.md).
