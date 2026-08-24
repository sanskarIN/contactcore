# Architecture

ContactCore is a local-first cross-platform application organized as a modular monolith. Domain rules and application use cases are shared across every target. Avalonia presentation is split between the mature desktop head and a portable single-view UI used by Android, iOS/iPadOS, and WebAssembly. Native platforms use the hardened SQLite infrastructure; the browser implements the same repository contract over IndexedDB.

## Solution map

```text
                                   ┌──────────────────────────────┐
                                   │      ContactCore.Domain      │
                                   └──────────────▲───────────────┘
                                                  │
                                   ┌──────────────┴───────────────┐
                                   │   ContactCore.Application    │
                                   │ contracts + use cases/codecs │
                                   └──────────▲──────────▲────────┘
                                              │          │
                      native implementation   │          │ browser implementation
                                              │          │
                ┌─────────────────────────────┘          └──────────────────────────┐
                │                                                                  │
   ┌────────────┴─────────────┐                                      ┌─────────────┴────────────┐
   │ ContactCore.Infrastructure│                                      │ ContactCore.Browser      │
   │ SQLite/migrations/backup  │                                      │ IndexedDB repository     │
   └────────────▲─────────────┘                                      └─────────────▲────────────┘
                │                                                                  │
   ┌────────────┴─────────────┐        ┌────────────────────────┐                   │
   │ ContactCore.Native       │        │ ContactCore.UI         │◄──────────────────┘
   │ native service composition│       │ portable Avalonia UI   │
   └──────▲──────────▲────────┘        └──────▲────────▲────────┘
          │          │                        │        │
    ┌─────┘          └─────┐            ┌─────┘        └─────┐
    │                      │            │                    │
ContactCore.Android   ContactCore.iOS   │            ContactCore.Browser
                                          
ContactCore.Desktop ───────────────► Application + Infrastructure
(mature desktop shell)
```

`ContactCore.slnx` contains the complete cross-platform repository. `ContactCore.Core.slnx` deliberately excludes workload-dependent Android/iOS/Browser heads so ordinary three-OS core validation and CodeQL do not require all mobile/WebAssembly workloads on every runner.

## Dependency rules

### Domain

`ContactCore.Domain` contains contact entities/records, validation, display-name behavior, deep-copy behavior, and normalization helpers. It has no dependency on Application, Infrastructure, Avalonia, SQLite, Android, iOS, or JavaScript interop.

### Application

`ContactCore.Application` depends on Domain. It owns:

- `IContactRepository`, `IAppPreferences`, and `IBackupService` contracts;
- contact normalization/validation and CRUD orchestration;
- duplicate scoring and merge policy;
- stale-safe merge orchestration through repository contracts;
- CSV and focused vCard import/export codecs.

Application code is therefore reusable by native and browser persistence implementations.

### Infrastructure

`ContactCore.Infrastructure` depends on Application and Domain. It owns the native storage path:

- `AppPaths`;
- `SqliteConnectionFactory`;
- ordered database migrations/schema identity;
- `SqliteContactRepository`;
- shared group/tag dictionary linking;
- atomic SQLite aggregate updates/duplicate merges;
- verified native backup/restore;
- JSON preferences;
- diagnostic redaction.

It is referenced by desktop and through `ContactCore.Native` for mobile targets, but is deliberately not referenced by `ContactCore.Browser`.

### Shared UI

`ContactCore.UI` depends on Application and Domain, not Infrastructure. It owns:

- the Avalonia `App` that supports classic desktop and single-view application lifetimes;
- responsive `MainView`;
- portable `MainViewModel` workflows;
- the complete rich `ContactDraftViewModel`;
- repeated field draft view models;
- duplicate-review presentation models;
- platform capability metadata;
- portable Avalonia storage-picker integration;
- in-view confirmation instead of requiring a desktop `Window` dialog.

This separation prevents Android/iOS/browser presentation from depending on desktop-only `Window` behavior.

