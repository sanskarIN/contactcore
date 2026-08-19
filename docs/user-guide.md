# User Guide

ContactCore is an offline-first desktop contact manager. It does not require an account, mandatory cloud synchronization, telemetry, or advertising services. Contact data is stored in the local ContactCore data directory unless you explicitly export or back it up elsewhere.

Current release-preparation version: **2.0.12**.

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

Repeated values appear as independent add/edit/remove rows. Removing one row does not intentionally remove unrelated values.

### Identity behavior

Phones, emails, addresses, and organizations are contact-owned rows. Their IDs are preserved through ordinary edits while the row remains.

Groups and tags are different: they are shared case-insensitive dictionary entries linked to contacts.

- An unchanged group/tag assignment retains its shared dictionary identity.
- A case-only or normalization-equivalent edit keeps the existing identity and canonical stored name.
- A true per-contact group/tag rename becomes reassignment to a new dictionary identity instead of reusing the old global ID with a different name.
- Names containing commas or semicolons remain exact because each group/tag is its own row.
- Duplicate names on the same contact collapse case-insensitively during draft conversion.

This keeps an ordinary contact edit from accidentally becoming a global taxonomy rename. A separate global group/tag management screen is not currently implemented.

A contact whose name fields are empty is displayed as `Unnamed contact`, subject to validation rules for data being saved/imported.

## Create a contact

Choose **New contact** or press `Ctrl+N`.

The new draft has an application-generated identity but is explicitly marked **not yet persisted**. Choosing **Delete / discard** on an unsaved draft simply discards it and never performs a database delete.

Add any repeated rows you need and choose **Save**. Birthday, when present, must use `yyyy-MM-dd`.

Blank newly added phone/email/address/organization/group/tag rows are ignored according to the row's minimum meaningful-value semantics.

## Edit a contact

Select a contact in the middle pane. The right pane loads the complete editable aggregate.

You can edit scalar fields; add/remove/edit multiple phones/emails; add/remove/edit addresses and organizations; add/remove/edit groups/tags; change Favorite/Archived state; and edit notes.

Saving preserves the contact ID and creation timestamp. Contact-owned repeated IDs are retained while those rows remain. Shared group/tag identity follows the assignment rules above.

Repeated rows support add/edit/remove; drag-to-reorder is not implemented.

## Saving and validation

ContactCore trims/normalizes supported text fields in the application service, refreshes `UpdatedAt`, validates the contact, and persists the complete aggregate transactionally.

Validation includes practical name/note length constraints, basic email validation, and a permissive phone-character/length pattern. Error messages identify affected fields without intentionally echoing invalid private values.

`Ctrl+S` saves only while the contact editor is visible. It does not invoke contact save from Settings, Data Tools, or Duplicate Review.

## Search and filters

Search matches given name, family name, nickname, phone number, and email address. The repository also supports favorites-only filtering, archived inclusion, exact case-insensitive tag/group filtering, and alphabetical `StartsWith` filtering.

The sidebar exposes All, Favorites, Archived, and A–Z filtering. Search input is trimmed. SQL wildcard characters `%`, `_`, and backslash are escaped so they behave as literal search text.

Search is debounced for 180 ms and older pending searches are cancelled when newer input arrives. `Ctrl+F` focuses search.

## Favorites and archive

The editor's **Favorite** checkbox changes stored favorite state. The Favorites view filters by it.

Archive is reversible. Archived contacts remain in the database but are excluded from normal views unless archived records are requested. Use the **Archived** checkbox to change this state.

## Permanent delete

**Delete / discard** behaves differently for persisted and unsaved contacts:

- **unsaved draft** — discarded locally; no database delete and no permanent-delete confirmation;
- **persisted contact** — permanent database deletion, subject to the configured confirmation preference.

Permanent deletion removes the contact row and foreign-key cascades remove owned child/link rows. If confirmation is required but the platform confirmation callback is unavailable, ContactCore blocks deletion.

Create a verified backup before irreversible cleanup when recovery might matter.

## Duplicate review and merge

Choose **Find duplicates** to open Duplicate Review. ContactCore scans all contacts, including archived contacts, and scores likely pairs using normalized name, phone, email, and birthday signals.

For each candidate you can review both names, confidence, matching reasons, side-by-side summaries, and the merge behavior. Then choose either **Keep first record…** or **Keep second record…**.

ContactCore always asks for confirmation before the destructive merge.

The application reloads both contacts, builds/normalizes/validates the merged survivor, and then the repository requires **both reviewed records to still exist inside the same SQLite merge transaction**.

If either contact disappeared after review:

- a missing secondary cancels the merge rather than committing only the survivor update;
- a missing chosen primary is not recreated from stale reviewed UI state.

