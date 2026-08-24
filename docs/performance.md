# Performance

ContactCore prioritizes correctness and local data safety over unsupported performance claims. The current implementation should be evaluated with realistic contact sets before assigning scale numbers.

## Current performance model

### Local SQLite

All normal contact data is local SQLite, avoiding network latency. Connections use a 5-second busy timeout and shared cache. The active database normally uses connection pooling.

### Search

Free-text search issues a SQLite query that checks:

- given name;
- family name;
- nickname;
- matching phone rows through `EXISTS`;
- matching email rows through `EXISTS`.

Additional filters can check favorites, archived inclusion, tag, group, and starting letter.

The schema contains indexes on:

- `(family_name, given_name)`;
- `(is_archived, is_favorite)`;
- phone number;
- case-insensitive email address.

The current free-text search uses leading-wildcard patterns (`%text%`), so ordinary B-tree indexes may not accelerate every name/phone/email search shape. Do not infer constant-time or full-text-search performance from the presence of indexes.

### Debounced UI search

The desktop waits 180 ms after search text changes before refreshing. A newer input cancels the previous debounce/search token. This reduces unnecessary queries while typing and helps prevent obsolete queries from continuing.

### Aggregate loading

`SqliteContactRepository` first materializes matching root contacts and then calls `LoadChildrenAsync` once per contact. Each contact's children are loaded using separate sequential queries for phones, emails, addresses, organizations, groups, and tags.

This is straightforward and correct, but it creates an N×child-query pattern for large result sets. It should be benchmarked before claiming support for very large address books.

### In-memory list rendering

The view model clears and repopulates an `ObservableCollection<ContactListItemViewModel>` for the full query result. The current `ListBox`/layout should be profiled for virtualization and memory behavior on representative Avalonia platforms rather than assuming ideal virtualization.

### Duplicate detection

`DuplicateDetector.Find` compares contact pairs using nested loops. For `n` contacts the comparison count grows approximately as `n(n-1)/2`—quadratic growth.

This is acceptable for modest data sets but can become expensive for large ones. A future optimization can bucket candidates by normalized email/phone/name keys before scoring, with tests proving candidate quality is not lost.

### Import

Desktop import reads the complete selected text into memory with a maximum of 5,000,000 characters, then the codec parses to contact objects, the service deep-copies/normalizes the collection, and the repository writes it in one transaction.

The bounded input size prevents unbounded desktop text ingestion, but the current pipeline is not streaming. Large permitted files can temporarily exist as multiple in-memory representations.

### Export

Export loads all contacts including archived contacts, materializes full aggregates, encodes the entire CSV/vCard output into a string, then writes it to the selected stream. This is also not streaming.

### Backup

Backup uses SQLite's native `BackupDatabase` API and performs a full integrity/identity verification afterwards. Backup time is therefore related to database size and storage performance; integrity checking intentionally adds work for safety.

### Restore

Restore performs multiple full-file/database stages—read-only verification, pre-restore snapshot, staging copy, migration, staged verification, switch, and final verification. This is deliberately more expensive than a raw file replacement because data-loss protection is the priority.

## What is not currently claimed

The repository does not claim:

- a maximum supported contact count;
- sub-millisecond search;
- constant-time duplicate detection;
- streaming import/export;
- zero-copy backup/restore;
- a fixed startup time;
- a fixed memory ceiling;
- high-scale benchmark results.

Any such numbers should come from a committed/reproducible benchmark methodology and specific hardware/software context.

## Benchmark scenarios

A useful performance suite should generate **fictional** data and measure at least these scales:

- 100 contacts;
- 1,000 contacts;
- 10,000 contacts;
- larger sets only after the smaller tests identify bottlenecks.

Data should include realistic variation in multiple emails/phones, notes, tags/groups, and archived/favorite states.

### Startup

Measure:

- empty database first migration;
- already-migrated database;
- database with 1k/10k contacts;
- time to first usable window/list.

### Search

Measure:

- prefix-like common name;
- substring near end of names;
- phone search;
- email search;
- no-result search;
- favorites/archived filters;
- repeated typing with debounce/cancellation.

Record database size, result count, OS, CPU, storage, .NET SDK/runtime, and commit.

### Contact load

Profile SQL statement count and elapsed time for 10, 100, 1,000 returned contacts. The current child-loading design is likely to show increasing query counts; this provides a baseline before join/batching changes.

### Save/import

Measure:

- single contact with no children;
- rich contact with several child rows;
- 100/1,000-contact bulk import;
- rollback cost for a failure near the end of a large batch.

### Duplicate detection

Measure candidate scan for 100/1k/5k contacts. Record comparison count and elapsed time. If optimizing, compare both runtime and detected-pair equivalence/quality.

### Backup/restore

Measure databases at several sizes and report:

- backup API duration;
- integrity verification duration;
- snapshot/staging/migration/final verification duration for restore;
- resulting artifact sizes.

Do not remove safety checks merely because they dominate a benchmark; optimize them only with equivalent assurance.

## Optimization priorities

### 1. Avoid data-loss regressions

Performance work must not weaken transactions, backup verification, staged restore, or schema identity.

### 2. Reduce child-query amplification

Potential approaches include batched child queries using contact ID sets, carefully joined projections, or a two-phase root + bulk-children mapper. Any change must preserve ordering/identity and avoid Cartesian product bugs across multiple repeated collections.

### 3. Introduce pagination/virtualized loading

For large contact lists, add repository pagination/cursors and UI incremental loading rather than materializing every contact and every child field for a list that displays only name/subtitle/favorite.

A useful architectural split could load lightweight list projections and fetch the full aggregate on selection.

### 4. Full-text search

If substring search becomes a bottleneck, evaluate SQLite FTS5. This requires an ADR covering tokenizer/Unicode behavior, schema migration, index synchronization, privacy, query escaping, and test strategy.

### 5. Duplicate candidate blocking

Bucket by normalized phone/email and selected name keys before pairwise scoring. Keep a fallback/quality strategy so candidates with only fuzzy name similarities are not silently lost if product requirements need them.

### 6. Streaming interchange

For larger imports/exports, move codecs toward streaming readers/writers while preserving whole-batch validation semantics or redesigning atomic import using a database transaction with validation checkpoints.

### 7. Avoid UI-thread blocking

SQLite/file work should remain async from the presentation perspective. Be careful that CPU-heavy encoding/duplicate detection can still occupy the UI thread even when surrounding methods are async; profile before moving work to a scheduler/thread pool.

## Profiling guidance

Use tooling appropriate to .NET/Avalonia such as `dotnet-trace`, `dotnet-counters`, IDE profilers, and SQLite query-plan inspection (`EXPLAIN QUERY PLAN`) in development-only contexts.

Do not commit profiles containing real contact values or real database paths without sanitization.

## Performance regression review

A change touching search, list loading, duplicate detection, imports, or SQLite mapping should answer:

- Did SQL statement count change?
- Did full aggregate materialization increase?
- Did allocation behavior change?
- Did cancellation remain effective?
- Does a new index improve the actual query plan?
- Did transaction scope become much longer?
- Were backup/restore safety steps preserved?
- Are results identical and deterministic?

## Documentation rule

If a release advertises a performance number, record the benchmark setup, generated-data characteristics, tested commit, hardware/OS, and percentile/statistic used. Avoid unqualified phrases such as “instant for millions of contacts” without evidence.
