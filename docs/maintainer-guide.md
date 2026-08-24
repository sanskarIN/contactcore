# Maintainer Guide

This guide captures ContactCore's maintainer-level invariants and release expectations. It is intentionally stricter than a quick-start guide because changes in persistence, duplicate handling, backup/restore, mobile/browser composition, or release automation can affect user data even when the UI appears to work.

## Core maintenance principles

1. Preserve local-first behavior. Do not introduce mandatory account, cloud sync, telemetry, analytics, or remote contact upload while keeping the existing privacy claims unchanged.
2. Treat contact data as private. Tests, screenshots, bug reports, logs, and release evidence should use fictional/disposable data.
3. Preserve complete aggregates. A save must not silently drop rich repeated fields that a caller did not intentionally remove.
4. Preserve identity semantics. Root contact IDs and surviving contact-owned child IDs should remain stable through ordinary edits; shared group/tag identities follow their documented reassignment rules.
5. Prefer transactional/data-safe failure over partial success for imports, duplicate merge, and native restore.
6. Keep warnings-as-errors/analyzers meaningful. Fix actionable code rather than broadly suppressing diagnostics.
7. Keep exact-final-head CI and CodeQL as the merge gate. Older or cancelled runs are evidence for diagnosis, not approval of a newer commit.
8. Do not fabricate signing/provisioning success. Public source build gates and production distribution are separate claims.

## Solutions and project boundaries

Use `ContactCore.Core.slnx` for workload-free core verification and CodeQL. It contains Domain, Application, Infrastructure, shared UI, native composition, Desktop, and all five behavioral test projects.

Use `ContactCore.slnx` when working with the complete cross-platform tree, including Android, iOS/iPadOS, and Browser/WebAssembly heads.

Keep dependencies pointed inward:

```text
Domain
  ↑
Application
  ↑
Infrastructure / UI adapters
  ↑
Desktop / Native / Browser / mobile heads
```

Browser remains intentionally separate from native SQLite Infrastructure. Do not add a native SQLite dependency to the Browser target merely to reuse implementation details.

## Domain and rich-field maintenance

When adding or changing a contact field:

- update the Domain model and `DeepCopy` behavior;
- update validation and normalization where applicable;
- update native persistence schema/mapping if persisted;
- update browser persistence DTO/source-generation metadata if the Browser stores it;
- update CSV/vCard only if the interchange contract intentionally supports the field;
- update both mature Desktop and portable shared UI editing paths where applicable;
- preserve existing IDs for surviving contact-owned rows;
- update tests before claiming full editor support;
- document fidelity/import-export limitations explicitly.

Repeated fields currently support add/edit/remove but not drag/drop reorder. Do not document reorder as implemented until the actual interaction and persistence semantics exist.

## Group and tag identity rules

Groups and tags are shared dictionary entities rather than contact-owned rows.

Maintain these rules:

- unchanged assignment keeps its shared identity;
- case-only/normalization-equivalent edits keep the existing canonical identity/name;
- a true per-contact rename means reassignment to another/new shared identity rather than mutating a shared row globally;
- names containing delimiters must remain exact because the editor uses independent rows rather than comma/semicolon tokenization;
- ordinary relationship removal must not silently claim global taxonomy cleanup.

A dedicated global group/tag taxonomy management workflow remains future work. Define rename/delete/orphan semantics explicitly before implementing it.

## Native SQLite changes

For any schema change:

1. add a numbered forward migration;
2. keep schema-family identity metadata intact;
3. update expected schema version tests;
4. verify old supported databases migrate successfully;
5. verify newer-than-supported schema versions fail safely;
6. review backup/restore compatibility;
7. update data model/storage/maintenance documentation.

Do not edit an already-shipped migration in a way that changes the meaning of an existing schema version.

`SqliteContactRepository` aggregate writes should remain transactional. Shared group/tag linking, child replacement, duplicate merge, and bulk import must not leave partial state after failure.

## Native connection/encryption boundary

`CONTACTCORE_DATABASE_KEY` is runtime-only and must not be serialized into normal settings.

If a database key is requested, the connection factory must continue to fail closed unless a compatible cipher provider proves itself. Do not turn a requested-but-unavailable cipher into a silent plaintext database.

Before claiming production encrypted-at-rest support:

- choose and license/validate a SQLCipher-compatible provider;
- validate native packaging on every supported native platform;
- add encrypted create/open/backup/restore/migration tests;
- add secure OS credential-store integration;
- expose a user-visible encryption state only when runtime verification can prove it.

## Native preferences

