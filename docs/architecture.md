# Architecture

ContactCore is a local-first desktop application organized as a small modular monolith. It favors explicit layer boundaries so the data model, use cases, SQLite implementation, and Avalonia UI can evolve independently and remain testable.

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

`ContactCore.Domain` contains contact entities/records, validation, display-name behavior, deep-copy behavior, and normalization helpers. It has no dependency on Application, Infrastructure, Avalonia, or SQLite.

### Application

`ContactCore.Application` depends on Domain. It owns repository/preferences/backup contracts, contact use cases, normalization/validation boundaries, duplicate scoring/merge policy, atomic duplicate-merge orchestration, and CSV/vCard codecs.

### Infrastructure

`ContactCore.Infrastructure` depends on Application and Domain. It owns cross-platform paths, SQLite configuration/migrations/persistence, shared group/tag dictionary linking, atomic duplicate persistence, verified backup/restore, JSON preferences, and diagnostic redaction.

### Desktop

`ContactCore.Desktop` depends on Application and Infrastructure and is the composition root/platform adapter. It owns Avalonia presentation, native file pickers, confirmation dialogs, theme application, shortcuts, and presentation-only draft models.

## Startup

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

`MainWindowViewModel.InitializeAsync()` initializes storage through `ContactService` and the repository/migrator.

## Contact read flow

```text
SearchBox
  → MainWindowViewModel.SearchText
  → 180 ms debounce/cancellation
  → ContactService.SearchAsync
  → IContactRepository.SearchAsync
  → parameterized SQLite query
  → complete Contact aggregates
  → ContactListItemViewModel collection
```

Free-text search covers names, phones, and email addresses. Repository filters also support favorite, archived inclusion, tag, group, and starting letter.

## Contact edit/write flow

```text
Avalonia full aggregate editor
  → ContactDraftViewModel.ToContact()
  → ContactService.SaveAsync
      ├─ normalize
      ├─ update timestamp
      └─ validate
  → IContactRepository.UpsertAsync
  → SqliteContactRepository.UpsertManyAsync([contact])
  → SQLite transaction
```

The repository treats the supplied `Contact` as the complete desired aggregate and replaces that contact's owned child/link rows inside one transaction.

## Identity model in the full editor

Not every repeated record has the same ownership semantics.

### Contact-owned repeated rows

Phones, emails, addresses, and organizations are contact-owned rows. Their IDs remain stable through ordinary edits unless the row is intentionally removed/recreated.

### Shared group/tag dictionary rows

Groups and tags are global case-insensitive dictionary rows referenced through relationship tables. Their IDs are therefore **shared dictionary identities**, not contact-owned child identities.

`GroupDraftViewModel` and `TagDraftViewModel` retain `OriginalName` when a contact is loaded. Draft conversion follows these rules:

- unchanged or normalization-equivalent assignment → retain the existing shared dictionary ID and canonical stored name;
- true name change → assign a fresh dictionary identity, making the edit a per-contact reassignment;
- new assignment → use a new identity;
- duplicate names in one draft collapse case-insensitively;
- commas/semicolons remain exact because each assignment is an independent row.

This prevents a per-contact edit from reusing a global dictionary primary key for a different name or silently performing a global taxonomy rename. Old unreferenced dictionary rows may remain; cleanup/global rename semantics belong to a future explicit taxonomy-management feature.

## Other editor invariants

- root `Contact.Id` and `CreatedAt` survive ordinary edits;
- blank newly added rich rows are ignored;
- an existing label-only address remains preservable;
- removing one row/link does not remove unrelated rows;
- draft edits do not mutate the source aggregate before save;
- generated GUID alone is not proof that a draft is persisted.

`ContactDraftViewModel.IsPersisted` separates a new draft from a database-backed record so unsaved discard never becomes a database delete.

## Duplicate detection

```text
Find duplicates UI
  → load all contacts, including archived
  → DuplicateDetector.Find
  → candidate list
  → score/reasons/side-by-side review
  → user chooses survivor
  → confirmation
```

Detection is advisory. A score never automatically causes destructive persistence.

## Atomic and stale-safe duplicate merge

```text
confirmed survivor + secondary IDs
  → ContactService.MergeAsync
      ├─ reload both contacts
      ├─ ContactMerger.Merge
      ├─ normalize
      ├─ timestamp
      └─ validate
  → IContactRepository.MergeAsync
  → SqliteContactRepository.MergeAsync
      ├─ BEGIN
      ├─ require chosen survivor/primary still exists
      ├─ require secondary still exists
      ├─ upsert complete survivor aggregate
      ├─ delete secondary
      ├─ require exactly one secondary deletion
      └─ COMMIT
```

