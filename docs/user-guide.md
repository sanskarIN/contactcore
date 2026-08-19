# User Guide

ContactCore is an offline-first desktop contact manager. It does not require an account, mandatory cloud synchronization, telemetry, or advertising services. Contact data is stored in the local ContactCore data directory.

## First launch

On startup, ContactCore resolves its local data directory, loads local preferences, configures the requested theme, opens the SQLite database, applies supported migrations, and then loads the desktop workspace. If database encryption is requested but a SQLCipher-compatible provider is unavailable, startup fails rather than silently creating or opening plaintext data.

## Contact data capabilities

The ContactCore domain and storage model can represent:

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

### Current desktop-editor scope

The present desktop editor is intentionally simpler than the full domain/storage model. It exposes names, birthday, **one phone**, **one email**, notes, Favorite, and Archived. It does not yet provide full multi-value editing for addresses, organizations, groups, tags, or additional phone/email entries.

When an existing rich contact is loaded, the draft retains a deep copy of the complete aggregate. Saving through the compact editor changes the fields that are exposed while preserving additional phones/emails and all addresses, organizations, groups, and tags that the editor does not currently expose. Editing the visible phone/email updates the existing primary item's value while keeping its ID/label/kind; clearing it removes only that primary item and keeps any remaining values.

This preservation safeguard prevents hidden rich fields from being dropped by an unrelated compact edit. It is still not full rich-field **editing**—additional values are preserved but cannot yet be directly modified from the current main editor. See [Desktop UI](desktop-ui.md) for the exact behavior.

## Create and edit

Use **New contact** or `Ctrl+N` to start a draft. Fill the fields that are useful and choose **Save** or `Ctrl+S`. Birthday, when supplied, must use `yyyy-MM-dd`.

Saving normalizes surrounding whitespace, validates the contact, updates its modification timestamp, and writes the resulting aggregate transactionally. Existing rich child data retained by the draft is included in that complete aggregate write.

Validation currently enforces practical length limits for names and notes, basic email validity, and a permissive phone-character/length pattern. Validation errors identify the field without echoing potentially sensitive invalid values.

## Search and filters

Search matches given name, family name, nickname, phone number, and email address. The repository also supports:

- favorites-only filtering;
- hiding/showing archived contacts;
- exact case-insensitive tag filtering at the repository/API layer;
- exact case-insensitive group filtering at the repository/API layer;
- alphabetical `StartsWith` filtering.

The current sidebar exposes All, Favorites, Archived, and A–Z filtering. Search input is trimmed before execution. SQL wildcard characters entered by the user are escaped so they are treated as search text instead of changing the intended query pattern.

Search is debounced for 180 ms and older pending searches are cancelled when a newer query is entered. `Ctrl+F` focuses the search box.

## Favorites

The editor's **Favorite** checkbox changes the contact's `IsFavorite` state when saved. The favorites filter uses the persisted flag, so it is preserved across restarts.

## Archive and permanent delete

Archive is the reversible removal path: archived contacts remain in the database but are excluded by normal searches unless archived records are requested. The editor exposes an **Archived** checkbox.

Permanent delete removes the contact row. SQLite foreign keys cascade deletion to contact-owned rows and relationship rows. The desktop settings include a local preference to confirm before permanent deletion; the default is enabled. If confirmation is required but the platform confirmation service is unavailable, the application blocks deletion rather than bypassing confirmation.

For important contacts, create a backup before irreversible cleanup.

## Duplicate detection and merge support

The current **Find duplicates** command scans all contacts, including archived contacts, and reports the number of likely duplicate pairs plus the highest score. It does not yet present a complete pair-by-pair review/merge screen.

At the application layer, duplicate detection assigns a score using normalized names, phone numbers, and email addresses. The minimum score is clamped to `0..1`. A deterministic `ContactMerger` also exists: it keeps the selected primary identity, fills missing name/nickname fields, combines distinct notes, preserves favorite state, de-duplicates repeated values, generates fresh IDs for copied child rows where needed, and rejects self-merge.

The interactive duplicate-review/merge experience is still a UI roadmap item; do not interpret the existing merge engine as a completed merge screen.

## CSV import/export

CSV export writes these columns:

`GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes`

Only the first email and first phone are represented by the current CSV codec. Fields are quoted and embedded quotes are doubled. Birthday uses `yyyy-MM-dd`.

CSV import is header-driven and case-insensitive. Unknown columns are ignored. Invalid birthday text produces a warning. Parsed contacts are normalized and validated before persistence. The application bulk-import path validates the entire batch before calling the repository, and the repository writes the batch in one SQLite transaction.

The desktop importer reads UTF-8 text with BOM detection and enforces a maximum size of **5,000,000 characters**. Oversized files are rejected with a controlled error.

CSV quoting does not currently implement spreadsheet-specific formula neutralization, so treat exported contact text as untrusted when opening it in spreadsheet software.

See [Import and export](import-export.md) for format details and limitations.

## vCard import/export

The vCard codec writes vCard 4.0 records with `N`, `FN`, repeated `TEL`, repeated `EMAIL`, optional `BDAY`, and optional `NOTE`. Import supports those fields and unfolded continuation lines. An unterminated vCard is ignored with a warning.

The codec is intentionally focused; it is not a complete implementation of every vCard property or parameter defined by the standard.

## Export behavior

The Data tools surface can export CSV or vCard. Export includes archived contacts and runs only after you choose a destination. Text is written as UTF-8 without a BOM.

Export is an interchange function, not a full-fidelity backup. Use the SQLite backup function when you need to preserve the complete ContactCore database model.

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

Restore always requires the desktop confirmation callback. If the storage provider does not expose a local path for the chosen backup, the desktop layer creates a temporary local copy and deletes that picker copy after the restore attempt.

This process reduces the chance that a corrupt, foreign, or incompatible backup can replace the only good copy of the user's data. See [Storage, backup, and recovery](storage-backup-recovery.md) for the failure-path details.

## Settings

Preferences are local and include:

- Theme: System, Light, or Dark.
- Reduced motion preference.
- Confirmation before permanent deletion.

The database key is runtime-only and is not serialized into `settings.json`.

Preference writes use a temporary file followed by replacement to reduce the chance of leaving a partially written JSON settings file.

The reduced-motion preference is persisted for present/future UI behavior, but its existence is not a claim that every framework-level animation is disabled.

## Data location

By default the data directory is `ContactCore` under the operating system's local application-data location. If that platform API returns no path, the application falls back to its base directory.

`CONTACTCORE_DATA_PATH` can override the **directory**. The database is named `contactcore.db` inside the selected directory; settings use `settings.json`; automatic recovery artifacts are placed under `backups/`.

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

Current application shortcuts are:

- `Ctrl+N` — new contact;
- `Ctrl+S` — save;
- `Ctrl+F` — focus search;
- `Esc` — close/cancel the active editing/settings/data-tools surface.

The desktop UI uses labeled fields, keyboard navigation, visible focus styling, theme-aware dynamic resources, and a reduced-motion preference. Accessibility must still be manually verified on each supported operating system and with representative assistive technology before making conformance claims.