`JsonAppPreferences` stores ordinary UI/safety preferences only. It uses source-generated JSON metadata through `JsonAppPreferencesContext` so mobile/AOT builds do not depend on reflection-based serializer discovery.

When changing the preferences model:

- update the source-generation DTO/context;
- preserve safe defaults for malformed/missing settings;
- keep theme normalization deliberate;
- keep database keys and future secrets out of serialized settings;
- run Infrastructure tests and mobile/Browser gates as applicable.

## Browser persistence maintenance

`BrowserContactRepository` implements `IContactRepository` with IndexedDB-backed serialized state.

Preserve these invariants:

- malformed state and duplicate root identities are rejected;
- repository boundaries deep-copy aggregates;
- writes are serialized;
- failed persistence restores the previous in-memory snapshot;
- duplicate merge stale-checks both reviewed records;
- JSON serialization uses generated metadata rather than reflection-dependent runtime discovery;
- owned synchronization primitives are disposed;
- native SQLite backup/encryption capabilities remain unavailable through Browser capabilities.

A real-browser/IndexedDB automated harness and cross-tab conflict handling remain future work. Do not make strong multi-tab durability claims before those exist.

## Portable Avalonia UI and AOT

The shared `MainView.axaml` uses explicit data types and compiled bindings. Keep portable view markup trim/AOT-friendly:

- prefer typed compiled bindings for production shared UI;
- add `x:DataType` to new item templates;
- do not reintroduce broad reflection bindings into Browser/mobile markup without evaluating trim impact;
- diagnose Browser/iOS linker/AOT failures as first-class regressions;
- keep view-model tests deterministic and independent of real platform storage.

The portable UI test project currently covers search debounce/cancellation ordering and confirmation-gated delete/restore behavior. Extend it for new stateful view-model workflows.

## Duplicate detection and merge

Duplicate detection and merge de-duplication must use the same phone-equivalence policy.

Current phone comparison deliberately prefers false negatives over destructive false positives:

- exact digit-normalized equality matches;
- otherwise suffix equivalence requires a shorter representation of at least ten digits;
- the longer representation may differ by no more than three leading digits.

If changing this rule, update Domain and Application regression cases together and consider the destructive merge impact, not only candidate recall.

Duplicate merge must continue to:

- require explicit reviewed survivor direction;
- require confirmation;
- reload/stale-check records before destructive persistence;
- preserve primary identity;
- avoid contact-owned child-ID collisions;
- update survivor/delete secondary atomically;
- reject missing reviewed records instead of resurrecting stale UI state.

## Import/export maintenance

CSV and vCard are interchange formats, not native full-fidelity backup formats.

When modifying parsers/codecs:

- maintain bounded/malformed-input behavior;
- keep validation messages from unnecessarily echoing private imported values;
- preserve CSV header protections;
- preserve explicit spreadsheet-formula safety boundaries;
- preserve vCard escaping/folding/terminator behavior supported by the implementation;
- add deterministic regression/fuzz-style cases for parser changes;
- keep import validation before one-transaction persistence.

Do not silently broaden claims to full vCard implementation unless the actual property/parameter/encoding surface is covered.

## Backup and restore

Native backup/restore is a high-impact data path.

Maintain the current sequence:

1. verify selected backup before touching active data;
2. stage a copy;
3. migrate/validate the stage;
4. create/verify a pre-restore recovery snapshot;
5. switch active data only after preflight succeeds;
6. verify the switched database;
7. roll back from the recovery snapshot on final verification failure where possible;
8. retain useful failed-copy evidence without exposing private data publicly;
9. clean temporary staging files.

Any new cleanup/failure injection must use disposable fixtures. Never ask contributors to upload a real contact database publicly.

Browser backup remains CSV/vCard export; do not route Browser through native SQLite backup APIs.

## Search and async state

Portable search is debounced and cancellation-safe. New async UI state must not allow stale operations to replace newer results.

When modifying search:

- preserve cancellation propagation;
- retain latest-query wins semantics;
- keep literal SQLite wildcard escaping on native storage;
- update portable UI race tests for changed timing/state behavior;
- avoid unbounded sleeps as correctness mechanisms in tests.

## Accessibility and UX maintenance

For significant UI changes, review:

- keyboard navigation/focus visibility where applicable;
- touch target/phone/tablet layout on mobile heads;
- theme/contrast behavior;
- reduced-motion preference;
- labels/accessible names;
- confirmation/cancellation flows;
- high-DPI/scaling;
- screen-reader behavior on representative real targets before stronger conformance claims.

Do not claim certification/conformance from source inspection alone.

## Performance changes

Before optimizing, preserve correctness/data safety and measure representative workloads.

