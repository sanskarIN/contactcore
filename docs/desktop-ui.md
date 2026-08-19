# Desktop UI

The desktop application is an Avalonia presentation layer over the Domain, Application, and Infrastructure projects. `App.axaml.cs` is the composition root; `MainWindow.axaml` defines the primary visual tree; `MainWindowViewModel` coordinates user workflows; `MainWindow.axaml.cs` supplies platform file pickers, confirmation dialogs, runtime shortcuts, and platform-facing adapters. Rich-field row models live in `RichFieldViewModels.cs`; the reverse-direction duplicate merge command lives in `DuplicateCommands.cs`; destructive delete/restore commands live in `DataSafetyCommands.cs`.

## Composition root

At desktop startup `App.axaml.cs`:

1. creates `AppPaths`, honoring `CONTACTCORE_DATA_PATH`;
2. creates `JsonAppPreferences` and reads the stored theme/safety preferences plus the runtime-only database key environment value;
3. applies the requested Avalonia theme variant;
4. creates `SqliteConnectionFactory`, using the runtime key provider;
5. creates `DatabaseMigrator` and `SqliteContactRepository`;
6. creates `ContactService` and `BackupService`;
7. creates `MainWindowViewModel` and wires runtime theme changes;
8. assigns the view model to `MainWindow`;
9. starts view-model initialization.

The desktop project therefore owns dependency construction while business/storage behavior remains in lower layers.

## Main window layout

`MainWindow.axaml` uses a three-column desktop layout below a top bar:

- **left sidebar** — All contacts, Favorites, Archived, A–Z filter, Import/Export, Find duplicates;
- **middle list pane** — filtered contact results with avatar initials, display name, first email/phone subtitle, favorite marker, and archived marker when applicable;
- **right detail pane** — contact editor, duplicate review, Settings, or Data tools, depending on current state.

The footer reports application state and reinforces local/offline behavior.

The window currently defaults to **1220×800** with a minimum of **900×600**. Layout behavior below that minimum is not claimed.

## Search

The top search box binds two-way to `SearchText`. View-model changes trigger a 180 ms debounced refresh. Each new search replaces the previous `CancellationTokenSource`, cancels/disposes the older pending operation, and prevents a stale debounce from continuing.

Search covers names, phones, and email addresses through the repository query. SQL `LIKE` wildcard characters entered by a user are escaped before the repository constructs the pattern. `Ctrl+F` focuses the search box.

## Browse modes

The sidebar exposes:

- All contacts;
- Favorites;
- Archived;
- A–Z first-letter filtering.

Normal/favorites views exclude archived records. Archived mode requests archived records and then keeps only archived contacts for display. The repository also supports tag/group query filters even though those filters are not yet exposed as dedicated sidebar controls.

## Complete contact editor

The current editor exposes the complete persisted aggregate used by the present ContactCore data model:

- given name;
- family name;
- nickname;
- birthday text in `yyyy-MM-dd` format;
- Favorite flag;
- Archived flag;
- multiple phone-number rows with label, value, and `ContactFieldKind`;
- multiple email-address rows with label, value, and `ContactFieldKind`;
- multiple postal-address rows with label, street, city, region/state, postal code, and country;
- multiple organization rows with organization name, title, and department;
- multiple group rows;
- multiple tag rows;
- notes.

Each repeated row has explicit add/remove UI. Reordering is not currently exposed.

### Identity preservation

`ContactDraftViewModel.Load` projects every child collection into dedicated draft rows while preserving the child record IDs. `ToContact()` reconstructs the aggregate with the original contact ID and creation timestamp and retains existing child IDs for rows that remain.

This matters because `SqliteContactRepository` treats the supplied contact as the complete desired aggregate and replaces contact-owned child/link rows during save. The editor therefore must not silently recreate unchanged children or omit collections it cannot represent.

Current behavior:

- existing phone/email/address/organization/group/tag IDs are preserved when their row remains;
- new rows receive generated IDs;
- removed rows are absent from the resulting aggregate and are therefore removed by the aggregate save;
- blank phone/email rows are ignored unless they contain a value;
- a blank new address row is ignored;
- an existing address containing only a label is still preserved;
- an organization row is included when it has a nonblank organization name;
- blank group/tag rows are ignored;
- duplicate group/tag names are collapsed case-insensitively while preserving the first row identity;
- group/tag names containing commas or semicolons remain exact because they are independent rows rather than delimiter-separated text.

Desktop regression tests cover these invariants.

## Draft lifetime and unsaved contacts

`ContactDraftViewModel.IsPersisted` distinguishes a new draft from a database-backed contact even though both already have a generated GUID.

- **New contact** loads a new aggregate with `isPersisted: false`.
- After a successful save, the draft is reloaded with `isPersisted: true`.
- Choosing **Delete / discard** on an unsaved draft simply closes/discards it and reports `Unsaved contact discarded.`
- No database delete and no permanent-delete confirmation are invoked for an unsaved draft.

This avoids treating generated identity as proof that a row already exists in SQLite.

## Saving

`SaveCommand` first requires the contact editor to be visible. It converts the draft to a `Contact`, parses birthday exactly, passes the aggregate to `ContactService.SaveAsync`, refreshes the list, restores the matching selection when it remains in the active filter, and reports success.

`ContactService.SaveAsync` normalizes supported text fields, refreshes `UpdatedAt`, validates the contact, and then persists the complete aggregate transactionally.

Errors are sanitized through `RedactingLog.Sanitize` before being presented as status text.

## Permanent deletion

`RequestDeleteCommand` is the desktop destructive-delete boundary.

For persisted contacts:

- the default preference requires confirmation;
- if confirmation is required but the platform confirmation callback is unavailable, deletion is blocked;
- the message explains that the active database record is removed while backups/exports remain separate copies;
- after a confirmed delete, the selection/editor closes and the list refreshes.

For unsaved drafts, the command discards the draft without touching SQLite.

## Duplicate review and merge

**Find duplicates** now opens an interactive duplicate-review surface rather than only reporting a count.

The workflow:

1. load all contacts, including archived records;
2. run `DuplicateDetector.Find`;
3. show likely pairs with confidence and matching reasons;
4. show side-by-side summaries of each selected record;
5. show a merge-behavior explanation;
6. let the user choose **Keep first record…** or **Keep second record…**;
7. require confirmation for the destructive merge;
8. ask `ContactService.MergeAsync` to build/normalize/validate the merged aggregate;
9. persist the surviving aggregate and delete the secondary contact in **one SQLite transaction**;
10. refresh the contact list and duplicate candidates.

The operation is intentionally atomic. `SqliteContactRepository.MergeAsync` rolls the transaction back when the secondary row no longer exists instead of committing only the survivor update.

The merge engine keeps the chosen survivor identity, fills missing scalar identity fields from the other contact, combines distinct notes, preserves favorite state if either side is favorite, and adds unique phones, emails, addresses, organizations, groups, and tags. Copied child rows receive new IDs where needed.

There is no general-purpose undo stack; users should use verified backups for recovery needs.

## Data tools

The Data tools surface provides:

- import CSV/vCard;
- export CSV;
- export vCard;
- create verified SQLite backup;
- restore verified backup.

Exports include archived contacts because export queries use `IncludeArchived: true`.

## Import picker and size limit

`MainWindow.axaml.cs` opens one `.csv`, `.vcf`, or `.vcard` file. Text is decoded with `StreamReader` using UTF-8 with BOM detection. The reader enforces a maximum of **5,000,000 characters**. Oversized files raise a controlled `InvalidDataException` instead of being read without bound.

The extension selects vCard parsing for `.vcf`/`.vcard`; other supported picker results use CSV parsing.

## Export picker

Export prompts for a destination, suggests `contactcore-contacts.csv` or `contactcore-contacts.vcf`, truncates an existing destination stream when seekable, and writes UTF-8 without a BOM.

The UI explicitly warns that CSV is an interchange format and that formula-like contact text is preserved rather than neutralized for spreadsheet software.

## Backup picker portability