If either reviewed record disappeared, the operation is cancelled/rolled back. A removed chosen primary is never recreated from stale UI data, and a removed secondary cannot leave only the survivor update committed.

The chosen survivor determines the retained root identity. The merge engine combines documented unique data and gives fresh IDs to copied contact-owned child rows where needed.

## Bulk import

```text
Native file picker
  → bounded UTF-8 text read
  → CSV/vCard codec
  → ImportResult
  → ContactService.ImportAsync
      ├─ deep-copy
      ├─ normalize
      ├─ validate complete batch
      └─ shared UpdatedAt
  → UpsertManyAsync
  → one SQLite transaction
```

Validation happens before persistence, and a repository failure rolls the batch back. CSV/vCard are interchange formats, not full-fidelity backups.

## Backup and restore

Backup uses SQLite's backup API followed by integrity/schema/version/ContactCore-identity verification.

Restore follows:

```text
selected backup
  → read-only verify
  → verified pre-restore snapshot
  → staging copy
  → migrate staging
  → verify staging
  → clear pools/sidecars
  → replace active DB
  → verify active DB
      └─ failure: retain failed copy + restore recovery snapshot
```

See `storage-backup-recovery.md` for exact recovery behavior.

## Schema ownership

`DatabaseMigrator` is the schema authority. It tracks ordered migrations, rejects future schemas, and enforces the ContactCore schema-family marker introduced in schema version 2.

## SQLite connection boundary

`SqliteConnectionFactory` centralizes database path/access mode, pooling/cache behavior, foreign keys, busy timeout, and optional keyed-SQLite initialization. If a runtime key is requested, cipher support must be verifiable or the connection fails closed.

## Preferences boundary

`JsonAppPreferences` persists theme, reduced-motion, and permanent-delete-confirmation preferences. The database key is runtime-only, loaded even on first launch before a settings file exists, and deliberately excluded from serialized preferences.

## Platform-service boundary

`MainWindowViewModel` exposes narrow callbacks for focus, theme changes, import selection, export saving, backup selection, and confirmation. `MainWindow` wires/unwires them, keeping most native Avalonia APIs outside use-case logic.

## Error boundary

Desktop sanitizes workflow errors before display, but lower layers must still avoid embedding raw secrets/contact payloads in exception messages. Parser/validation warnings avoid unnecessarily echoing private invalid values.

## Security/data-safety principles

- local-first; no mandatory account/cloud/telemetry dependency;
- parameterized user/data SQL;
- literal `LIKE` wildcard escaping;
- fail-closed requested database encryption;
- runtime-only database key;
- verified backup/restore with recovery path;
- bounded desktop import and transactional batch persistence;
- confirmation-gated persisted delete/restore/duplicate merge;
- unsaved drafts never masquerade as persisted rows;
- complete aggregate editor preserves correct ownership/identity semantics;
- shared dictionary renames are reassignment, not accidental global mutation;
- duplicate merge is both atomic and stale-review-safe;
- CI and CodeQL independently verify the repository head.

## Test architecture

- `ContactCore.Domain.Tests` — validation/normalization/model behavior;
- `ContactCore.Application.Tests` — service, duplicate/merge policy, CSV/vCard behavior;
- `ContactCore.Infrastructure.Tests` — SQLite repository, shared dictionary reassignment, atomic stale-safe merge, preferences, paths, redaction, backup/restore;
- `ContactCore.Desktop.Tests` — non-visual draft behavior including contact-owned ID preservation and shared group/tag rename identity rules.

Cross-platform CI is configured for Windows, Ubuntu, and macOS; CodeQL is configured separately.

## Why a modular monolith

ContactCore has one local data store and no server boundary. Project-level modularity provides dependency direction, independent tests, framework isolation, and explicit contracts without the complexity/privacy surface of networked microservices.

See ADR `0001-modular-monolith.md`.

## Evolution rules

1. Put pure rules/types in Domain.
2. Put use-case orchestration/contracts in Application.
3. Put filesystem/SQLite/serialization/native concerns in Infrastructure.
4. Put Avalonia/platform presentation in Desktop.
5. Test behavior at the lowest useful layer and add integration regressions for data-safety boundaries.
6. Preserve complete aggregate data and correct contact-owned/shared-dictionary identity semantics.
7. Make destructive multi-row changes atomic and stale-safe.
8. Never convert heuristics into automatic destructive decisions.
9. Avoid cross-layer convenience leakage.
10. Add an ADR for durable storage/dependency/encryption/privacy architecture changes.
