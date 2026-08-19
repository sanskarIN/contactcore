# Testing

ContactCore uses MSTest with one test project per production layer. Tests are intended to prove domain rules, interchange behavior, SQLite data integrity, backup/recovery safety, preference secrecy/defaults, and non-visual desktop draft behavior.

The suite is part of `ContactCore.slnx`, so normal solution-level test commands include all four test projects.

## Test stack

Central package versions are defined in `Directory.Packages.props`:

- `Microsoft.NET.Test.Sdk`
- `MSTest`
- `coverlet.collector`

The repository currently targets .NET 10 and treats build warnings as errors.

## Run everything

```bash
dotnet test ContactCore.slnx -c Release
```

For the full CI-equivalent sequence:

```bash
dotnet restore ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes --no-restore
dotnet build ContactCore.slnx -c Release --no-restore
dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
```

CI executes this strategy on Ubuntu, Windows, and macOS and uploads the available `TestResults` artifacts for 14 days.

## Domain tests

Project: `tests/ContactCore.Domain.Tests`

Current `ContactValidationTests.cs` covers:

- a normal valid contact with email and phone produces no validation issues;
- malformed email is reported against the `Email` field;
- validation messages do not echo supplied invalid email/phone values;
- Unicode search normalization removes accents (`Élodie` → `elodie`);
- search normalization trims and lowercases text (`  HELLO  ` → `hello`).

### Recommended additions

- boundary tests for 120-character names and 20,000-character notes;
- just-over-limit cases;
- phone minimum/maximum length boundaries;
- representative Unicode combining-mark/normalization cases;
- `PhoneKey` punctuation/country-code cases;
- `DisplayName` fallback behavior;
- `DeepCopy` collection independence.

## Application tests

Project: `tests/ContactCore.Application.Tests`

### `DuplicateDetectorTests.cs`

Current coverage includes:

- shared normalized name + case-insensitive shared email gives a high-confidence score;
- normalized phone equivalence prevents duplicate phone values during merge;
- child phone/email/address/organization copied from a secondary contact receive fresh IDs;
- self-merge is rejected.

Recommended additions include threshold clamping, reason ordering, no-signal pairs, note merge combinations, group/tag equivalence, and address/organization normalization edge cases.

### `ImportExportTests.cs`

Current coverage includes:

- CSV round trip with comma, quotes, and embedded newline;
- vCard round trip for primary name fields, birthday, note, and phone;
- 250 seeded randomized Unicode CSV inputs to ensure ordinary arbitrary text does not make the parser throw.

This randomized test is a deterministic robustness check, not a complete fuzzing campaign.

Recommended additions:

- malformed/unclosed CSV quotes;
- CR-only/mixed line endings;
- duplicate/unknown headers;
- empty header/file behavior;
- birthday warnings;
- vCard folding/unfolding cases;
- escape sequences and multiple cards;
- unterminated vCard warning;
- larger bounded inputs;
- spreadsheet-formula export policy tests if mitigation is implemented.

### ContactService import/save coverage

The hardening branch relies on service behavior for trimming, whole-batch validation, field-index prefixes, and normalized timestamps. Add focused service tests when this behavior changes so it is not proven only indirectly through repository tests/UI paths.

## Infrastructure tests

Project: `tests/ContactCore.Infrastructure.Tests`

Infrastructure tests create isolated temporary paths/databases rather than touching a normal user profile.

### `SqliteRepositoryTests.cs`

Current coverage includes:

- aggregate round-trip for root contact plus phone/email/tag;
- search of a favorite contact;
- permanent delete plus foreign-key cascade behavior;
- `UpsertManyAsync` transaction rollback when a later contact fails because of a duplicate child primary key.

The rollback test specifically asserts that an earlier successful prefix does **not** remain committed.

Recommended additions:

- archived/favorites combinations;
- literal `%`, `_`, and backslash search escaping;
- tag/group filters;
- StartsWith behavior;
- address/organization/group round trip;
- update replacing stale child collections;
- migration from every released schema version;
- future-schema rejection during normal initialization.

### `BackupServiceTests.cs`

Current coverage includes:

- restore returns the database to a previously verified snapshot;
- invalid non-SQLite backup is rejected without replacing the active database;
- schema-version-1 backup is migrated before replacement;
- future-schema backup is rejected while active data remains intact;
- consecutive backups receive unique paths and both files exist.

Recommended additions:

- valid non-ContactCore SQLite rejection;
- corrupt SQLite that opens but fails integrity check;
- schema identity-marker tampering;
- forced final-verification failure and verified rollback artifact behavior;
- pre-restore snapshot existence/contents;
- staging temporary cleanup after every failure stage;
- same-path restore rejection;
- missing-file restore rejection;
- encrypted-provider integration when a supported provider is available in a dedicated test environment.