Restore accepts one `.db` file. When Avalonia provides a normal local path, that path is passed to `BackupService`. When the provider exposes only a stream-backed item, Desktop copies it to a unique temporary file under the system temporary directory and sets `DeleteAfterUse = true`; `DataSafetyCommands` then removes that picker copy after the restore attempt.

## Restore confirmation

Restore is blocked if a confirmation callback is unavailable. The user is told that ContactCore will retain a verified pre-restore recovery snapshot before replacement. After restore succeeds, the view model reinitializes the repository, clears active detail views/selection, refreshes contacts, and reports success.

The underlying backup service validates identity/integrity before replacement and stages/migrates/verifies the candidate before switching the active file.

## Settings

The Settings surface includes:

- System / Light / Dark theme selector;
- Reduced motion preference;
- Confirm-before-permanent-delete preference;
- local data directory display;
- privacy/storage reminder;
- project/license/author/support information.

`SaveSettingsCommand` normalizes the theme, updates local preferences, writes `settings.json`, requests the runtime theme change, closes Settings, and reports success.

The runtime database key is not a Settings text field and is not serialized into normal preferences.

The reduced-motion preference is persisted and surfaced, but the current UI has little custom animation. Its presence is not proof that every framework-level animation is disabled.

## Keyboard shortcuts

`MainWindow.axaml.cs` handles:

| Shortcut | Action |
|---|---|
| `Ctrl+N` | Start a new contact draft |
| `Ctrl+S` | Save **only when the contact editor is visible** |
| `Ctrl+F` | Focus search |
| `Esc` | Close/cancel the active editor/settings/data-tools/duplicate surface |

The explicit editor check on `Ctrl+S` prevents Settings/Data Tools/Duplicate Review from accidentally invoking a stale contact-save command.

## Confirmation dialog

`ConfirmDialog.axaml` is an owner-centered modal confirmation window. It returns a nullable Boolean result; destructive operations interpret only `true` as approval.

## Styling and focus

`Styles/DesignSystem.axaml` centralizes layout and visual styles for the top bar, sidebar, list/detail panes, status bar, logo/avatar, settings cards, labels, primary buttons, and alphabet controls.

A focus style applies a visible accent-colored two-pixel border to buttons, text boxes, combo boxes, check boxes, toggle buttons, and list boxes. Theme-facing colors use Avalonia dynamic resources rather than a light-only palette.

## View-model platform callbacks

The main view model exposes delegates for platform services:

- `FocusSearchRequested`;
- `ThemeChangeRequested`;
- `PickImportTextRequested`;
- `SaveTextRequested`;
- `PickBackupFileRequested`;
- `ConfirmActionRequested`.

`MainWindow` wires them when its `DataContext` changes and unwires them on replacement/close. This keeps direct Avalonia picker/dialog APIs outside the bulk of view-model logic and makes non-visual view-model testing practical.

## Error presentation

Initialization, contact save, duplicate merge, import/export, backup/restore, and persisted delete flows catch failures at the desktop boundary and sanitize exception text before exposing it as status text. This is defense-in-depth; lower layers must still avoid placing raw secrets/contact values in exception messages.

## Desktop test coverage

The desktop test project covers, among other cases:

- contact ID/creation timestamp/Favorite/Archived preservation;
- explicit persisted versus unsaved draft state;
- exact ISO birthday validation;
- rich phone/email/address/organization/group/tag editing while preserving child IDs;
- removal of selected repeated rows without deleting unrelated rows;
- exact group/tag names containing commas and semicolons;
- preservation of a legacy label-only address;
- suppression of blank newly added rich rows.

Higher-level destructive/storage behavior is covered in Application/Infrastructure tests, including atomic merge rollback when the secondary record is missing.

## Manual verification still required

Automated tests do not replace release testing for:

- screen-reader announcements and accessible names;
- focus order/visuals across all supported desktop platforms;
- high-DPI and text scaling;
- native file pickers;
- System theme integration;
- keyboard behavior inside platform-specific controls;
- small-window usability at the documented minimum;
- release packaging/signing/notarization behavior.

Do not make accessibility-conformance or platform-certification claims until those checks are performed and recorded.
