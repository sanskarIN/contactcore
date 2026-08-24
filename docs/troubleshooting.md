# Troubleshooting

Start with low-risk checks. Do not delete native contact data or clear browser site data as a first response. If contacts matter, preserve an appropriate copy before destructive troubleshooting.

## First identify the target

ContactCore now has different application heads/storage boundaries:

- Desktop — Windows/Linux/macOS, native SQLite;
- Android — `net10.0-android`, native SQLite;
- iOS/iPadOS — `net10.0-ios`, native SQLite;
- Browser/WebAssembly — `net10.0-browser`, IndexedDB.

The correct diagnosis depends on the target.

## Core project will not restore/build

Check SDK:

```bash
dotnet --version
dotnet --info
```

`global.json` expects stable .NET 10 SDK baseline 10.0.100 with latest compatible feature-band roll-forward and prereleases disabled.

For workload-free core quality use:

```bash
dotnet restore ContactCore.Core.slnx
dotnet build ContactCore.Core.slnx -c Release
```

Do **not** use the complete `ContactCore.slnx` as a first diagnostic unless Android/iOS/WebAssembly workloads are installed; the complete solution intentionally includes those platform heads.

## Format check fails

```bash
dotnet format ContactCore.Core.slnx
```

Review the diff, then:

```bash
dotnet format ContactCore.Core.slnx --verify-no-changes
```

Warnings are errors by repository policy; fix them rather than globally disabling the rule.

## Android target fails before compilation

Check workload:

```bash
dotnet workload list
dotnet workload install android
```

Then:

```bash
dotnet restore src/ContactCore.Android/ContactCore.Android.csproj
dotnet build src/ContactCore.Android/ContactCore.Android.csproj -c Release --no-restore
```

If failure mentions Android SDK/JDK/build-tools, verify the .NET Android toolchain installation rather than removing the Android project or CI gate.

A normal CI/source build does not require a production Play Store keystore. Signing errors belong to a separately configured distribution pipeline.

## iOS/iPadOS target fails

Use macOS with required Apple tooling.

```bash
dotnet workload list
dotnet workload install ios
dotnet restore src/ContactCore.iOS/ContactCore.iOS.csproj
dotnet build src/ContactCore.iOS/ContactCore.iOS.csproj -c Release --no-restore
```

Distinguish:

- compile/workload/Xcode/toolchain failure;
- simulator/device deployment failure;
- Apple signing/provisioning failure.

The public build gate intentionally does not contain production signing credentials.

## Browser/WebAssembly target fails to build

```bash
dotnet workload list
dotnet workload install wasm-tools
dotnet restore src/ContactCore.Browser/ContactCore.Browser.csproj
dotnet build src/ContactCore.Browser/ContactCore.Browser.csproj -c Release --no-restore
```

Inspect the first error for:

- WebAssembly SDK/workload;
- Avalonia.Browser references;
- `[JSImport]` source generation;
- shared UI/XAML compile;
- browser project target framework.

## Browser publish loads incorrectly

Publish:

```bash
dotnet publish src/ContactCore.Browser/ContactCore.Browser.csproj -c Release -o artifacts/browser
```

Serve output over HTTP(S). Direct `file://` loading is not the supported host model.

If the page appears but application does not boot, inspect browser developer-console/network errors for missing `_framework` assets, `main.js`, `contactcore-storage.js`, base path problems, or WebAssembly runtime errors.

Do not post screenshots containing real contacts/browser storage content publicly.

## Browser says local storage is unavailable / data disappears

Browser contacts live in IndexedDB for the current origin/profile. Preferences normally use local browser storage.

Data can disappear because of:

- clearing site data;
- private/incognito session ending;
- deleting/changing browser profile;
- storage policy/quota eviction;
- moving the application to a different origin/host;
- browser/extension/enterprise policy.

If data matters, export a portable copy before clearing anything.

There is no native SQLite backup file in the WebAssembly target. Do not search for `contactcore.db` as a browser recovery method.

## Browser data exists in another deployment but not this one

IndexedDB is scoped by web origin. `https://example-a/...` and `https://example-b/...` do not automatically share ContactCore browser data, even when serving identical application files.

Use explicit export/import for intentional moves unless a future version adds a documented full-fidelity browser migration format.

