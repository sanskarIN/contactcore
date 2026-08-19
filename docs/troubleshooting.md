# Troubleshooting

This guide starts with low-risk checks. Do not delete contact data as a first response. If the database contains important contacts, preserve a verified backup/recovery copy before destructive troubleshooting.

## The project will not restore or build

### Check SDK resolution

From the repository root:

```bash
dotnet --version
dotnet --info
```

The repository requires the stable .NET 10 SDK policy in `global.json` (`10.0.100`, latest compatible feature-band roll-forward, prereleases disabled).

If no compatible SDK is resolved, install a suitable stable .NET 10 SDK.

### Restore packages explicitly

```bash
dotnet restore ContactCore.slnx
```

If restore fails, inspect the first NuGet error, package-source/network configuration, and centralized versions in `Directory.Packages.props`.

### Clean generated output only

Close the IDE/application, remove project `bin`/`obj` directories if needed, then restore/build again. **Do not delete the ContactCore data directory while cleaning build output.**

## Build fails because of warnings

`TreatWarningsAsErrors=true` is intentional. Fix the underlying warning rather than disabling the repository-wide policy.

```bash
dotnet build ContactCore.slnx -c Release
```

Start with the first actionable diagnostic; later compiler errors may be cascades.

## Format check fails

```bash
dotnet format ContactCore.slnx
```

Review `git diff`, then rerun:

```bash
dotnet format ContactCore.slnx --verify-no-changes
```

## Desktop window does not open

Check:

- `dotnet --info`;
- terminal exception output;
- graphical desktop/session availability on Linux;
- Avalonia/native runtime prerequisites;
- whether `CONTACTCORE_DATABASE_KEY` was set accidentally;
- whether `CONTACTCORE_DATA_PATH` points to a writable directory.

If the error says encryption was requested but no SQLCipher-compatible provider is active, see the next section.

## Encryption requested but startup is blocked

This is expected fail-closed behavior when `CONTACTCORE_DATABASE_KEY` is non-empty but `PRAGMA cipher_version` cannot prove a compatible provider is active.

If encryption was not intended, remove the environment variable from the process/shell and restart.

PowerShell current-process check:

```powershell
$env:CONTACTCORE_DATABASE_KEY
```

Bash/zsh:

```bash
printenv CONTACTCORE_DATABASE_KEY
```

Do not “fix” this by bypassing cipher verification. A real encrypted build requires a maintained SQLCipher-compatible provider and corresponding packaging/integration tests.

## Encryption key appears ignored on first launch

Current code reads the runtime database key before checking whether `settings.json` exists. A first launch with `CONTACTCORE_DATABASE_KEY` should therefore still request keyed behavior and fail closed if the provider is not compatible.

If a build silently ignores the key only on first launch, verify the exact commit/release: that behavior indicates an older build or regression.

## ContactCore is using the wrong data directory

Open Settings and inspect **Local data directory**.

`CONTACTCORE_DATA_PATH` is a directory override. ContactCore derives:

```text
<directory>/contactcore.db
<directory>/settings.json
<directory>/backups/
```

Use distinct clearly named directories when testing multiple profiles.

## Settings are ignored or reset

Malformed/missing `settings.json` intentionally falls back to safe defaults:

- System theme;
- reduced motion off;
- permanent-delete confirmation on.

The database key is never loaded from `settings.json`; it is runtime-only.

If preferences repeatedly fail to save, check data-directory write permissions and external file locking.

## Search does not find what I expect

Free-text search covers:

- given name;
- family name;
- nickname;
- phone number;
- email address.

Normal views hide archived contacts. Use **Archived** for archived-only display.

`%`, `_`, and backslash are escaped for literal SQLite `LIKE` behavior. The repository can filter by exact group/tag names, but dedicated group/tag search controls are not currently exposed in the sidebar.

## Search results lag while typing

Search is intentionally debounced by 180 ms and older pending requests are cancelled as text changes.

Large databases can still become slow because the current repository loads complete matching aggregates and child collections, and duplicate scanning is currently pairwise in memory. See `performance.md` before treating scale work as a UI-only issue.

## A rich field is missing after editing

The current editor directly represents phones, emails, addresses, organizations, groups, and tags. Losing an unchanged repeated field is therefore a **data-integrity bug**, not an intentional compact-editor limitation.

Current invariants include:

- existing repeated row IDs are preserved;
- removing one row should remove only that row;
- group/tag names containing comma/semicolon characters remain exact;
- blank newly added rows do not create meaningless children;
- an existing label-only address remains preservable.

If a current build violates these invariants:

1. stop repeated edits to the affected contact;
2. preserve the active database and verified backups;
3. note the exact commit/release;
4. reproduce with fictional data if possible;
5. keep the original recovery material until the repaired data is verified;
6. report the minimal fictional reproduction without uploading real contact files.

## Group or tag containing commas/semicolons changes unexpectedly

Current builds use independent group/tag rows rather than delimiter-separated text. Names such as `Research, Team; East` should round-trip exactly.

If a build splits such a name, verify that it predates the independent-row editor fix or report a regression with a fictional group/tag name.

## Blank address row is saved unexpectedly

A completely blank newly added address row should be ignored. Existing legacy rows that contain only a label are intentionally preserved.

If a blank new address becomes persisted, record whether any label/value was entered before save and provide a fictional reproduction.

## New unsaved contact asks for permanent deletion

Current builds explicitly track `IsPersisted`. **Delete / discard** on an unsaved new contact should discard the draft without database deletion or permanent-delete confirmation.

If a new draft still produces permanent-delete confirmation, verify the exact build; that indicates an older version or regression.

## `Ctrl+S` saves while Settings/Data Tools/Duplicate Review is open

Current shortcut handling requires `IsEditorVisible` before executing the contact save command. Pressing `Ctrl+S` outside the editor should not create/update a contact.

If this occurs, capture the active surface and exact commit with fictional data; it is a desktop workflow regression.

## Birthday is rejected

The editor requires exact:

```text
yyyy-MM-dd
```

Fictional example:

```text
2000-01-02
```

Inputs such as `02/01/2000` are rejected by the current draft parser.

## CSV import creates no contacts

Check the first row. ContactCore requires at least one recognized header:

- `GivenName`
- `FamilyName`
- `Nickname`
- `Email`
- `Phone`
- `Birthday`
- `Notes`

A CSV with no supported headers intentionally imports **zero** contacts and returns a warning rather than manufacturing unnamed contacts from unrelated data.

## CSV has duplicate headers

Duplicate header names are accepted defensively: the first occurrence is used and a warning is returned. If this is not what you intended, fix the source CSV before importing.

## CSV import fails completely because one contact is invalid

The service validates the whole parsed batch before persistence. One invalid domain value can reject the whole batch. This is intentional all-or-nothing behavior.

Parser warnings such as an invalid birthday may be non-fatal, while domain-invalid email/phone data can fail batch validation.

## CSV warns about spreadsheet formulas

ContactCore preserves formula-like contact text rather than changing the data. A value whose first non-whitespace character is `=`, `+`, `-`, or `@` can trigger a spreadsheet-safety warning.

This warning does **not** mean ContactCore executed a formula. It means downstream spreadsheet software might interpret exported text specially. Treat external/contact-derived CSV as untrusted data when opening it in spreadsheets.

## CSV import file is rejected as too large

Desktop input is bounded to **5,000,000 characters**. Oversized text raises a controlled error. Split/clean the source using a trusted workflow, or use a different import approach designed for large data rather than removing the bound casually.

## vCard import loses unsupported fields

The codec is a focused vCard 4.0 subset centered on `N`, `FN`, `TEL`, `EMAIL`, `BDAY`, and `NOTE` plus common TYPE mapping/unfolding/escaping behavior.

It does not claim full fidelity for addresses, organizations, photos, custom properties, ContactCore groups/tags/IDs, or every vCard encoding/parameter variant.

Use a verified SQLite backup—not vCard—for complete ContactCore recovery.

## vCard birthday warning does not show the invalid value

This is intentional privacy behavior. The parser reports that the birthday could not be parsed without echoing the imported value into UI/log-facing warning text.

## Backup creation fails

Backup creation uses SQLite's backup API and verifies the destination before reporting success. Common causes include:

- destination permissions;
- storage full/unavailable;
- SQLite/provider errors;
- integrity/identity verification failure;
- incompatible keyed-provider behavior.

Do not treat a backup as successful until ContactCore reports verified success.

## Restore says the file is not a ContactCore backup

A restore source must be valid supported ContactCore SQLite data. The service checks SQLite integrity, required schema structures/version, and ContactCore schema-family identity.

