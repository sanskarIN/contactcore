# Development

This guide explains how to change ContactCore without weakening its local-first, data-integrity, destructive-operation, privacy, and testability guarantees.

## Before editing

Read the relevant guides first:

- `architecture.md` — dependency/layer ownership;
- `data-model.md` — aggregate and relational semantics;
- `desktop-ui.md` — editor, duplicate review, platform callbacks, shortcuts;
- `import-export.md` — interchange/parser boundaries;
- `storage-backup-recovery.md` — SQLite, migration, backup, restore safety;
- `security.md` — input, secrets, encryption-key behavior;
- `testing.md` — current regression map;
- `../CONTRIBUTING.md` — repository contribution rules.

## Solution structure

Production projects:

- `ContactCore.Domain`
- `ContactCore.Application`
- `ContactCore.Infrastructure`
- `ContactCore.Desktop`

One test project mirrors each layer. `ContactCore.slnx` contains all eight projects.

## Shared compiler/build policy

`Directory.Build.props` applies common policy such as `net10.0`, latest C#, nullable/implicit usings, warnings-as-errors, analyzer level, deterministic builds, and CI metadata.

Do not disable warnings globally to make a branch pass. Fix the warning or use the narrowest justified suppression with review context.

## Central package management

`Directory.Packages.props` owns package versions. Avoid duplicating versions in individual project files unless NuGet/MSBuild semantics specifically require it.

When adding a dependency, justify why BCL/current project code is insufficient and review privacy, native distribution, licensing, and supply-chain impact.

## Layer placement

### Domain

Pure contact concepts, validation, normalization, deep-copy/model behavior.

### Application

Use cases/contracts, import/export transformation, duplicate scoring/merge policy, orchestration around domain behavior.

### Infrastructure

SQLite/filesystem/serialization/native-provider behavior and Application abstraction implementations.

### Desktop

Avalonia presentation, window/keyboard behavior, file pickers/dialogs, theme application, and presentation adapters.

Keep lower-layer rules out of Desktop even when UI is initially their only consumer.

## Contact aggregate changes

`SqliteContactRepository` treats a supplied `Contact` as the complete desired aggregate. Saving replaces that contact's owned child/link rows with the collection state supplied in the same transaction.

When adding/changing a contact field:

1. update Domain;
2. update validation/normalization;
3. append a migration if persistence changes;
4. update repository read/write mapping;
5. update `DeepCopy` and duplicate merge behavior;
6. update Application service semantics;
7. update interchange codecs only where intentionally supported;
8. update the desktop editor while preserving all aggregate data/identities;
9. add regression tests;
10. synchronize documentation.

## Full editor rules

The current desktop editor represents all repeated collections in the current model:

- phones;
- emails;
- addresses;
- organizations;
- groups;
- tags.

Development must preserve these invariants:

- unchanged repeated rows retain their IDs;
- an edited repeated row normally retains its ID;
- removing one row removes only that intended value/link;
- blank newly added rows are ignored rather than persisted;
- an existing label-only address remains representable;
- group/tag names are independent rows, so commas/semicolons remain exact;
- case-insensitive duplicate group/tag names collapse before persistence while retaining the first row identity;
- root `Id`/`CreatedAt` survive editing;
- draft changes do not mutate the source aggregate before save.

If adding reorder behavior, first decide whether order is a real persisted domain property. Do not promise durable ordering using only visual collection order.

## Unsaved versus persisted drafts

A new `Contact` has a generated GUID before first persistence. Use explicit persistence state, not `Guid.Empty`, to distinguish a new draft from a database row.

Current Desktop behavior uses `ContactDraftViewModel.IsPersisted`: Delete/discard on an unsaved draft closes it without a repository delete or permanent-delete confirmation.

## Duplicate merge development

Duplicate detection is advisory. Never automatically merge/delete because a score crosses a threshold.

Current destructive workflow is:

```text
candidate scan
→ user reviews score/reasons and both records
→ user chooses survivor
→ confirmation
→ ContactService.MergeAsync
→ repository transaction: upsert survivor + delete secondary
```

`MergeAsync` must remain all-or-nothing. If the secondary row disappears before commit, the repository throws and rolls back the survivor update.

When modifying merge semantics, test self-merge, scalar fill behavior, notes, favorite/archive decisions, duplicate child suppression, copied child IDs, group/tag equivalence, and missing-secondary rollback.

## Async and cancellation

Use cancellation-aware async I/O and pass tokens through layer boundaries where practical.

Desktop search already cancels superseded debounce/search operations. Avoid uncontrolled fire-and-forget I/O that can update stale UI state after a newer user action.