### Native composition

`ContactCore.Native` depends on Application, Infrastructure, and shared UI. It is intentionally small: `NativeAppServiceFactory` composes `AppPaths`, JSON preferences, SQLite connection/migration/repository, `ContactService`, and `BackupService`, then returns the shared UI's `AppServices` object.

Android and iOS/iPadOS use this factory with platform-specific names.

### Desktop

`ContactCore.Desktop` remains the mature desktop-specific composition/presentation head. It owns the existing `Window`, desktop confirmation dialog, desktop styling, and desktop view models while sharing Domain/Application/Infrastructure behavior with the new heads.

Keeping this head intact avoids destabilizing the already-hardened desktop workflow merely to force every platform through identical window chrome.

### Android

`ContactCore.Android` contains only Android host responsibilities:

- `net10.0-android` application project;
- `AvaloniaAndroidApplication<App>`;
- `AvaloniaMainActivity` launcher;
- configuration of `AppBootstrapper` with native SQLite services.

All contact workflows live below the Android host.

### iOS/iPadOS

`ContactCore.iOS` contains:

- `net10.0-ios` application project;
- `AvaloniaAppDelegate<App>`;
- UIKit application entry point;
- `Info.plist` declaring both iPhone and iPad families;
- configuration of `AppBootstrapper` with native SQLite services.

### Browser

`ContactCore.Browser` depends on Application, Domain, and shared UI but **not** Infrastructure. It owns:

- WebAssembly startup;
- `BrowserContactRepository` implementing `IContactRepository`;
- browser preferences;
- an unsupported native-backup adapter used to make the capability boundary explicit;
- .NET/JavaScript storage interop;
- IndexedDB/local-storage JavaScript module and static host assets.

This is a deliberate platform adapter, not a second set of business rules.

## Startup paths

### Desktop

```text
ContactCore.Desktop App
  → AppPaths
  → JsonAppPreferences
  → SqliteConnectionFactory
  → DatabaseMigrator
  → SqliteContactRepository
  → ContactService
  → BackupService
  → MainWindowViewModel
  → MainWindow
```

### Android / iOS

```text
platform host
  → AppBootstrapper.Configure(NativeAppServiceFactory)
  → ContactCore.UI.App
  → AppServices
      ├─ ContactService
      ├─ SqliteContactRepository
      ├─ BackupService
      └─ JsonAppPreferences
  → MainViewModel
  → MainView
  → ISingleViewApplicationLifetime.MainView
```

### Browser

```text
wwwroot/main.js
  → import contactcore-storage.js
  → expose contactcoreStorage JS bridge
  → start .NET WebAssembly
  → AppBootstrapper.Configure(BrowserAppServices)
  → ContactCore.UI.App
  → BrowserContactRepository
  → IndexedDB
  → MainViewModel / MainView
```

The JavaScript storage bridge is loaded before .NET main starts so repository initialization cannot race a missing interop module.

## Contact read flow

The presentation type differs by head, but the application contract is the same:

```text
search UI
  → view-model query state
  → 180 ms debounce/cancellation
  → ContactService.SearchAsync
  → IContactRepository.SearchAsync
      ├─ native: parameterized SQLite query
      └─ browser: normalized in-memory filtering over IndexedDB-loaded state
  → complete Contact aggregates
  → contact list view models
```

Free-text search covers names, phones, and email addresses. Repository filters support favorites, archived inclusion, tag, group, and starting letter.

## Contact edit/write flow

```text
rich editor
  → ContactDraftViewModel.ToContact()
  → ContactService.SaveAsync
      ├─ normalize
      ├─ update timestamp
      └─ validate
  → IContactRepository.UpsertAsync
      ├─ native: SQLite aggregate transaction
      └─ browser: gated snapshot mutation + IndexedDB persistence
```

The supplied `Contact` represents the complete desired aggregate.

## Identity model

