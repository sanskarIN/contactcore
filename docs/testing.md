# Testing

ContactCore uses MSTest with one test project per production layer. The suite is intended to prove domain rules, use-case orchestration, interchange behavior, SQLite consistency, backup/recovery safety, preferences/secrets behavior, and non-visual desktop draft semantics.

All four test projects are included in `ContactCore.slnx`.

## Test stack

Central versions live in `Directory.Packages.props`:

- `Microsoft.NET.Test.Sdk`;
- `MSTest`;
- `coverlet.collector`.

The repository targets .NET 10 and treats build warnings as errors.

## Run the full quality sequence

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
```

The CI workflow is configured to execute restore/format/build/test on Ubuntu, Windows, and macOS and to upload available test-result/coverage artifacts. CodeQL runs separately.

## Domain tests

Project: `tests/ContactCore.Domain.Tests`

`ContactValidationTests.cs` covers current validation/normalization behavior including:

- a representative valid contact;
- malformed email detection;
- non-echoing invalid email/phone validation messages;
- Unicode/accent search normalization;
- trimming/lowercasing search keys.

Useful future boundary additions include exact name/note length limits, phone length edges, combining-mark cases, `PhoneKey` punctuation/country-code behavior, display-name fallback, and deeper `DeepCopy` independence cases.

## Application tests

Project: `tests/ContactCore.Application.Tests`

### `ContactServiceTests.cs`

Current coverage includes:

- save normalization for scalar values, phone label/number, and email label/address;
- timestamp refresh on save;
- whole-batch import validation before any repository bulk write;
- imported validation-field indexing such as `Contact[2].Email`;
- deep-copy import normalization so source objects are not mutated;
- one normalized bulk call for a valid batch;
- one shared import update timestamp;
- search-text trimming while all other query filters are preserved.

The production service additionally normalizes addresses, organizations, groups, and tags. Focused unit assertions for every one of those richer normalization branches remain useful future coverage.

### `DuplicateDetectorTests.cs`

Current coverage includes:

- high-confidence matching from normalized signals;
- normalized-phone duplicate suppression during merge;
- fresh IDs for phone/email/address/organization children copied from a secondary contact;
- self-merge rejection.

The repository/application integration path is additionally protected by `SqliteMergeTests.cs` in Infrastructure.

Future additions could exercise threshold clamping, reason ordering, no-signal pairs, every note-combination branch, and more group/tag/address/organization equivalence cases.

### `ImportExportTests.cs`

Current coverage includes:

- CSV round trip with commas, quotes, and embedded newlines;
- vCard round trip for supported name/birthday/note/phone fields;
- seeded randomized Unicode CSV inputs to check ordinary arbitrary/malformed text does not crash the parser.

The randomized test is deterministic robustness coverage, not a full fuzzing campaign.

### `ImportExportHardeningTests.cs`

Hardening coverage includes:

- CSV files with no recognized ContactCore headers import zero contacts;
- duplicate CSV headers use the first occurrence and warn;
- formula-like CSV text is preserved and produces a spreadsheet-safety warning;
- escaped vCard name delimiters, backslashes, and newlines round-trip across supported fields;
- common vCard `TYPE` parameters map to `ContactFieldKind`;
- invalid vCard birthday warnings do not echo the invalid imported value.

Future parser work should continue to add a minimal regression for each newly supported syntax/edge case.

## Infrastructure tests

Project: `tests/ContactCore.Infrastructure.Tests`

All storage tests use unique temporary directories/databases rather than the user's normal ContactCore path.

### `SqliteRepositoryTests.cs`

Coverage includes:

- root/phone/email/tag aggregate round trip;
- full rich aggregate round trip for phones, emails, address, organization, group, and tag;
- complete-aggregate replacement removes stale child/link rows while keeping supplied rows;
- favorite search;
- literal `%`, `_`, and backslash search behavior;
- tag/group case-insensitive filters;
- family-name-first `StartsWith` behavior;
- permanent delete with cascade cleanup;
- `UpsertManyAsync` rollback when a later contact fails, proving an earlier successful prefix is not committed.

### `SqliteMergeTests.cs`

Coverage includes:

- survivor aggregate update plus secondary deletion as one successful operation;
- rollback of the attempted survivor update when the requested secondary contact no longer exists.

This is the critical consistency regression for destructive duplicate merge.

### `BackupServiceTests.cs`

Coverage includes:

- restore to a verified snapshot;
- retained verified pre-restore snapshot containing the prior active state;
- missing backup rejection before active data changes;
- active database rejected as its own restore source;
- invalid non-SQLite backup rejection without replacement;
- valid but unrelated SQLite database rejection;
- tampered ContactCore schema-family identity rejection;
- legacy schema backup migration before replacement;
- future schema rejection without active-data replacement;
- unique consecutive backup filenames.

High-value remaining failure injection includes a forced **post-switch** verification failure to prove the rollback artifact path end-to-end, plus staging/temp cleanup failures at every individual stage.

### `JsonAppPreferencesTests.cs`

Coverage includes runtime key non-persistence, first-run/runtime-key behavior, malformed JSON safe defaults, and theme normalization/persistence behavior represented by the current suite.

The key invariant is that normal `settings.json` must never serialize `DatabaseKey` or its secret value.

### `AppPathsTests.cs`

Covers configured environment/data-path behavior and path derivation/fallback semantics using temporary/controlled values.

### `RedactingLogTests.cs`

Covers likely email/long-number shape sanitization and maximum diagnostic-message length behavior.

## Desktop tests

Project: `tests/ContactCore.Desktop.Tests`

`ContactDraftViewModelTests.cs` covers:

- contact ID and creation timestamp preservation;
- Favorite/Archived preservation;
- explicit persisted versus unsaved draft state;
- exact `yyyy-MM-dd` birthday requirement;
- editing phone/email values while preserving child IDs;
- address/organization editing while preserving child IDs;
- group/tag editing and ID preservation;
- case-insensitive duplicate group suppression while retaining the first identity;
- exact group/tag names containing commas and semicolons;
- removal of a selected repeated row without removing unrelated repeated rows;
- preservation of a legacy address that contains only a label;
- suppression of blank newly added phone/email/address/organization/group/tag rows;
- source aggregate non-mutation while editing the draft.

These are intentionally non-visual view-model tests.

## Important unautomated desktop paths

The following remain valuable candidates for future view-model/Avalonia integration coverage:

- debounce/cancellation races under rapid search input;
- All/Favorites/Archived command orchestration;
- Settings save/theme callback behavior;
- confirmation-required permanent deletion;
- duplicate merge confirmation/cancellation in the desktop command layer;
- restore confirmation + refresh behavior;
- picker cancellation and temporary picker-file cleanup;
- export status behavior and archived-contact inclusion;
- keyboard/focus behavior inside a running Avalonia window;
- repeated-row reorder behavior if reorder controls are added.

## Temporary-data hygiene

Infrastructure tests should:

1. create unique paths under `Path.GetTempPath()`;
2. use fictional data only;
3. clear SQLite pools when necessary before cleanup;
4. delete temporary directories in `TestCleanup` while tolerating reasonable OS file-lock timing differences.

Never point tests at the default ContactCore user data directory.

## Test naming

Prefer behavior-oriented names such as:

`Merge_rolls_back_primary_update_when_secondary_disappeared`

A good name communicates the precondition/action/observable contract rather than a private implementation detail.

## Determinism

Tests should not depend on:

- local time zone;
- user profile contact data;
- network access;
- machine-specific paths;
- execution order;
- real contact-provider services;
- changing random seeds without recording them.

When randomness adds parser robustness, use a fixed seed in normal CI and reserve broader fuzz campaigns for dedicated tooling/jobs.

## Coverage policy

CI collects XPlat Code Coverage, but this project intentionally avoids promising a fixed percentage in README/docs. A percentage alone can reward shallow tests and quickly becomes stale.

Prioritize branches where a regression could cause data loss, partial writes, unsafe restore, malformed-input crashes, secret leakage, or destructive UI surprises.

## Manual verification matrix

Automated tests do not replace release checks for:

- first launch on each supported OS;
- create/edit every repeated field;
- repeated-field add/remove behavior;
- search/favorite/archive/A–Z navigation;
- duplicate pair review and both survivor choices using fictional contacts;
- permanent-delete confirmation;
- keyboard shortcuts and visible focus;
- theme switching;
- CSV/vCard native picker flows;
- verified backup and real restore using fictional data;
- minimum-window/high-DPI/text scaling;
- screen reader/assistive technology appropriate to any release claim;
- each published release RID, including macOS x64/arm64 where artifacts are produced.

Document exactly what was manually verified. Do not replace evidence with a blanket phrase such as “tested on all platforms.”

## CI-only failure checklist

Compare:

- OS-specific path/file-lock behavior;
- filesystem case sensitivity;
- newline behavior;
- Avalonia/native dependencies;
- SQLite native library behavior;
- SDK resolution via `global.json`;
- parallel execution/shared static state;
- generated MVVM source behavior;
- XAML parser/runtime binding behavior.

Do not weaken a cross-platform assertion until the product behavior itself has been understood.

## Regression-test workflow

For a bug fix:

1. create or identify a test that represents the old bad behavior;
2. keep fixtures minimal and fictional;
3. implement the fix at the correct layer;
4. run the focused test;
5. run the full solution quality sequence;
6. update user/developer documentation when behavior changed;
7. keep the regression permanently unless there is a documented reason to replace it with stronger coverage.