### `JsonAppPreferencesTests.cs`

Current coverage includes:

- `DatabaseKey` and its value are not written into `settings.json`;
- corrupted JSON returns safe defaults (`System`, reduced motion off, delete confirmation on);
- unknown themes normalize to `System` across save/reload.

Recommended additions:

- persistence of `ConfirmPermanentDelete` false/true;
- persistence of all valid theme options;
- existing older JSON missing newly introduced fields;
- temporary-file cleanup on write failures where practical.

## Desktop tests

Project: `tests/ContactCore.Desktop.Tests`

`ContactDraftViewModelTests.cs` currently covers:

- contact ID and creation timestamp survive draft round trip;
- Favorite and Archived flags survive draft round trip;
- non-ISO birthday input is rejected;
- editing the compact first phone/email preserves their existing IDs, labels, and field kinds;
- additional phone/email values survive the compact edit;
- addresses, organizations, groups, and tags survive the compact edit even though the current editor does not expose them;
- draft edits do not mutate the source aggregate supplied to `Load`;
- clearing the visible primary phone/email removes only that first value and preserves remaining values.

These are intentionally non-visual tests.

### Rich editor preservation guarantee

The desktop editor still exposes only one phone/email and does not directly edit addresses, organizations, groups, tags, or additional phone/email entries. However, the draft now retains a deep copy of the complete loaded aggregate and regression tests explicitly protect those unexposed values from being lost during ordinary edit/save conversion.

Treat this preservation behavior as a correctness invariant. As the richer editor is implemented, extend the tests to cover adding, editing, removing, and reordering every exposed collection without regressing preservation of untouched values.

Recommended desktop/view-model tests:

- new-contact defaults and explicit unsaved-draft behavior;
- search debounce/cancellation races;
- All/Favorites/Archived filter orchestration;
- settings load/save and theme callback;
- confirmation-required delete path;
- restore confirmation and refresh behavior;
- picker cancellation;
- import warning/status behavior;
- export includes archived records;
- temporary backup-picker copy cleanup;
- full multi-value editor behavior once those controls are added.

Avalonia integration tests may be added for focus/keyboard behavior where reliable; manual accessibility/platform testing is still required.

## Temporary-data hygiene

Infrastructure tests should:

1. create unique paths under `Path.GetTempPath()`;
2. use fictional data only;
3. clear SQLite pools when necessary before deleting database files;
4. clean directories in `TestCleanup` while tolerating reasonable OS file-lock timing differences.

Never point tests at the default ContactCore data directory.

## Test naming

Prefer behavior-oriented names such as:

`Bulk_upsert_rolls_back_every_contact_when_one_write_fails`

A good test name states the precondition/action/observable contract rather than the private method being exercised.

## Determinism

Tests should not depend on:

- local time zone;
- user profile data;
- network access;
- machine-specific paths;
- execution order;
- real external contact clients;
- random seeds that change on each run without recording the seed.

When randomness adds robustness, use a fixed seed for normal CI and reserve broader fuzz campaigns for dedicated jobs/tooling.

## Coverage policy

CI collects XPlat Code Coverage, but the README deliberately does not claim a fixed percentage. A percentage without context can reward shallow tests and becomes stale quickly.

Review coverage as one signal. Prioritize meaningful branch/failure-path tests around data loss, migration, restore, validation, input parsing, and destructive UI operations.

## Manual verification matrix

Automated tests do not replace these release checks:

- first launch on each supported OS;
- create/edit/search/favorite/archive/delete;
- keyboard shortcuts and focus visibility;
- theme switching;
- CSV/vCard native file pickers;
- backup creation and real restore using fictional data;
- macOS Intel/Apple Silicon release artifact smoke tests when available;
- Linux desktop startup;
- high-DPI/text scaling;
- assistive-technology checks appropriate to release claims.

Document exactly what was manually tested in release notes/handoff rather than saying “tested on all platforms” without evidence.

## When a test fails in CI only

Compare:

- OS-specific path and file-lock behavior;
- case sensitivity;
- newline behavior;
- Avalonia/native picker dependencies;
- SQLite native library behavior;
- SDK version selected by `global.json`;
- parallel execution/shared static state.

Do not weaken a cross-platform assertion before determining whether the product itself has a platform bug.

## Adding a regression test

For a bug fix:

1. create a test that fails for the old behavior;
2. keep the reproduction minimal and fictional;
3. implement the fix at the correct layer;
4. run the focused test;
5. run the full solution tests;
6. update relevant documentation/changelog when user-visible.