## Editing in two browser tabs gives unexpected state

Cross-tab optimistic concurrency/conflict resolution is not currently implemented. Simultaneous editing in multiple tabs is not a supported synchronization workflow.

Close extra editing tabs, reload from the intended current state, and reproduce with fictional data before reporting a browser consistency bug.

## Desktop window/native app will not start

Check:

- `dotnet --info` for source runs;
- terminal/runtime exception output;
- graphical session availability on Linux desktop;
- native platform toolchain/runtime prerequisites;
- whether `CONTACTCORE_DATABASE_KEY` was set accidentally;
- native data directory accessibility.

## Native encryption requested but startup is blocked

This is expected fail-closed behavior when `CONTACTCORE_DATABASE_KEY` is non-empty but `PRAGMA cipher_version` cannot prove a compatible provider.

If encryption was not intended, remove the environment variable from the current process/shell and restart. Do not bypass cipher verification to make ordinary SQLite appear encrypted.

Browser/WebAssembly does not use this SQLite key integration.

## Native key seems ignored on first launch

Current `JsonAppPreferences` reads the environment key even when `settings.json` does not exist. A first launch with a key should request keyed behavior and fail closed if cipher provider is absent.

Silent first-launch ignore indicates an older build/regression.

## Native app uses wrong data directory

`CONTACTCORE_DATA_PATH` is a **directory** override. Native layout is:

```text
<directory>/contactcore.db
<directory>/settings.json
<directory>/backups/
```

The shared/mobile Settings view reports its local data location. Use clearly disposable paths for experiments.

## Native settings reset

Malformed/missing `settings.json` falls back to safe defaults:

- System theme;
- reduced motion off;
- permanent-delete confirmation on.

The database key never comes from persisted settings.

For browser preference reset, investigate browser storage policy/profile rather than native filesystem permissions.

## Search does not find expected contact

Free-text search covers names, phones, emails. Normal views hide archived contacts; use Archived for archived-only display.

Native SQLite search escapes `%`, `_`, and backslash as literal characters. Browser search uses normalized in-memory matching over the IndexedDB-loaded contact state.

## Search results lag while typing

Shared/desktop search uses a 180 ms debounce with cancellation of prior requests. Very large datasets can still be slow because full aggregates are involved and duplicate candidate generation remains pairwise in memory.

## A rich field disappears after editing

This is a data-integrity regression, not an intentional compact-editor limitation. Current editor models represent phones/emails/addresses/organizations/groups/tags.

Expected invariants:

- existing contact-owned repeated IDs remain through ordinary edits;
- removing one row removes only that row;
- delimiter-containing group/tag names remain exact;
- blank new rows are ignored;
- label-only legacy addresses remain preservable;
- unchanged group/tag identity remains stable;
- true per-contact group/tag rename is reassignment.

Preserve native backups/browser exports as appropriate and reproduce with fictional data.

## New unsaved contact asks for permanent deletion

Unsaved drafts track `IsPersisted=false`. Delete/discard should close the draft without repository deletion. A permanent-delete confirmation for a never-saved contact indicates older behavior/regression.

## Birthday rejected

Use exact:

```text
yyyy-MM-dd
```

Example: `2000-01-02`.

## CSV imports zero contacts

The header must contain at least one supported ContactCore column such as:

```text
GivenName
FamilyName
Nickname
Email
Phone
Birthday
Notes
```

No recognized header intentionally means zero contacts + warning.

## Duplicate CSV headers

First occurrence is used and a warning is produced. Fix source CSV if ambiguity was accidental.

## One invalid imported contact rejects the batch

Application validates the whole parsed batch before persistence. Native uses one SQLite transaction; browser uses one gated persisted snapshot replacement. This all-or-nothing/storage-consistent behavior is intentional.

## CSV formula warning

Formula-like text is preserved, not executed by ContactCore. Spreadsheet software may later interpret values beginning with `=`, `+`, `-`, or `@`. Treat exported/imported CSV as untrusted data in spreadsheet programs.

## Import too large

Portable picker reader bounds selected text to **5,000,000 characters**. Split/clean input using a trusted workflow rather than casually removing the bound.

## vCard loses fields

