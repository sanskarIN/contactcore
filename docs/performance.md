# Performance

ContactCore should remain responsive with ordinary personal/professional contact collections and should degrade predictably for large datasets.

## Initial budgets

These are engineering targets to measure on representative hardware, not claims of already-achieved benchmark results:

- cold application launch to interactive shell: target under 2 seconds on a typical SSD desktop;
- simple search response for 10,000 fictional contacts: target under 150 ms after database open;
- save/update of one contact aggregate: target under 100 ms in normal local conditions;
- UI interactions should avoid blocking the UI thread for disk/database work;
- search pages are capped at 1,000 results by the application/repository boundary.

## Current design choices

- Indexed contact name, favorite/archive, phone, email, and relationship lookup columns.
- Parameterized SQLite `LIKE`/`EXISTS` search.
- Short-lived connections and transactional aggregate writes.
- No mandatory network requests in normal workflows.
- Result limits to prevent accidental unbounded UI materialization.

## Known performance debt

Search currently reads matching IDs then materializes each aggregate with separate child queries. This is simple and correct but creates N+1-style database work for large pages. Before claiming high-scale performance, replace this with batched aggregate materialization (or a carefully measured equivalent) and compare query plans/benchmarks.

The initial desktop collection is also fully materialized rather than virtualized. Large-list UI virtualization must be verified with Avalonia once high-volume fixtures exist.

## Benchmark dataset

Performance tests must use deterministic fictional data generated in code. Suggested tiers:

- 1,000 contacts — small baseline;
- 10,000 contacts — normal stress target;
- 100,000 contacts — upper-bound investigation.

Mix names, multiple emails/phones, organizations, tags/groups, archive/favorite state, Unicode, and notes. Do not benchmark with real exported contact data.

## Measurement process

1. Build Release configuration.
2. Warm up the operation where appropriate.
3. Record OS, CPU, memory, storage, .NET version, database size, dataset count, and commit SHA.
4. Run enough iterations to report median and tail behavior rather than one best run.
5. Capture SQLite query plans for slow searches.
6. Profile allocations/CPU before optimizing.
7. Commit benchmark code and before/after results with the optimization.

## Optimization rules

- Measure first.
- Preserve correctness and transaction boundaries.
- Do not remove validation or security checks for speed.
- Make caching invalidation explicit.
- Prefer indexes supported by measured query plans.
- Avoid oversized in-memory caches containing sensitive contact content unless there is a demonstrated need.