A random `.db` may be valid SQLite and still be correctly rejected.

## Restore rejects a newer schema

A database version newer than the running build supports is rejected. Use a build that supports that schema or a backup from a supported version.

Do not manually edit `schema_migrations` on real data to force a downgrade.

## Restore fails before replacement

The service validates/stages/migrates/verifies the source before replacing the active database. A failure during those stages should leave the original active database in place.

Preserve the failing source privately if it is useful for diagnosis; reproduce publicly with fictional data.

## Restore fails after replacement attempt

After switching, `BackupService` verifies the active database again. On final-verification failure it attempts to:

- retain the failed restored database as `backups/failed-restore-*.db`;
- copy the verified `pre-restore-*.db` snapshot back to the active database path.

Do not delete recovery artifacts until you have identified which file contains the desired state.

## Restore temporary file remains

The staging `.restore-*.tmp` file is cleaned in a `finally` block. A stream-backed picker may also create a temporary local copy whose deletion is attempted after restore.

Abrupt process/OS termination can still leave temp files. Treat them as potentially sensitive until inspected privately.

## Permanent delete button does nothing

For a persisted contact, if confirmation is enabled and the confirmation callback is unavailable, deletion is intentionally blocked.

If a dialog appears, only affirmative confirmation proceeds. Cancel/close leaves the contact intact.

For an unsaved contact, Delete / discard should close the draft without a database operation.

## Find duplicates shows no candidates

Duplicate scoring uses specific signals/thresholds; similar-looking contacts may not cross the current threshold. It is safer for this heuristic to miss a possible duplicate than to automatically destroy data.

No candidate is merged automatically.

## I selected the wrong duplicate survivor but have not confirmed

Cancel the confirmation dialog. No merge is persisted until confirmation succeeds.

## I merged the wrong duplicate

A confirmed duplicate merge is destructive and there is no general-purpose undo stack. The repository does make the survivor update and secondary deletion atomic, but atomicity is not undo.

Use a verified backup/recovery point if restoration is appropriate. Keep any current backup and recovery copies until you have verified the restored data.

## Duplicate merge fails because a contact disappeared

The repository requires the secondary contact to still exist. If another operation removed it between review and commit, the merge transaction rolls back instead of committing only the survivor update. Refresh duplicate candidates and review again.

## Duplicate scan is slow

Current candidate generation is pairwise in memory, so cost grows roughly quadratically with contact count. This is a documented scale limitation; see `performance.md`/`ROADMAP.md` for candidate-generation optimization work.

## Theme change is not persisted

Use **Save settings** after selecting System/Light/Dark. Closing/cancelling Settings does not intentionally persist the draft change.

Unknown theme values normalize to System.

## Reduced motion seems to do nothing

The preference is persisted, but ContactCore currently has little bespoke motion. It is a UI contract for present/future animations, not an operating-system-wide animation switch.

## Cannot delete a temporary/test database on Windows

SQLite pools/file handles may keep files open briefly. Ensure ContactCore/tests are closed and connections disposed. Backup tests explicitly clear pools where needed.

Do not apply file-lock workarounds to the live user profile.

## CI passes locally but fails on one platform

Inspect the failing matrix job/artifacts. Common differences include:

- path separators and case sensitivity;
- file locking;
- newline behavior;
- SQLite native behavior;
- Avalonia/native runtime behavior;
- SDK resolution;
- generated MVVM source/XAML behavior.

Treat a single-platform failure as a compatibility signal until understood.

## Safe public diagnostic bundle

Include only:

- ContactCore commit/release;
- OS/version/architecture;
- `dotnet --info` with personal paths redacted if necessary;
- exact commands;
- sanitized error text;
- minimal fictional reproduction;
- whether a custom data path or custom encryption provider is involved.

Do **not** include:

- real `contactcore.db`;
- `-wal`/`-shm` files;
- backups/recovery copies;
- real CSV/vCards;
- database keys/environment dumps;
- screenshots containing real contacts/private paths unless fully sanitized.

## Last-resort disposable-profile reset

Only for a profile you are certain is disposable:

1. close ContactCore;
2. confirm the exact data directory;
3. preserve anything needed;
4. rename/delete the disposable directory;
5. restart to create a fresh profile.

Never use profile deletion as the default fix for an unknown issue involving important contacts.