## SQL rules

- Parameterize user/data values.
- Escape `LIKE` metacharacters when user text is intended literally.
- Never derive SQL table/column identifiers from untrusted input.
- Keep foreign keys enabled.
- Keep aggregate, bulk-import, and duplicate-merge writes transactional.
- Add indexes only for measured query patterns.

## Migration rules

Append new numbered migrations; never rewrite released migration meaning. Add upgrade tests. Reject future-schema databases rather than improvising downgrade behavior.

Any `schema_family` contract change requires backup/restore design review and an ADR.

## Preferences and secrets

Persist only non-secret preferences in `settings.json`. `DatabaseKey` is runtime-only and must remain absent from serialized settings.

New preference fields require safe defaults and tolerant loading from older JSON. Keep temp-file/replacement writes unless a stronger atomic strategy replaces them.

## Import parser rules

Treat CSV/vCard as hostile or malformed input even though the application is offline-first.

Requirements:

- bounded resource use at an appropriate entry point;
- predictable malformed-input behavior;
- parser warnings separated from domain validation;
- no persistence inside codecs;
- complete-batch validation before write;
- one-transaction batch persistence;
- escaping/Unicode/malformed/privacy tests.

Current desktop text limit is 5,000,000 characters.

CSV-specific current behavior includes rejection-with-warning when no supported header exists, first-occurrence handling for duplicate headers, and warnings—not mutation—for spreadsheet formula-like prefixes.

Focused vCard current behavior includes supported escaping/unfolding/common TYPE mapping and non-echoing invalid birthday warnings. Do not broaden compatibility claims without tests.

## Backup and restore development

Use disposable `CONTACTCORE_DATA_PATH` values and fictional data.

Preserve source verification, pre-restore snapshot, staging migration/verification, switch, final verification, and rollback behavior unless a reviewed design provides equivalent or stronger safety.

## Error messages and privacy

Do not put raw contact payloads, database keys, or arbitrary untrusted input into exception messages. Desktop redaction is defense-in-depth, not a complete PII classifier.

Validation/parser messages should identify the problem without unnecessarily echoing private values.

## UI development

Preserve keyboard reachability, visible focus, labels, dynamic theme resources, logical traversal, text wrapping/scaling, reduced-motion behavior for custom animation, and destructive confirmation guarantees.

The full rich editor has many repeated-row controls; manually check focus order/add-remove behavior at representative scaling. The duplicate pane has two explicitly different survivor actions; labels and confirmation text must continue to match the actual merge direction.

If adding a platform service, prefer a narrow callback/interface rather than spreading picker/dialog APIs across business logic.

`Ctrl+S` is intentionally restricted to an active contact editor. Do not make application-wide save shortcuts invoke stale contact state from Settings/Data Tools/Duplicate Review.

## Formatting and quality

Verify formatting:

```bash
dotnet format ContactCore.slnx --verify-no-changes
```

Apply if needed:

```bash
dotnet format ContactCore.slnx
```

Full quality sequence:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

CI is configured for Ubuntu, Windows, and macOS. Verify the final commit, not an earlier green head.

## Test placement

Prefer the lowest meaningful layer:

- validation/normalization → Domain;
- services/codecs/duplicate policy → Application;
- migration/repository/preferences/backup → Infrastructure;
- draft/desktop orchestration → Desktop.

Cross-layer data-safety behavior often deserves focused unit tests plus an integration regression.

## Test data

Use fictional names, addresses, phone numbers, domains, databases, exports, and screenshots. Never copy real user data into fixtures/log output.

Infrastructure tests use isolated temporary directories and should clean them best-effort after disposing/clearing SQLite resources.

## Git discipline

Keep commits focused. Avoid mixing broad formatting, unrelated dependencies, feature logic, and unrelated docs cleanup.

Before commit in a local clone:

```bash
git status
git diff --check
git diff
```

Do not commit build output, secrets, contact databases, WAL/SHM files, backups, exports, recovery copies, temporary restore files, or real `.env` content.

## Pull requests

A useful PR explains user/developer problem, architecture choice, migration/data-safety impact, privacy/security impact, tests, manual verification, and remaining limitations.

Do not merge until the exact final head's required CI/CodeQL checks are understood.

## Documentation definition of done

For behavior/file changes, update the relevant guides in the same PR. `repository-reference.md` must remain exhaustive whenever tracked files are added/removed/renamed.

`what_changed.md` is the continuation/handoff record and must state the exact branch/PR, meaningful changes, verification status, and remaining work without presenting queued/pending checks as passed.
