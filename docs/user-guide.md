# User Guide

ContactCore is an offline-first desktop contact manager. It does not require an account, mandatory cloud synchronization, telemetry, or advertising services. Contact data is stored in the local ContactCore data directory unless you explicitly export or back it up elsewhere.

## First launch

On startup, ContactCore resolves its local data directory, loads local preferences, configures the requested theme, opens the SQLite database, applies supported migrations, and loads the desktop workspace.

If `CONTACTCORE_DATABASE_KEY` requests database encryption but the active SQLite provider cannot report SQLCipher-compatible cipher support, startup fails instead of silently continuing with plaintext behavior.

## Contact data supported by the editor

The current desktop editor can create and edit:

- given name, family name, and nickname;
- birthday and notes;
- favorite and archived state;
- multiple phone numbers with label and field kind;
- multiple email addresses with label and field kind;
- multiple postal addresses;
- multiple organization affiliations;
- multiple groups;
- multiple tags.

Repeated values appear as independent add/edit/remove rows. Existing row identities are preserved through ordinary edits, and removing one row does not remove unrelated values.

Group and tag names are also independent rows. Names containing commas or semicolons are stored as exact names rather than being split by a delimiter parser. Case-insensitive duplicate names entered for one contact are collapsed when the draft is converted for saving.

A contact whose name fields are empty is displayed as `Unnamed contact` by the domain model, subject to validation rules for any data being saved/imported.

## Create a contact

Choose **New contact** or press `Ctrl+N`.

The new draft has an application-generated identity but is explicitly marked **not yet persisted**. This distinction matters for destructive actions: choosing **Delete / discard** on an unsaved draft simply discards it and never performs a database delete.

Add any repeated rows you need and choose **Save**. Birthday, when present, must use `yyyy-MM-dd`.

Blank newly added phone/email/address/organization/group/tag rows are ignored according to the row's required-value semantics instead of creating meaningless child records.

## Edit a contact

Select a contact in the middle pane. The right pane loads the complete editable aggregate.

You can:

- edit scalar identity/contact fields;
- add/remove/edit multiple phones and emails;
- add/remove/edit addresses;
- add/remove/edit organization rows;
- add/remove/edit groups and tags;
- change Favorite/Archived state;
- edit notes.

Saving preserves the contact ID, creation timestamp, and IDs of repeated rows that remain. This avoids unnecessary identity churn in aggregate child tables.

Repeated rows are currently add/edit/remove; drag-to-reorder is not implemented.

## Saving and validation

ContactCore trims/normalizes supported text fields in the application service, refreshes `UpdatedAt`, validates the contact, and persists the complete aggregate transactionally.

Validation includes practical name/note length constraints, basic email validation, and a permissive phone-character/length pattern. Error messages identify the affected field without intentionally echoing invalid private values.

`Ctrl+S` saves only while the contact editor is actually visible. It does not invoke contact save from Settings, Data Tools, or Duplicate Review.

## Search and filters

Search matches given name, family name, nickname, phone number, and email address. The repository also supports:

- favorites-only filtering;
- hiding/showing archived contacts;
- exact case-insensitive tag filtering at the repository/API layer;
- exact case-insensitive group filtering at the repository/API layer;
- alphabetical `StartsWith` filtering.

The current sidebar exposes All, Favorites, Archived, and A–Z filtering. Search input is trimmed before execution. SQL wildcard characters entered by the user are escaped so `%` and `_` behave as literal search text instead of silently broadening the intended query.

Search is debounced for 180 ms and older pending searches are cancelled when newer input arrives. `Ctrl+F` focuses the search box.

## Favorites

The editor's **Favorite** checkbox changes persisted `IsFavorite`. The Favorites view filters by that stored value.

## Archive

Archive is the reversible removal path. Archived contacts stay in the database but are excluded by normal searches unless archived records are requested. Use the editor's **Archived** checkbox to change this state.

## Permanent delete

**Delete / discard** behaves differently for persisted and unsaved contacts:

- **unsaved draft** — discarded locally; no database delete and no permanent-delete confirmation;
- **persisted contact** — permanent database deletion, subject to the configured confirmation preference.

Permanent deletion removes the contact row; foreign-key cascades remove owned child/link rows. If confirmation is required but the platform confirmation callback is unavailable, ContactCore blocks deletion rather than bypassing the safeguard.

Create a verified backup before irreversible cleanup when recovery might matter.

## Duplicate review and merge

Choose **Find duplicates** to open the duplicate-review screen. ContactCore scans all contacts, including archived contacts, and scores likely pairs using normalized name, phone, email, and birthday signals.

For each candidate you can review:

- both contact names;
- confidence score;
- matching reasons;
- side-by-side contact summaries;
- the documented merge behavior.

You then choose either:

- **Keep first record…**; or
- **Keep second record…**.

ContactCore always asks for confirmation before performing this destructive merge.

The chosen survivor keeps its identity. The application merge engine fills missing scalar identity fields from the other record where appropriate, combines distinct notes, preserves favorite state when either record is favorite, and adds unique phones, emails, addresses, organizations, groups, and tags.

