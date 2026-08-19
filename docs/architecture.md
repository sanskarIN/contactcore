# Architecture

ContactCore is a local-first desktop application organized as a small modular monolith. It favors explicit layer boundaries over framework-heavy indirection so the data model, use cases, SQLite implementation, and Avalonia UI can evolve independently and remain testable.

## Solution map

```text
ContactCore.Desktop
    │
    ├── ContactCore.Application ──► ContactCore.Domain
    │            ▲
    │            │ abstractions implemented by
    │            │
    └── ContactCore.Infrastructure ──► ContactCore.Domain
```

The solution also contains one test project per production layer.

## Dependency rules

### Domain

`ContactCore.Domain` is the innermost layer. It contains contact entities/records, validation, display-name behavior, deep-copy behavior, and normalization helpers. It has no project dependency on Application, Infrastructure, or Desktop.

Domain code must remain usable without Avalonia and without SQLite.

### Application

`ContactCore.Application` depends on Domain. It owns:

- repository, preferences, and backup abstractions;
- contact use cases and validation boundary;
- import normalization and bulk-validation flow;
- duplicate scoring/comparison;
- deterministic merge behavior;
- CSV and vCard codecs.

Abstractions are intentionally defined here so infrastructure depends inward on the use-case contract rather than Application depending on concrete SQLite types.

### Infrastructure

`ContactCore.Infrastructure` depends on Application and Domain. It implements:

- cross-platform application paths;
- SQLite connection configuration;
- ordered schema migrations;
- aggregate persistence/search;
- verified SQLite backup/restore;
- JSON preferences;
- PII-oriented diagnostic redaction.

Infrastructure is allowed to know about `Microsoft.Data.Sqlite`; Domain is not.

### Desktop

`ContactCore.Desktop` depends on Application and Infrastructure and acts as the composition root. It constructs concrete services and presents them through Avalonia view/view-model code.

The desktop layer also owns platform services that are naturally UI-specific: native file pickers, confirmation dialogs, theme application, and keyboard shortcuts.

## Main runtime flow

At startup:

```text
App
 ├─ AppPaths
 ├─ JsonAppPreferences
 ├─ SqliteConnectionFactory
 ├─ DatabaseMigrator
 ├─ SqliteContactRepository
 ├─ ContactService
 ├─ BackupService
 └─ MainWindowViewModel → MainWindow
```

`MainWindowViewModel.InitializeAsync()` asks `ContactService` to initialize storage; the service delegates to the repository; the repository delegates to `DatabaseMigrator`.

## Contact read flow

A search follows this path:

```text
SearchBox
  → MainWindowViewModel.SearchText
  → 180 ms debounce / cancellation
  → ContactService.SearchAsync
  → IContactRepository.SearchAsync
  → SqliteContactRepository
  → parameterized SQLite query
  → Contact aggregates
  → ContactListItemViewModel collection
```

The repository loads contact roots first and then their child collections. Current free-text search covers names, phone numbers, and email addresses. Filters exist for favorites, archived inclusion, tag, group, and starting letter.

## Contact write flow

```text
Desktop draft
  → ContactDraftViewModel.ToContact()
  → ContactService.SaveAsync
      ├─ normalize
      ├─ update timestamp
      └─ validate
  → IContactRepository.UpsertAsync
  → SqliteContactRepository.UpsertManyAsync([contact])
  → SQLite transaction
```

The repository treats a `Contact` as the complete desired aggregate. It upserts the contact row, deletes that contact's child/link rows, and inserts the supplied current children inside the same transaction.

### Important desktop limitation

The persistence model supports rich repeated fields, but the current desktop draft exposes only one phone and one email and does not expose addresses, organizations, groups, or tags. Because writes replace child collections, expanding/preserving rich editing is a high-priority UI correctness area. See `desktop-ui.md`.

## Bulk import flow

```text
Native file picker
  → bounded UTF-8 text read
  → CSV/vCard codec
  → ImportResult(contacts, warnings)
  → ContactService.ImportAsync
      ├─ deep-copy each contact
      ├─ normalize each contact
      ├─ validate complete batch
      └─ one UpdatedAt timestamp
  → IContactRepository.UpsertManyAsync
  → one SQLite transaction for batch
```

