# User Guide

ContactCore is an offline-first desktop contact manager. It does not require an account, mandatory cloud synchronization, telemetry, or advertising services. Contact data is stored in the local ContactCore data directory.

## First launch

On startup, ContactCore resolves its local data directory, loads local preferences, configures the requested theme, opens the SQLite database, applies supported migrations, and then loads the desktop workspace. If database encryption is requested but a SQLCipher-compatible provider is unavailable, startup fails rather than silently creating or opening plaintext data.

## Contact workspace

The main workspace is built around a searchable contact list and an editor/detail surface. A contact can contain:

- given name, family name, and nickname;
- birthday and notes;
- favorite and archived state;
- multiple phone numbers;
- multiple email addresses;
- multiple postal addresses;
- multiple organization affiliations;
- groups and tags;
- created/updated timestamps maintained by the application.

A contact whose name fields are empty is displayed as `Unnamed contact` by the domain model.

## Create and edit

Use the new-contact action to start a draft. Fill only the fields that are useful; repeated fields can be added or removed in the editor. Saving normalizes surrounding whitespace, validates the contact, updates its modification timestamp, and writes the full aggregate transactionally.

Validation currently enforces practical length limits for names and notes, basic email validity, and a permissive phone-character/length pattern. Validation errors identify the field without echoing potentially sensitive invalid values.

## Search and filters

Search matches given name, family name, nickname, phone number, and email address. The repository also supports:

- favorites-only filtering;
- hiding/showing archived contacts;
- exact case-insensitive tag filtering;
- exact case-insensitive group filtering;
- alphabetical `StartsWith` filtering.

Search input is trimmed before execution. SQL wildcard characters entered by the user are escaped so they are treated as search text instead of changing the intended query pattern.

## Favorites

Favoriting changes the contact's `IsFavorite` state and persists the updated aggregate. The favorites filter uses the persisted flag, so it is preserved across restarts.

## Archive and permanent delete

Archive is the reversible removal path: archived contacts remain in the database but are excluded by normal searches unless archived records are requested.

Permanent delete removes the contact row. SQLite foreign keys cascade deletion to contact-owned rows and relationship rows. The desktop settings include a local preference to confirm before permanent deletion; the default is enabled.

For important contacts, create a backup before irreversible cleanup.

## Duplicate review and merge

Duplicate detection assigns a score based on matching normalized names, phone numbers, and email addresses. The minimum-score input is clamped to the valid `0..1` range.

Merge keeps the selected primary contact identity, fills missing primary name/nickname fields from the secondary contact, combines notes when they differ, preserves favorite state, and merges repeated fields while avoiding equivalent values. New IDs are generated when phone/email/address/organization rows are copied from the secondary contact so child-row primary keys do not collide. A contact cannot be merged with itself.

Review the preview before confirming a merge because merge is a data-changing operation.

## CSV import/export

CSV export writes these columns:

`GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes`

Only the first email and first phone are represented by the current CSV codec. Fields are quoted and embedded quotes are doubled. Birthday uses `yyyy-MM-dd`.

CSV import is header-driven and case-insensitive. Unknown columns are ignored. Invalid birthday text produces a warning. Parsed contacts are normalized and validated before persistence. The application bulk-import path validates the entire batch before calling the repository, and the repository writes the batch in one SQLite transaction.

See [Import and export](import-export.md) for format details and limitations.

## vCard import/export

The vCard codec writes vCard 4.0 records with `N`, `FN`, repeated `TEL`, repeated `EMAIL`, optional `BDAY`, and optional `NOTE`. Import supports those fields and unfolded continuation lines. An unterminated vCard is ignored with a warning.

The codec is intentionally focused; it is not a complete implementation of every vCard property or parameter defined by the standard.

## Backup

Creating a backup uses SQLite's backup API rather than copying a potentially active WAL-mode database file. The generated backup is then checked with `PRAGMA integrity_check` and ContactCore schema-identity validation before the path is returned as a successful backup.

Backup filenames are timestamped and include a random identifier to prevent collisions.

## Restore and recovery

Restore is deliberately conservative:

1. The selected file must exist and cannot be the active database itself.
2. It is opened read-only and checked for SQLite integrity and ContactCore identity before the active database is touched.
3. If an active database exists, ContactCore creates a verified pre-restore recovery snapshot.
4. The selected backup is copied to a staging file.
5. Supported migrations are applied to the staging copy.
6. The staged database is integrity/identity checked again.
7. SQLite pools and sidecar files are cleared, then the staged file replaces the active database.
8. The new active database is verified once more.
9. If final verification fails, the failed restore is retained in the backups directory and the verified pre-restore snapshot is copied back into place.

This process reduces the chance that a corrupt, foreign, or incompatible backup can replace the only good copy of the user's data.

## Settings

Preferences are local and include:

- Theme: System, Light, or Dark.
- Reduced motion preference.
- Confirmation before permanent deletion.

The database key is runtime-only and is not serialized into `settings.json`.

Preference writes use a temporary file followed by replacement to reduce the chance of leaving a partially written JSON settings file.

## Data location

By default the data directory is `ContactCore` under the operating system's local application-data location. If that platform API returns no path, the application falls back to its base directory.

`CONTACTCORE_DATA_PATH` can override the directory. The database is always named `contactcore.db` inside the selected directory; settings use `settings.json`; automatic recovery artifacts are placed under `backups/`.

## Encryption

The default open-source build uses ordinary SQLite. `CONTACTCORE_DATABASE_KEY` requests keyed SQLite behavior. ContactCore encodes the runtime key for `PRAGMA key`, then checks `PRAGMA cipher_version`. If no compatible cipher provider is active, the connection is closed and the application reports an error.

Do not put real keys in `.env.example`, source files, issues, screenshots, logs, or committed configuration.

## Privacy habits

- Keep real exports and database files out of the Git repository.
- Use fictional data in screenshots and bug reports.
- Remove sensitive values from diagnostic material before posting publicly.
- Store backups in a location appropriate for the sensitivity of the contacts they contain.
- Remember that a normal backup has the same confidentiality requirements as the live database unless the underlying SQLite provider encrypts it.

## Keyboard and accessibility

The desktop UI is designed around labeled fields, keyboard navigation, visible focus, scalable text, theme support, and a reduced-motion preference. `Ctrl+F` focuses search in the desktop workflow. Accessibility should still be manually verified on each supported operating system and with representative assistive technology before making conformance claims.
