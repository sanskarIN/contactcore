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

The solution contains one test project per production layer.

## Dependency rules

### Domain

`ContactCore.Domain` is the innermost layer. It contains contact entities/records, validation, display-name behavior, deep-copy behavior, and normalization helpers. It has no project dependency on Application, Infrastructure, or Desktop.

Domain code must remain usable without Avalonia and without SQLite.

### Application

`ContactCore.Application` depends on Domain. It owns:

- repository, preferences, and backup abstractions;
- contact use cases and validation boundaries;
- normalization and bulk-validation flow;
- duplicate scoring/comparison;
- deterministic aggregate merge behavior;
- orchestration of atomic destructive duplicate merge;
- CSV and vCard codecs.

Abstractions are defined here so Infrastructure depends inward on the use-case contract rather than Application depending on concrete SQLite types.

### Infrastructure

`ContactCore.Infrastructure` depends on Application and Domain. It implements:

- cross-platform application paths;
- SQLite connection configuration;
- ordered schema migrations;
- aggregate persistence/search;
- atomic survivor-update/secondary-delete merge persistence;
- verified SQLite backup/restore;
- JSON preferences;
- PII-oriented diagnostic redaction.

Infrastructure is allowed to know about `Microsoft.Data.Sqlite`; Domain is not.

### Desktop

`ContactCore.Desktop` depends on Application and Infrastructure and acts as the composition root. It constructs concrete services and presents them through Avalonia view/view-model code.

Desktop also owns UI-specific platform services: native file pickers, confirmation dialogs, theme application, keyboard shortcuts, and temporary picker-file adaptation.

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

A search follows:

```text
SearchBox
  → MainWindowViewModel.SearchText
  → 180 ms debounce / cancellation
  → ContactService.SearchAsync
  → IContactRepository.SearchAsync
  → SqliteContactRepository
  → parameterized SQLite query
  → complete Contact aggregates
  → ContactListItemViewModel collection
```

The repository loads contact roots and then their repeated child/relationship collections. Free-text search covers names, phones, and email addresses. Filters exist for favorite, archived inclusion, tag, group, and starting letter.

## Contact edit/write flow

```text
Avalonia full aggregate editor
  → ContactDraftViewModel
  → ContactDraftViewModel.ToContact()
  → ContactService.SaveAsync
      ├─ normalize
      ├─ update timestamp
      └─ validate
  → IContactRepository.UpsertAsync
  → SqliteContactRepository.UpsertManyAsync([contact])
  → SQLite transaction
```

The repository treats a `Contact` as the complete desired aggregate. It upserts the contact root, replaces that contact's child/link rows, and inserts the supplied current children inside one transaction.

### Full editor identity invariant

The desktop editor represents every repeated collection in the current domain model: phones, emails, addresses, organizations, groups, and tags.

`ContactDraftViewModel.Load` creates editable row view models while preserving each child ID. `ToContact()` reconstructs the complete aggregate with the original contact ID/creation timestamp and keeps IDs for rows that remain.

Important invariants:

- editing an existing repeated value does not silently assign it a new ID;
- removing one repeated row does not remove unrelated rows;
- blank newly added rows do not become meaningless persisted children;
- an existing label-only legacy address remains representable/preservable;
- group/tag names are independent rows, not delimiter-split strings;
- comma/semicolon characters in group/tag names therefore remain exact;
- case-insensitive duplicate group/tag names in a draft collapse to one relationship before persistence.

These are correctness requirements because the repository intentionally uses complete-aggregate replacement semantics.

## Unsaved draft boundary

A generated GUID does not prove that a contact exists in SQLite. `ContactDraftViewModel.IsPersisted` explicitly tracks whether the current draft has been saved.

An unsaved delete request is treated as discard at the Desktop boundary. Persisted contacts follow the configured permanent-delete confirmation path.

## Duplicate detection flow

```text
Find duplicates UI
  → load all contacts, including archived
  → DuplicateDetector.Find
  → DuplicatePairViewModel list
  → user reviews score/reasons/side-by-side details
  → user chooses surviving record
  → confirmation dialog
```

Detection and destructive merge are deliberately separate steps. A score never triggers an automatic merge.

## Atomic duplicate merge flow

```text
confirmed survivor + secondary IDs
  → ContactService.MergeAsync
      ├─ load both contacts
      ├─ ContactMerger.Merge
      ├─ normalize merged aggregate
      ├─ update timestamp
      └─ validate
  → IContactRepository.MergeAsync
  → SqliteContactRepository.MergeAsync
      ├─ BEGIN transaction
      ├─ upsert complete survivor aggregate
      ├─ DELETE secondary contact
      ├─ require exactly one secondary row deleted
      └─ COMMIT
```