### Contact-owned repeated rows

Phones, emails, addresses, and organizations are contact-owned. Their IDs remain stable through ordinary edits unless the user intentionally removes/recreates a row.

### Shared group/tag dictionary rows

Groups and tags are shared case-insensitive dictionary identities on the SQLite model. Shared draft conversion preserves the important semantics:

- unchanged or normalization-equivalent assignment → preserve the existing ID/canonical name;
- true per-contact name change → create a fresh identity, which is a reassignment rather than a silent global rename;
- new assignment → new identity;
- duplicate names within one contact collapse case-insensitively;
- commas and semicolons remain exact values because assignments are independent records.

The browser stores the same domain IDs/aggregate representation so data exported or reasoned about through Application behavior retains the same identity model even though persistence is not relational SQLite.

## Other editor invariants

- root `Contact.Id` and `CreatedAt` survive ordinary edits;
- blank newly added rich rows are ignored;
- existing label-only addresses remain preservable;
- removing one row/link does not remove unrelated values;
- draft edits do not mutate the source aggregate before save;
- generated GUIDs do not imply persistence;
- `IsPersisted` separates unsaved drafts from stored contacts.

## Duplicate detection and merge

Detection remains advisory on every platform:

```text
Find duplicates
  → load all contacts including archived
  → DuplicateDetector.Find
  → candidate list + reasons/score
  → explicit survivor choice
  → explicit confirmation
  → ContactService.MergeAsync
```

`ContactService.MergeAsync` reloads both records, merges, normalizes, timestamps, validates, and then asks the repository for a stale-safe destructive merge.

### Native merge

`SqliteContactRepository.MergeAsync` checks both records and updates survivor/deletes secondary in one SQLite transaction.

### Browser merge

`BrowserContactRepository.MergeAsync` runs behind the repository gate, requires both reviewed IDs still exist, updates/removes the two records, and persists one new IndexedDB snapshot. If persistence throws, it restores the prior in-memory dictionary before rethrowing.

Thus the browser path preserves the important application property that a failed storage write does not leave the current repository instance half-mutated.

## Bulk import

```text
Avalonia storage picker
  → bounded text read (5,000,000 characters)
  → CSV/vCard codec
  → ImportResult
  → ContactService.ImportAsync
      ├─ deep-copy
      ├─ normalize
      ├─ validate entire batch
      └─ shared UpdatedAt
  → IContactRepository.UpsertManyAsync
```

Native SQLite persists the batch transactionally. Browser persistence applies the batch behind one gate and persists one replacement snapshot, restoring the previous in-memory state if the IndexedDB write fails.

CSV/vCard remain interchange formats rather than full-fidelity backups.

## Backup and restore boundary

### Native targets

Native backup uses SQLite's backup API followed by integrity/schema/version/ContactCore-identity verification. Restore uses read-only verification, a pre-restore recovery snapshot, staging, migration/verification, active replacement, final verification, and rollback handling.

See `storage-backup-recovery.md` for exact failure behavior.

### Browser

There is no native SQLite database inside the browser target, so SQLite backup/restore is intentionally disabled. The shared UI receives `SupportsDatabaseBackups=false` and presents export guidance instead of invoking a fake backup implementation.

This is an architectural capability boundary, not conditional UI hiding around an unsupported operation.

## Browser storage consistency model

`BrowserContactRepository` uses a `SemaphoreSlim` to serialize initialization and writes within one application instance. A write:

1. ensures repository initialization;
2. enters the gate;
3. snapshots current in-memory contacts;
4. applies the requested mutation;
5. serializes the complete ordered contact document set;
6. asks JavaScript to commit it to IndexedDB;
7. if persistence fails, restores the pre-write in-memory snapshot;
8. releases the gate.

The JavaScript layer uses a single IndexedDB object-store value for the versioned contact JSON snapshot. This favors simple atomic replacement and schema portability at the current scale over a browser-specific relational emulation.