The codec is focused rather than full vCard. It does not promise round trip for every address/organization/photo/custom extension/group/tag/identity/encoding.

For native full recovery use verified SQLite backup. Browser currently has no full-fidelity native DB backup equivalent; use documented export limitations deliberately.

## Native backup creation fails

Possible causes:

- destination permissions/storage space;
- SQLite/provider errors;
- integrity/schema/identity verification failure;
- incompatible keyed provider;
- platform picker/filesystem behavior.

Do not consider a backup successful until verified success is reported.

## Browser has no Backup/Restore button

Expected. Browser persistence is IndexedDB, not native SQLite. Shared UI capability flags intentionally disable native database backup/restore and encryption claims.

Use CSV/vCard export for a portable copy, understanding format fidelity limits.

## Native restore rejects selected DB

A restore source must be supported ContactCore SQLite: integrity, required schema/version, and schema-family identity are checked. An arbitrary valid `.db` is correctly rejected.

## Native restore rejects future schema

Use a build that supports that schema or an older compatible verified backup. Never edit migration tables on real data to force downgrade.

## Native restore fails before switch

Source verification/staging/migration/verification happen before active replacement. Failure there should leave active DB in place.

## Native restore fails after switch

Final verification attempts recovery:

- retain failed restored DB under `backups/failed-restore-*.db`;
- restore verified `pre-restore-*.db` snapshot when available.

Do not delete recovery artifacts until desired state is identified.

## Temp restore files remain

Abrupt termination can leave temporary files despite best-effort cleanup. Treat them as sensitive until privately inspected/deleted.

## Permanent delete appears to do nothing

When confirmation is required, lack/cancel of confirmation intentionally blocks deletion. Unsaved draft discard uses no repository delete.

## No duplicate candidates

Duplicate scoring uses heuristics/thresholds. Missing a possible duplicate is safer than automatic destructive merge. No candidate is auto-merged.

## Wrong survivor selected but not confirmed

Cancel. No destructive merge should persist until explicit confirmation.

## Wrong duplicate was confirmed

There is no general undo stack. Native storage makes merge atomic; browser storage makes it gated/rollback-on-persistence-failure, but neither is undo for a correctly persisted mistaken decision. Restore/import a suitable prior copy if appropriate.

## Duplicate merge says a contact disappeared

Expected stale-safety. Refresh candidates and review again instead of forcing reviewed stale state to persist.

## Duplicate scan slow

Current candidate generation is pairwise in memory; cost grows roughly quadratically. See `performance.md` and `ROADMAP.md`.

## Theme does not persist

Use Save settings. Native settings persist to JSON; browser settings use browser-local storage when available. Private/storage-blocked browser contexts may only retain session fallback.

## CI passes locally but fails on one platform

Inspect the exact job:

### Core OS matrix

Consider path separators/case sensitivity, file locks, newlines, native SQLite behavior, SDK resolution, XAML/MVVM generation.

### Browser

Consider `wasm-tools`, WebAssembly SDK, JS interop generation, host assets, XAML.

### Android

Consider Android workload/SDK/JDK/toolchain and platform package compilation.

### iOS

Consider macOS runner, iOS workload, Xcode/toolchain and whether error is compile vs signing/provisioning.

Treat one-platform failure as a real compatibility signal until understood.

## Safe public diagnostic bundle

Include only:

- ContactCore commit/release;
- target platform/OS/version/architecture/browser;
- sanitized `dotnet --info` when relevant;
- exact command;
- sanitized error text;
- minimal fictional reproduction;
- whether custom native data path/encryption provider is involved;
- whether browser storage/private mode is involved.

Do **not** include:

- real native DB/WAL/SHM;
- native backups/recovery artifacts;
- browser IndexedDB dumps containing contacts;
- real CSV/vCards;
- database keys/environment dumps;
- signing keys/certificates/provisioning profiles/passwords;
- screenshots with real contacts/private paths.

## Last-resort disposable reset

### Native

Only for a known disposable profile: close app, verify exact directory, preserve needed data, rename/delete disposable directory, restart.

### Browser

Only for a disposable browser origin/profile: export anything needed, then clear ContactCore site data/IndexedDB through normal browser controls.

Never delete a real profile/site store as a generic fix for an unknown problem.