Future benchmark priorities include 100/1,000/10,000 contacts, duplicate candidate generation, native SQL amplification, browser IndexedDB snapshot writes, pagination/list projection, and potential FTS5 evaluation.

If adopting FTS5 or another index/storage strategy, write an ADR covering migration, synchronization, recovery, and fallback behavior.

## CI maintenance

Required exact-final-head PR gates are:

- Ubuntu core restore/format/Release build/all tests;
- Windows core restore/format/Release build/all tests;
- macOS core restore/format/Release build/all tests;
- Browser/WebAssembly Release build;
- Android `android-arm64` Release build;
- iOS `iossimulator-arm64` Release build after selecting compatible Xcode;
- CodeQL.

All five behavioral test projects should continue to resolve the shared XPlat coverage collector.

Do not remove a platform job because it exposes a real regression. Fix the code/toolchain or document a deliberate platform-support decision.

### iOS simulator boundary

The public iOS gate selects `/Applications/Xcode_26.0.app/Contents/Developer` and builds `iossimulator-arm64`. `ContactCore.iOS.csproj` applies `TrimMode=copy` only for simulator RIDs.

This scoped policy exists after application-owned trim hazards were fixed with generated JSON metadata and compiled UI bindings. It avoids turning third-party linker diagnostics in an unsigned simulator build into a false production-distribution claim. It is **not** evidence of final device/App Store trimming, signing, provisioning, or certification.

If a production Apple distribution pipeline is added, create explicit signed/device verification with real protected credentials and do not treat the simulator gate as a substitute.

## Dependency updates

Dependabot suggestions are discovery, not approval.

For package/action updates:

- inspect release notes/security implications;
- preserve central package management;
- run exact-head core + platform gates;
- ensure mobile workload/action runtime compatibility;
- do not merge an automated version bump merely because it is newer;
- update docs when a dependency changes supported platforms/toolchains or release behavior.

## Release procedure

Before tagging:

1. merge only a final PR head whose exact synthetic merge candidate has all required CI/CodeQL gates green;
2. ensure version metadata and intended tag match;
3. ensure changelog/README/platform/setup/CI/release/handoff documentation match source;
4. verify repository inventory when tracked files changed;
5. check no real user data/secrets/signing material are tracked;
6. complete the release smoke-test record against the exact candidate/artifacts or explicitly mark unavailable manual checks;
7. preserve signing/manual-verification boundaries.

The tag workflow publishes six desktop archives plus Browser WebAssembly ZIP and checksums after Android/iOS source build gates. It does not produce production mobile store packages.

Do not tag an unmerged audit branch as the canonical public release unless the project explicitly changes its release policy.

## Branch/repository governance

`main` should be protected by repository rules requiring the stable CI and CodeQL checks before merge, blocking uncontrolled force-push/deletion, and routing normal changes through pull requests. Repository settings must match the documented check names after the final v2.0.12 workflow stabilizes.

If emergency bypass is ever enabled, document who may use it and require a follow-up audit trail. Do not normalize direct unchecked pushes as routine maintenance.

## Documentation maintenance

Update documentation in the same change whenever behavior/platform/release claims change.

At minimum consider:

- `README.md`;
- `CHANGELOG.md`;
- `ROADMAP.md`;
- `docs/platform-support.md`;
- `docs/setup.md`;
- `docs/architecture.md`;
- `docs/testing.md`;
- `docs/ci-cd.md`;
- `docs/release.md`;
- `docs/repository-reference.md`;
- `what_changed.md`.

The canonical repository inventory is currently **132 tracked files**. Regenerate `docs/repository-reference.md` whenever a tracked file is added/removed/renamed.

## Security/reporting

Keep public issue/PR discussion free of private user data and secrets. Security-sensitive findings should follow `SECURITY.md` rather than being turned into a public data dump.

Before merging/releasing, review diffs for:

- real contact data;
- SQLite databases/WAL/SHM;
- exports/backups;
- `.env`/tokens/passwords;
- certificates/keystores/provisioning profiles;
- private endpoints/identifiers;
- screenshots containing personal information.

## Current remaining non-blocking work

After the v2.0.12 merge/release gate, meaningful future work includes:

- repeated-field drag/reorder;
- global group/tag taxonomy management;
- general undo/recovery UX;
- real IndexedDB browser automation and cross-tab conflict handling;
- deeper restore cleanup/failure injection;
- representative accessibility/lifecycle automation;
- scale benchmarks and candidate-generation optimization;
- production SQLCipher/secret-store integration if chosen;
- signed/notarized/store distribution pipelines;
- additional installer/package-manager formats.

Do not relabel those items complete until implementation and appropriate verification actually exist.