Cross-tab optimistic concurrency/merge is not currently implemented; users should not treat simultaneous editing in multiple tabs as a collaborative synchronization feature.

## Preferences

Native targets use `JsonAppPreferences`; the runtime database key is deliberately excluded from serialized settings.

Browser preferences use local browser storage and fall back to session memory when access throws. They contain theme/reduced-motion/delete-confirmation state only, not a SQLite database key.

## Platform capabilities

`AppPlatformCapabilities` keeps differences visible to shared presentation code:

```text
PlatformName
DataLocation
BackupLocation
SupportsDatabaseBackups
SupportsDatabaseEncryption
```

Native heads report backup/encryption integration capability. Browser reports IndexedDB data location and disables native database backup/encryption claims.

## Error boundary

Shared UI sanitizes common local paths before displaying exception text. Lower layers must still avoid embedding secrets or unnecessary contact payloads in exceptions. Parser/validation warnings likewise avoid echoing private invalid values when not needed.

## Security/data-safety principles

- local-first; no mandatory account/cloud/telemetry dependency;
- parameterized SQL on SQLite targets;
- literal SQLite wildcard escaping;
- fail-closed requested native database encryption;
- runtime-only native database key;
- verified native backup/restore with recovery path;
- browser-native persistence isolated behind `IContactRepository`;
- bounded import and pre-persistence batch validation;
- confirmation-gated persisted delete/duplicate merge and native restore;
- unsaved drafts never masquerade as persisted rows;
- complete aggregate editor preserves ownership/identity semantics;
- shared dictionary renames are reassignment, not accidental global mutation;
- duplicate merge remains stale-safe across repository implementations;
- platform capabilities prevent browser/native feature overclaiming;
- CI separately verifies core, Android, iOS, and WebAssembly build paths;
- CodeQL remains independent from mobile workload availability.

## Test and build architecture

Current behavioral test projects:

- `ContactCore.Domain.Tests` — validation/normalization/model behavior;
- `ContactCore.Application.Tests` — services, duplicate/merge policy, CSV/vCard behavior;
- `ContactCore.Infrastructure.Tests` — SQLite repository, dictionary semantics, stale-safe merge, preferences, paths, redaction, backup/restore;
- `ContactCore.Desktop.Tests` — non-visual desktop draft behavior.

GitHub Actions adds platform build gates:

- `ContactCore.Core.slnx` restore/format/build/test on Ubuntu, Windows, and macOS;
- WebAssembly workload + Browser Release build;
- Android workload + Android Release build;
- iOS workload + iOS Release build on macOS;
- CodeQL on `ContactCore.Core.slnx`.

A compile/build gate is not a replacement for manual device/browser/accessibility verification; documentation keeps those claims separate.

## Why a modular monolith

ContactCore still has no server/microservice boundary. Project-level modularity gives strong dependency direction, framework isolation, replaceable repository implementations, and focused tests without introducing a cloud/network privacy surface merely to gain platform portability.

See ADR `0001-modular-monolith.md`.

## Evolution rules

1. Put pure rules/types in Domain.
2. Put use-case orchestration/contracts in Application.
3. Put native filesystem/SQLite/backup concerns in Infrastructure.
4. Put portable Avalonia single-view workflows in UI.
5. Keep Android/iOS/browser projects thin platform heads/adapters.
6. Keep browser persistence behind Application contracts instead of leaking JS APIs into use-case logic.
7. Test behavior at the lowest useful layer and add integration regressions for data-safety boundaries.
8. Preserve complete aggregate data and contact-owned/shared-dictionary identity semantics.
9. Make destructive multi-record changes stale-safe and storage-consistent.
10. Never convert duplicate heuristics into automatic destructive decisions.
11. Treat platform capability differences explicitly instead of silently pretending native features exist everywhere.
12. Add an ADR for durable storage/dependency/encryption/privacy architecture changes.