The repository updates the survivor and deletes the secondary row in **one SQLite transaction**. If the secondary contact disappeared before commit, the operation rolls back instead of leaving only half the merge committed.

There is no general-purpose undo stack. Verified backups remain the recovery mechanism for destructive cleanup.

## CSV import/export

CSV export writes:

`GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes`

Only the first email and first phone are represented. This is an interchange format, not a complete backup.

CSV import is header-driven and case-insensitive. Current safeguards include:

- unknown columns are ignored when supported columns exist;
- duplicate header names use the first occurrence and return a warning;
- a file with no recognized ContactCore headers imports zero contacts instead of creating meaningless unnamed contacts;
- invalid birthday text returns a warning;
- formula-like text beginning with `=`, `+`, `-`, or `@` is preserved but produces a spreadsheet-safety warning.

Parsed contacts are normalized and validated as a complete batch before persistence. The SQLite repository writes the whole accepted batch in one transaction.

The desktop importer reads UTF-8 text with BOM detection and enforces a maximum of **5,000,000 characters**. Oversized files are rejected with a controlled error.

ContactCore does not currently neutralize spreadsheet formulas by altering the stored text, so use care when opening CSV containing untrusted contact values in spreadsheet software.

See [Import and export](import-export.md) for exact format behavior.

## vCard import/export

The focused vCard 4.0 codec exports `N`, `FN`, repeated `TEL`, repeated `EMAIL`, optional `BDAY`, and optional `NOTE` using CRLF line endings.

Import supports those focused properties, line unfolding, supported escaping, escaped delimiters in structured names, common `TYPE` mappings (`home`, `work`, `cell`/`mobile`, `other`), and generic warnings for malformed birthday/unterminated-card cases.

The codec does not claim complete vCard ecosystem compatibility. It does not round-trip every address, organization, media field, custom extension, ContactCore group/tag, or ContactCore identity field.

## Export behavior

The Data Tools surface exports CSV or vCard only after you choose a destination. Archived contacts are included. Text is written UTF-8 without a BOM.

Exports are interoperability copies. Use the SQLite backup workflow for full ContactCore database recovery.

## Backup

Creating a backup uses SQLite's backup API rather than copying a potentially active WAL-mode database file. The result is checked with `PRAGMA integrity_check` and ContactCore schema-identity validation before success is reported.

Backup filenames include a timestamp and random identifier to avoid collisions.

## Restore and recovery

Restore is deliberately conservative:

1. the selected file must exist and cannot be the active database itself;
2. it is opened read-only and checked for SQLite integrity and ContactCore identity before the active database is touched;
3. if an active database exists, ContactCore creates a verified pre-restore recovery snapshot;
4. the selected backup is copied to staging;
5. supported migrations are applied to the staging copy;
6. the staged database is integrity/identity checked again;
7. SQLite pools and sidecars are cleared, then staging replaces the active database;
8. the new active database is verified;
9. if final verification fails, the failed restored file is retained for diagnosis and the verified pre-restore snapshot is copied back.

Restore always requires the desktop confirmation callback. If the selected storage item does not expose a local path, Desktop makes a temporary local picker copy and deletes that copy after the restore attempt.

See [Storage, backup, and recovery](storage-backup-recovery.md) for failure-path detail.

## Settings

Local preferences include:

- Theme: System, Light, or Dark;
- Reduced motion preference;
- Confirm before permanent deletion.

The database key is runtime-only and is not serialized into `settings.json`.

Preference writes use a temporary file followed by replacement to reduce the chance of leaving partially written JSON.

The reduced-motion preference is available for UI behavior, but it is not a claim that every Avalonia/framework animation has been disabled.

## Data location

By default ContactCore uses a `ContactCore` directory under the operating system's local application-data location. If that API returns no path, the application falls back to its base directory.

`CONTACTCORE_DATA_PATH` can override the **directory**. The application uses:

```text
contactcore.db
settings.json
backups/
```

inside that directory.

## Encryption behavior

The default open-source build uses ordinary SQLite. `CONTACTCORE_DATABASE_KEY` requests keyed behavior. ContactCore applies the runtime key and then checks `PRAGMA cipher_version`; when compatible cipher support cannot be verified, the connection is closed and an error is reported.

Do not put real keys in `.env.example`, source files, issues, screenshots, logs, or committed configuration.

## Privacy habits

- Keep real exports/databases/backups out of Git.
- Use fictional data in screenshots and bug reports.
- Remove sensitive values from diagnostics before posting them publicly.
- Store backups in a location appropriate for the sensitivity of their contents.
- Remember that a normal backup has the same confidentiality requirements as the live database unless the underlying provider encrypts it.

## Keyboard and accessibility

Current shortcuts:

- `Ctrl+N` — new contact;
- `Ctrl+S` — save only when the contact editor is active;
- `Ctrl+F` — focus search;
- `Esc` — close/cancel the active editor/settings/data-tools/duplicate-review surface.

The desktop UI uses labeled fields, keyboard navigation, visible focus styling, theme-aware resources, and a reduced-motion preference. Accessibility still requires manual validation on supported operating systems and representative assistive technology before any conformance claim is made.
