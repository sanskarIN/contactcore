# Troubleshooting

This guide starts with low-risk checks and avoids telling users to delete contact data as a first response. If the database contains important contacts, preserve a verified backup/recovery copy before destructive troubleshooting.

## The project will not restore/build

### Check SDK resolution

From the repository directory:

```bash
dotnet --version
dotnet --info
```

The repository requires the stable .NET 10 SDK policy in `global.json` (`10.0.100`, roll forward to latest compatible feature band, prereleases disabled).

If the SDK resolver reports no compatible SDK, install a suitable stable .NET 10 SDK.

### Restore packages explicitly

```bash
dotnet restore ContactCore.slnx
```

If restore fails, inspect the first NuGet error, network/package source configuration, and central versions in `Directory.Packages.props`.

### Clean generated build output

For a source checkout with no uncommitted generated files you need, close the IDE/app and remove project `bin`/`obj` directories, then restore/build again. Do not delete the ContactCore data directory while cleaning build output.

## Build fails because of warnings

The repository has `TreatWarningsAsErrors=true`. Fix the underlying warning rather than disabling the policy globally.

Run:

```bash
dotnet build ContactCore.slnx -c Release
```

Read the first actionable compiler/analyzer diagnostic; later errors may be cascades.

## Format check fails

To apply repository formatting:

```bash
dotnet format ContactCore.slnx
```

Then inspect `git diff` and rerun:

```bash
dotnet format ContactCore.slnx --verify-no-changes
```

## Desktop window does not open

Check:

- `dotnet --info`;
- terminal exception output;
- graphical desktop/session availability on Linux;
- Avalonia/native runtime prerequisites for the OS;
- whether `CONTACTCORE_DATABASE_KEY` was set accidentally;
- whether `CONTACTCORE_DATA_PATH` points to a writable directory.

If the status/error indicates encryption was requested but no SQLCipher-compatible provider is active, see the encryption section below.

## Database encryption requested but startup is blocked

This is expected fail-closed behavior when `CONTACTCORE_DATABASE_KEY` is non-empty but the loaded SQLite implementation does not report `PRAGMA cipher_version`.

If you did **not** intend to use encrypted SQLite, remove the environment variable from the process/shell and restart.

PowerShell current-process check:

```powershell
$env:CONTACTCORE_DATABASE_KEY
```

Bash/zsh:

```bash
printenv CONTACTCORE_DATABASE_KEY
```

Do not solve this by changing code to ignore the failed cipher check. For actual encryption integration, use a maintained SQLCipher-compatible provider and follow `security.md`.

## ContactCore is using the wrong data directory

Open Settings and inspect **Local data directory**.

Also inspect `CONTACTCORE_DATA_PATH`. It is interpreted as a directory. ContactCore creates/uses:

```text
<directory>/contactcore.db
<directory>/settings.json
<directory>/backups/
```

If testing multiple profiles, use distinct directories and label them clearly.

## Settings are ignored/reset

`settings.json` may be missing or malformed. Malformed JSON intentionally falls back to safe defaults rather than crashing:

- System theme;
- reduced motion off;
- permanent-delete confirmation on.

The database key is never loaded from `settings.json`; it is runtime-only.

If preferences repeatedly fail to save, check write permissions for the data directory and whether another tool is locking/replacing files.

## Search does not find what I expect

Search currently covers:

- given name;
- family name;
- nickname;
- phone number;
- email address.

Normal views hide archived contacts. Use Archived when looking for archived records.

`%`, `_`, and backslash in user search text are escaped for literal SQL `LIKE` behavior.

The current desktop does not expose group/tag search controls even though repository-level filters exist.

## Search results lag while typing

Search is intentionally debounced by 180 ms and cancels an older pending request when text changes. A very large database can still take longer because the current repository materializes full matching aggregates and loads child collections per contact.

See `performance.md` if this is reproducible at scale.

## Saving an existing rich contact removed repeated data

The underlying model/storage supports multiple phones/emails/addresses/organizations/groups/tags, but the **current desktop draft editor only exposes one phone and one email and does not expose the other repeated collections**.

Because repository saves replace child collections with the aggregate supplied by the editor, editing/saving a rich contact through this simplified draft can drop unexposed repeated fields.

If this happened and the data matters:

1. do not continue destructive edits;
2. preserve the current database and any pre-change verified backup;
3. restore the appropriate verified backup through the Data tools restore flow if that is the correct recovery point;
4. keep the original backup until you have verified the recovered contact data.

This UI preservation gap is documented as a roadmap item; do not assume full rich-field editing is complete.

## Birthday is rejected

The desktop editor requires exact:

```text
yyyy-MM-dd
```

Example fictional date:

```text
2000-01-02
```

Inputs such as `02/01/2000` are rejected by the current draft parser.

## CSV import fails

Check:

- the selected file is `.csv` (anything other than `.vcf`/`.vcard` is decoded as CSV after the picker filters it);
- text is compatible with UTF-8/BOM detection;
- the file is at most 5,000,000 characters;
- expected header names such as `GivenName`, `FamilyName`, `Email`, etc.;
- every parsed contact passes domain email/phone validation.

