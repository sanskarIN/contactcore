# Desktop UI

The desktop application is an Avalonia presentation layer over the Domain, Application, and Infrastructure projects. `App.axaml.cs` is the composition root; `MainWindow.axaml` defines the primary visual tree; `MainWindowViewModel` coordinates user workflows; `MainWindow.axaml.cs` supplies platform file pickers, confirmation dialogs, and keyboard shortcuts.

## Composition root

At desktop startup `App.axaml.cs`:

1. creates `AppPaths`, honoring `CONTACTCORE_DATA_PATH`;
2. creates `JsonAppPreferences` and reads the stored theme/safety preferences;
3. applies the requested Avalonia theme variant;
4. creates `SqliteConnectionFactory`, using a runtime key provider;
5. creates `DatabaseMigrator` and `SqliteContactRepository`;
6. creates `ContactService` and `BackupService`;
7. creates `MainWindowViewModel` and wires runtime theme changes;
8. assigns the view model to `MainWindow`;
9. starts view-model initialization.

The desktop project therefore owns dependency construction while business/storage behavior remains in lower layers.

## Main window layout

`MainWindow.axaml` uses a three-column desktop layout below a top bar:

- **left sidebar** — All contacts, Favorites, Archived, alphabet filter, Import/Export, Find duplicates;
- **middle list pane** — filtered contact results with avatar initials, display name, first email/phone subtitle, and favorite marker;
- **right detail pane** — contact editor, Settings, or Data tools, depending on current state.

The footer reports application status and reinforces local/offline behavior.

The window has a default size of 1180×760 and a minimum size of 850×560. Responsiveness below that minimum is therefore not claimed.

## Search

The top search box binds two-way to `SearchText`. View-model changes trigger a 180 ms debounced refresh. Each new search replaces the prior `CancellationTokenSource`, cancels/disposes the older operation, and prevents a stale debounce from continuing.

Search covers names, phones, and email addresses through the repository query. `Ctrl+F` focuses the search box.

## Browse modes

The sidebar exposes:

- All contacts;
- Favorites;
- Archived;
- A–Z first-letter filtering.

The Archived mode asks the repository to include archived records, then keeps only archived contacts for the displayed list. Normal/favorites views exclude archived records through the repository query.

## Contact editor

The **current desktop draft editor is intentionally narrower than the full domain model**. It exposes:

- given name;
- family name;
- nickname;
- birthday text in `yyyy-MM-dd` format;
- one phone field;
- one email field;
- notes;
- Favorite flag;
- Archived flag.

The Domain and SQLite layers support multiple phone numbers, emails, addresses, organizations, groups, and tags, but this specific editor does not yet expose full multi-value editing for those collections. When an existing rich contact is loaded into `ContactDraftViewModel`, the draft reads only the first phone and first email; `ToContact()` currently constructs at most one phone and one email and does not reconstruct addresses, organizations, groups, or tags. Contributors must account for this behavior when expanding the editor because saving such a draft uses the repository's complete-aggregate replacement strategy.

This is an important current UI limitation and should not be described as full rich-field editing until the editor is expanded and regression-tested.

## Saving

`SaveCommand` converts the draft to a `Contact`, parses birthday exactly, passes it to `ContactService.SaveAsync`, and refreshes the list on success. Errors are converted to a sanitized status message through `RedactingLog`.

For a new draft, a new contact ID and creation timestamp are generated. Existing draft IDs and creation timestamps are preserved. Archived state is also preserved by the draft model.

## Permanent deletion

`RequestDeleteCommand` handles deletion rather than binding directly to the repository. The default preference requires confirmation. If confirmation is enabled but the platform confirmation callback is unavailable, deletion is blocked.

The confirmation message states that the operation removes the contact from the active database and that backups/exports are separate copies. If confirmed, the application deletes the contact, closes the editor selection, reports success, and refreshes the list.

For an unsaved draft (`Guid.Empty`), the action simply cancels editing.

## Duplicate command

The current `FindDuplicatesCommand` loads all contacts including archived records, runs `DuplicateDetector.Find`, and reports either no likely duplicates or the number of likely pairs plus the highest score.

The present main-window UI does **not** yet expose a full duplicate-pair review/merge screen. The application layer already contains deterministic comparison and merge logic, but a complete interactive merge workflow remains a UI roadmap item.

## Data tools

The Data tools surface provides:

- import CSV/vCard;
- export CSV;
- export vCard;
- create verified backup;
- restore backup.

Exports include archived contacts because `ExportTextAsync` queries with `IncludeArchived: true`.

## Import file picker and size limit

`MainWindow.axaml.cs` opens one `.csv`, `.vcf`, or `.vcard` file. Text is decoded with `StreamReader` using UTF-8 with BOM detection. The reader enforces a maximum of **5,000,000 characters**. Files exceeding this limit raise a controlled `InvalidDataException` instead of being read without bound.

The extension determines vCard decoding for `.vcf`/`.vcard`; otherwise the selected text is decoded as CSV.

## Export file picker

Export prompts for a destination, suggests `contactcore-contacts.csv` or `contactcore-contacts.vcf`, truncates an existing destination stream when seekable, and writes UTF-8 **without a BOM**.

## Backup picker portability

Restore accepts one `.db` file. When Avalonia provides a normal local path, that path is passed to `BackupService`. When the storage provider exposes only a stream-backed item, the desktop layer copies it to a unique temporary file under the system temporary directory and sets `DeleteAfterUse = true` so the temporary picker copy is removed after the restore attempt.

## Restore confirmation

Restore is blocked if a confirmation callback is unavailable. The user is told that ContactCore will retain a verified pre-restore recovery snapshot before replacement. After restore succeeds, the view model reinitializes the repository, clears the selection/editor, refreshes contacts, and reports success.

## Settings

The Settings surface includes:

- System / Light / Dark theme selector;
- Reduced motion preference;
- Confirm-before-permanent-delete preference;
- local data directory display;
- privacy/storage reminder;
- project/license/author/support information.

`SaveSettingsCommand` normalizes the theme, updates preferences, writes the local JSON settings file, requests a runtime theme change, closes Settings, and reports that settings were saved locally.

The reduced-motion preference is persisted and surfaced, but the current UI has very little custom animation. Future animated interactions should consult the preference rather than treating its presence as proof that all framework-level motion is disabled.

## Keyboard shortcuts

`MainWindow.axaml.cs` currently handles:

| Shortcut | Action |
|---|---|
| `Ctrl+N` | New contact |
| `Ctrl+S` | Save when the save command can execute |
| `Ctrl+F` | Focus search |
| `Esc` | Close/cancel the active editor/settings/data-tools surface |

Shortcut handling marks handled key events to prevent duplicate processing by child controls.

## Confirmation dialog

`ConfirmDialog.axaml` is a small owner-centered modal confirmation window. It returns a nullable Boolean result to the caller. Destructive and restore operations interpret only `true` as confirmation.

## Styling and focus

`Styles/DesignSystem.axaml` centralizes major layout styles for the top bar, sidebar, list/detail panes, status bar, logo/avatar, settings cards, text labels, primary buttons, and alphabet buttons.

A focus style applies a visible accent-colored, two-pixel border to buttons, text boxes, combo boxes, check boxes, toggle buttons, and list boxes. Theme-facing colors use Avalonia dynamic resources instead of hard-coding a light-only palette.

## View-model callbacks

The view model deliberately exposes delegates for platform services:

- `FocusSearchRequested`;
- `ThemeChangeRequested`;
- `PickImportTextRequested`;
- `SaveTextRequested`;
- `PickBackupFileRequested`;
- `ConfirmActionRequested`.

`MainWindow` wires them when its `DataContext` changes and unwires them on replacement/close. This keeps direct Avalonia storage/dialog APIs out of most view-model code and makes non-visual view-model tests possible.

## Error presentation

Initialization, save, import/export, backup/restore, and delete flows catch failures at the desktop boundary and pass error text through `RedactingLog.Sanitize` before exposing it as status text. This is a defense-in-depth diagnostic measure, not permission to place sensitive values in exception messages elsewhere.

## UI test priorities

The desktop test project currently covers draft-model behavior. Additional high-value tests include:

- preservation of every rich child collection once full multi-value editing is added;
- debounced-search cancellation races;
- settings persistence/view-model propagation;
- confirmation-required destructive actions;
- import/export callback cancellation;
- restore temporary-file cleanup;
- command state during overlapping operations;
- keyboard/focus behavior via Avalonia integration testing where practical.

Manual platform verification remains necessary for focus visuals, screen readers, high-DPI scaling, native pickers, theme integration, and window behavior.