When both still exist, the chosen survivor keeps its root identity, missing scalar fields may be filled from the other record, distinct notes are combined, favorite state is preserved if either contact is favorite, and unique phones/emails/addresses/organizations/groups/tags are combined according to merge rules. The secondary row is deleted in that same transaction.

There is no general-purpose undo stack. Verified backups remain the recovery mechanism for an incorrectly confirmed merge.

## CSV import/export

CSV export writes:

`GivenName,FamilyName,Nickname,Email,Phone,Birthday,Notes`

Only the first email and first phone are represented. CSV is an interchange format, not a complete backup.

CSV import is header-driven and case-insensitive. Current safeguards include:

- unknown columns are ignored when supported columns exist;
- duplicate header names use the first occurrence and return a warning;
- a file with no recognized ContactCore headers imports zero contacts instead of creating meaningless unnamed records;
- invalid birthday text returns a warning;
- formula-like text beginning with `=`, `+`, `-`, or `@` is preserved but produces a spreadsheet-safety warning.

Parsed contacts are normalized and validated as a complete batch before persistence. The repository writes the accepted batch in one transaction.

The desktop importer uses UTF-8 with BOM detection and enforces a maximum of **5,000,000 characters**. Oversized files are rejected.

ContactCore does not neutralize spreadsheet formulas by altering contact data, so use care when opening CSV containing untrusted values in spreadsheet software.

See [Import and export](import-export.md).

## vCard import/export

The focused vCard 4.0 codec exports `N`, `FN`, repeated `TEL`, repeated `EMAIL`, optional `BDAY`, and optional `NOTE` using CRLF line endings.

Import supports those focused properties, line unfolding, supported escaping, escaped structured-name delimiters, common `TYPE` mappings (`home`, `work`, `cell`/`mobile`, `other`), and generic malformed-card/birthday warnings.

It does not claim complete vCard ecosystem compatibility or full round-trip fidelity for every ContactCore/external property.

## Export behavior

The Data Tools surface exports CSV or vCard only after you choose a destination. Archived contacts are included. Text is written UTF-8 without a BOM.

Use verified SQLite backup for full ContactCore recovery.

## Backup

Backup uses SQLite's backup API rather than a raw copy of a potentially active WAL-mode database. The result is checked with `PRAGMA integrity_check` and ContactCore schema-identity validation before success is reported.

Backup filenames include a timestamp and random identifier to avoid collisions.

## Restore and recovery

Restore is deliberately conservative:

1. selected file must exist and cannot be the active database itself;
2. source is opened read-only and checked for SQLite integrity and ContactCore identity before active data changes;
3. ContactCore creates a verified pre-restore recovery snapshot when active data exists;
4. selected backup is copied to staging;
5. supported migrations run on staging;
6. staged database is verified again;
7. SQLite pools/sidecars are cleared and staging replaces the active database;
8. the new active database is verified;
9. on final verification failure, the failed restored file is retained and the verified recovery snapshot is copied back when possible.

Restore always requires desktop confirmation. A stream-backed picker item may be copied to a temporary local file, which Desktop attempts to delete after restore.

See [Storage, backup, and recovery](storage-backup-recovery.md).

## Settings

Local preferences include System/Light/Dark theme, Reduced motion, and Confirm before permanent deletion.

The database key is runtime-only and is not serialized into `settings.json`. Preference writes use a temporary file followed by replacement.

Reduced motion is a preference contract for custom UI behavior; it is not a claim that every framework/OS animation is disabled.

## Data location

By default ContactCore uses a `ContactCore` directory under the operating system's local application-data location, with fallback to the application base directory if needed.

`CONTACTCORE_DATA_PATH` overrides the **directory**. ContactCore derives:

```text
contactcore.db
settings.json
backups/
```

## Encryption behavior

The default build uses ordinary SQLite. `CONTACTCORE_DATABASE_KEY` requests keyed behavior. ContactCore applies the runtime key and checks `PRAGMA cipher_version`; when compatible cipher support cannot be verified, the connection is closed and an error is reported.

Do not put real keys in `.env.example`, source files, issues, screenshots, logs, or committed configuration.

## Privacy habits

- Keep real exports/databases/backups out of Git.
- Use fictional data in screenshots and bug reports.
- Remove sensitive values from diagnostics before posting them publicly.
- Store backups appropriately for the sensitivity of their contents.
- Remember that normal backups have the same confidentiality requirements as live data unless the underlying provider encrypts them.

## Keyboard and accessibility

Current shortcuts:

- `Ctrl+N` — new contact;
- `Ctrl+S` — save only when contact editor is active;
- `Ctrl+F` — focus search;
- `Esc` — close/cancel the active editor/settings/data-tools/duplicate-review surface.

The UI uses labeled fields, keyboard navigation, visible focus styling, theme-aware resources, and a reduced-motion preference. Accessibility still requires manual validation on supported operating systems and representative assistive technology before any conformance claim is made.