A validation failure occurs before persistence. A repository failure rolls back the transaction.

## Backup flow

```text
Active SQLite DB
  → SQLite BackupDatabase API
  → destination DB
  → PRAGMA integrity_check
  → ContactCore table/version/identity checks
  → success path returned
```

This avoids relying on a raw file copy of a possibly active WAL-mode database.

## Restore flow

Restore deliberately has more stages than ordinary writes:

```text
selected backup
  → read-only verify
  → verified pre-restore snapshot of active DB
  → staging copy
  → migrate staging
  → verify staging
  → clear pools / sidecars
  → replace active DB
  → verify active DB
      └─ on failure: retain failed copy + restore pre-restore snapshot
```

See `storage-backup-recovery.md` for exact recovery artifacts and failure behavior.

## Database schema ownership

`DatabaseMigrator` is the only source of schema evolution. It creates `schema_migrations`, applies ordered transactional migrations, rejects a schema version newer than the running build, and enforces the ContactCore schema-family marker introduced in schema version 2.

The schema-family identity is part of restore safety: a valid unrelated SQLite file should not be accepted as a ContactCore backup.

## SQLite connection boundary

`SqliteConnectionFactory` centralizes:

- database path normalization;
- read/write vs read-only mode;
- connection pooling selection;
- shared cache;
- foreign-key enforcement;
- busy timeout;
- optional keyed-SQLite initialization and fail-closed cipher verification.

Code needing a SQLite connection should use this factory rather than constructing differently configured connections ad hoc.

## Preferences boundary

`IAppPreferences` is owned by Application. `JsonAppPreferences` persists theme, reduced-motion, and destructive-action confirmation preferences. The database key is runtime-only and intentionally excluded from its serialized model.

Desktop code reads/writes the abstraction, while Infrastructure owns JSON path/serialization details.

## Platform-service boundary

`MainWindowViewModel` exposes callbacks for focus, theme changes, import file selection, export saving, backup selection, and confirmation. `MainWindow` wires them to Avalonia/platform APIs and unwires them when the data context is replaced or the window closes.

This avoids putting most storage-provider/dialog APIs into the view model and permits focused view-model tests without launching the full window.

## Error boundary

Lower layers throw meaningful exceptions; the desktop layer catches workflow failures and sanitizes messages before displaying them. Validation deliberately avoids echoing invalid email/phone values.

Do not interpret desktop sanitization as permission to create exceptions containing arbitrary secrets or full contact payloads in lower layers.

## Security architecture principles

- local-first; no mandatory account or cloud dependency;
- user-controlled SQL values parameterized;
- LIKE wildcards escaped explicitly;
- database encryption request fails closed when no cipher provider is active;
- database key not written to normal settings;
- backup source verified before active data changes;
- restore staging and rollback path;
- import text bounded by the desktop reader;
- destructive action confirmation defaults on;
- CI and CodeQL provide independent repository checks.

## Test architecture

The solution contains:

- `ContactCore.Domain.Tests` — validation/normalization behavior;
- `ContactCore.Application.Tests` — duplicate/merge and CSV/vCard behavior;
- `ContactCore.Infrastructure.Tests` — SQLite repository, preferences, backup/restore;
- `ContactCore.Desktop.Tests` — non-visual desktop draft/view-model behavior.

Cross-platform CI runs the solution tests on Windows, Ubuntu, and macOS.

## Why a modular monolith

ContactCore is a desktop application with one local data store and no server boundary. Splitting it into networked services would increase deployment complexity, failure modes, latency, and privacy surface without delivering an obvious user benefit.

Project-level modularity still provides the architectural benefit needed here: dependency direction, independent tests, framework isolation, and explicit contracts.

See ADR `0001-modular-monolith.md` for the durable decision record.

## Evolution rules

When adding a feature:

1. Put pure contact rules/types in Domain.
2. Put use-case orchestration and contracts in Application.
3. Put filesystem/SQLite/serialization/native concerns in Infrastructure.
4. Put Avalonia/platform presentation in Desktop.
5. Add tests at the lowest layer that can prove the behavior.
6. Avoid crossing layers only to reuse a convenience helper.
7. Add an ADR for a change that alters storage strategy, dependency direction, encryption assumptions, or another long-lived architectural decision.