If the secondary contact no longer exists, Infrastructure throws and rolls the transaction back. This prevents a race from committing only the survivor update while failing the destructive half of the operation.

The selected survivor determines the retained root identity. Unique copied child values receive new IDs when they originate from the secondary aggregate.

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

A validation failure occurs before persistence. A repository failure rolls back the batch transaction.

The codecs are interchange boundaries, not complete ContactCore backup serializers.

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

`DatabaseMigrator` is the source of schema evolution. It creates `schema_migrations`, applies ordered transactional migrations, rejects a schema newer than the running build, and enforces the ContactCore schema-family marker introduced in schema version 2.

Schema-family identity participates in restore safety so a valid but unrelated SQLite database is not accepted as a ContactCore backup.

## SQLite connection boundary

`SqliteConnectionFactory` centralizes:

- database path normalization;
- read/write versus read-only mode;
- connection pooling selection;
- shared cache configuration;
- foreign-key enforcement;
- busy timeout;
- optional keyed-SQLite initialization and fail-closed cipher verification.

Code needing a SQLite connection should use this factory rather than create differently configured connections ad hoc.

## Preferences boundary

`IAppPreferences` is owned by Application. `JsonAppPreferences` persists theme, reduced-motion, and permanent-delete-confirmation preferences. The database key is runtime-only and intentionally excluded from the serialized preferences model.

The environment database key is read even on first launch before a settings file exists.

## Platform-service boundary

`MainWindowViewModel` exposes callbacks for focus, runtime theme changes, import selection, export saving, backup selection, and confirmation. `MainWindow` wires/unwires them as its data context changes/closes.

This avoids embedding most Avalonia storage/dialog APIs inside the view model and permits focused non-visual tests.

## Error boundary

Lower layers throw meaningful exceptions; Desktop catches workflow failures and sanitizes messages before display. Validation and parser warnings avoid intentionally echoing private invalid values.

Desktop sanitization is defense-in-depth, not permission for lower layers to include secrets/full contact payloads in exception text.

## Security architecture principles

- local-first; no mandatory account/cloud dependency;
- user-controlled SQL values parameterized;
- `LIKE` wildcards escaped explicitly;
- database encryption request fails closed when no compatible cipher provider is active;
- database key remains runtime-only;
- backup source verified before active data changes;
- restore uses staging, recovery snapshot, final verification, and rollback handling;
- import text bounded by Desktop;
- batch import transactional;
- permanent delete guarded according to preference;
- duplicate merge always confirmation-gated and transactional;
- unsaved drafts never masquerade as persisted rows for deletion;
- complete aggregate editor preserves child identities;
- CI and CodeQL provide independent repository checks.

## Test architecture

The solution contains:

- `ContactCore.Domain.Tests` — validation/normalization behavior;
- `ContactCore.Application.Tests` — duplicate/merge and CSV/vCard behavior;
- `ContactCore.Infrastructure.Tests` — SQLite repository, atomic merge, preferences, paths, redaction, backup/restore;
- `ContactCore.Desktop.Tests` — non-visual desktop draft/view-model behavior, including complete rich-field identity/data preservation.

Cross-platform CI is configured to run restore/format/build/test on Windows, Ubuntu, and macOS; CodeQL is configured separately.

## Why a modular monolith

ContactCore is a desktop application with one local data store and no server boundary. Networked microservices would increase deployment complexity, failure modes, latency, and privacy surface without an obvious benefit.

Project-level modularity provides the boundaries needed here: dependency direction, independent tests, framework isolation, and explicit contracts.

See ADR `0001-modular-monolith.md` for the durable decision.

## Evolution rules

When adding a feature:

1. Put pure contact rules/types in Domain.
2. Put use-case orchestration/contracts in Application.
3. Put filesystem/SQLite/serialization/native concerns in Infrastructure.
4. Put Avalonia/platform presentation in Desktop.
5. Add tests at the lowest layer that can prove behavior.
6. Preserve complete aggregate identity/data across UI projection and persistence.
7. Make destructive multi-row changes atomic where consistency depends on all-or-nothing behavior.
8. Never turn heuristics such as duplicate scores into automatic destructive decisions.
9. Avoid crossing layers only to reuse a convenience helper.
10. Add an ADR when changing storage strategy, dependency direction, encryption assumptions, or another long-lived architectural decision.