The service validates the whole parsed batch. One invalid contact can reject persistence of the complete batch, which is intentional atomic behavior.

Invalid birthday text can be a non-fatal parser warning, but invalid email/phone values become validation errors later.

## vCard import loses unsupported fields

The current codec is a focused vCard 4.0 subset. It primarily handles `N`, `FN`, `TEL`, `EMAIL`, `BDAY`, and `NOTE`. Addresses, organizations, photos, every parameter/encoding variant, groups, and other advanced properties are not full-fidelity today.

Use a database backup rather than vCard as the full-fidelity ContactCore recovery format.

## CSV looks dangerous in a spreadsheet

ContactCore quotes CSV values but does not currently neutralize spreadsheet formula prefixes. Spreadsheet programs can interpret some leading characters as formulas.

Treat external/contact-derived CSV text as untrusted data in spreadsheet software. Do not enable macros/active content merely because the file was exported by ContactCore.

## Backup creation fails

Backup creation opens the active database, uses SQLite's backup API, then verifies the destination database. Common causes include:

- destination directory permissions;
- storage full/unavailable;
- SQLite/provider error;
- integrity/identity verification failure;
- keyed provider mismatch.

A backup path should not be treated as successfully created until ContactCore reports verified success.

## Restore says the file is not a ContactCore backup

The restore source must be a valid supported ContactCore database. The service checks SQLite integrity, required tables, schema version, and schema-family identity rules.

A random `.db` file can be perfectly valid SQLite and still be correctly rejected.

## Restore rejects a newer schema

ContactCore rejects a backup whose schema version is newer than the running build supports. Install/run a build that supports that database or restore a backup from a schema version supported by the current build.

Do not manually delete rows from `schema_migrations` to force an unsupported downgrade on real data.

## Restore fails before replacement

The service validates and stages the selected backup before replacing the active database. If verification/migration fails while staging, the original active database should remain in place.

Preserve the failing backup separately if it may be useful for diagnosis, but never upload a real one publicly.

## Restore fails after replacement attempt

BackupService performs final verification after switching. If that verification fails, it attempts to:

- move the failed restored database into `backups/failed-restore-*.db`;
- copy the verified `pre-restore-*.db` snapshot back to the active path.

Inspect the `backups/` directory carefully. Do not delete these files until you have identified which database contains the desired data.

If reporting a bug publicly, reproduce with a fictional database instead of attaching these real recovery artifacts.

## Restore temporary file remains

The staging `.restore-*.tmp` file is cleaned in a `finally` block. If the native picker had to create a stream-backed temporary copy, deletion is also attempted after the restore workflow.

An abrupt process/OS termination can still leave temp files. Verify their contents/sensitivity before removing them and avoid posting them publicly.

## Permanent delete button does nothing

If confirmation is enabled and a confirmation callback cannot be provided, deletion is intentionally blocked.

If a dialog appears, only affirmative confirmation proceeds. Cancel/close leaves the contact intact.

## Duplicate command does not let me merge

The main-window **Find duplicates** command currently reports candidate count/highest score. The application layer has a `ContactMerger`, but the full interactive review/merge UI is not yet implemented in the current main-window workflow.

This is a known product limitation, not a hidden shortcut.

## Theme change is not persisted

Use **Save settings** after selecting System/Light/Dark. Closing/canceling the Settings surface does not intentionally save the draft setting.

Unknown theme values in the preferences model normalize to System.

## Reduced motion seems to do nothing

The preference is persisted, but the current custom UI contains little bespoke motion. It is primarily a contract for present/future animations, not a system-wide OS animation switch.

## Cannot delete a temporary/test database on Windows

SQLite pools/file handles can keep files open briefly. Ensure ContactCore/tests are closed and call/allow connection disposal. The infrastructure tests use `SqliteConnection.ClearAllPools()` in backup test cleanup where needed.

Do not use file-lock workarounds that risk deleting the currently active real profile.

## CI passes locally but fails on one platform

Inspect the failing matrix job and its test artifacts. Common differences include path separators/case sensitivity, file locking, native SQLite/Avalonia runtime behavior, and OS-specific file picker/window APIs.

Treat a single-platform failure as a real compatibility signal until reproduced/explained.

## Safe diagnostic bundle

When asking for public help, include only:

- ContactCore commit/release version;
- OS/version/architecture;
- `dotnet --info` with personal paths redacted if necessary;
- exact commands used;
- sanitized exception/error text;
- minimal reproduction using fictional contacts;
- whether a custom data path or encryption provider is involved.

Do **not** include:

- real `contactcore.db`;
- `-wal`/`-shm` files;
- backups/recovery copies;
- real CSV/vCards;
- database key/environment dump;
- screenshots showing real contacts or private paths unless fully sanitized.

## Last-resort profile reset

Only for a profile you are certain is disposable:

1. close ContactCore;
2. confirm the exact data directory in Settings/environment/configuration;
3. copy anything needed to a safe location;
4. delete/rename the disposable directory;
5. restart to create a fresh database.

Never use profile deletion as the default fix for an unknown database problem involving important contacts.
