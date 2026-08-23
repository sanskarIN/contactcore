# ContactCore — v2.0.12 Final Cross-Platform Handoff

## Release checkpoint

ContactCore **2.0.12** is being finalized through the repository's single authoritative integration path.

- Repository: `sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Integration base: `3900063bcdc2f7f0834118abc2580e030f133d73`
- Authoritative branch: `audit/contactcore-20260819`
- Authoritative pull request: **PR #4**
- Latest source/docs checkpoint before this handoff update: `85de4295a6ed349595d3bcef0d5d24bbd9dce152`
- Version: **2.0.12**
- Intended tag after verified merge: **`v2.0.12`**
- Stack: C# / .NET 10 / Avalonia 12.1.1 / SQLite on native targets / IndexedDB in Browser
- License: MIT
- Product posture: private, local-first, cross-platform contact manager
- Project credit: **Made by the Sanskar**
- Canonical tracked-file inventory before this handoff update: **131 files**

Older overlapping integration attempts remain superseded. PR #4 is the intended v2.0.12 merge path.

## 2026-08-23 continuation

This continuation resumed from PR #4 instead of starting another overlapping branch. Work was driven by the actual latest GitHub Actions failures first, then expanded into missing regression coverage and release-process documentation.

### CI failures found at the start of this continuation

The previous exact-head run showed:

- CodeQL: **success**;
- Android `android-arm64`: **success**;
- core restore/format/build on Ubuntu, Windows, and macOS: **success**;
- core tests on all three OSes: **failure** because `Merger_deduplicates_phone_numbers` expected one phone after merging `+91 98765 43210` with `9876543210`, but two remained;
- Browser/WebAssembly build: **failure** because reflection-based `System.Text.Json` calls produced trimming/AOT diagnostic `IL2026`, and `BrowserContactRepository` owned a disposable `SemaphoreSlim` without implementing disposal (`CA1001`);
- iOS simulator build: **failure** because the installed .NET iOS workload required the Xcode 26.0 toolchain while the rolling macOS runner had selected Xcode 26.6;
- Application and Infrastructure tests reported that `XPlat Code Coverage` was unavailable because those projects did not reference `coverlet.collector`.

No failing gate was removed or weakened merely to obtain a green badge.

## Commits added in this continuation

The continuation intentionally used small, reviewable commits rather than one monolithic change.

### Phone duplicate correctness

1. `0d8256461` — `feat(domain): add country-code aware phone equivalence`
2. `5e991371d` — `fix(application): deduplicate equivalent international phone numbers`
3. `5d3767e39` — `test(domain): cover international phone equivalence boundaries`
4. `fb8d0a62c` — `test(application): lock country-code duplicate behavior`

`PhoneKey` remains a lossless digits-only normalization helper. A separate `PhoneEquivalent` rule now handles comparison: exact normalized numbers match immediately; otherwise a suffix comparison is accepted only when the shorter local part contains at least seven digits and the longer number differs by no more than three leading digits. This addresses normal country-calling-code differences without treating very short extensions/service numbers as equivalent.

Duplicate scoring and merge de-duplication both use the same equivalence rule.

### Coverage collector consistency

5. `6f1749fe5` — `test(application): enable cross-platform coverage collection`
6. `2a4ea6fe9` — `test(infrastructure): enable cross-platform coverage collection`

All five current behavioral test projects now reference the centrally pinned `coverlet.collector`, so the shared CI command:

```text
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

has the required collector available throughout the test suite.

### Browser trimming/AOT hardening

7. `deca467b4` — `feat(browser): add trimming-safe JSON serialization metadata`
8. `7855f27d5` — `fix(browser): make preferences serialization AOT safe`
9. `a546e0943` — `fix(browser): make contact persistence AOT safe and disposable`

`src/ContactCore.Browser/BrowserJsonContext.cs` now contains source-generated `System.Text.Json` metadata for browser preferences and full contact persistence DTOs. Browser persistence retains camel-case JSON compatibility while avoiding reflection-dependent serializer discovery in trimmed WebAssembly builds.

`BrowserContactRepository` now implements `IDisposable` and disposes its owned `SemaphoreSlim` instead of suppressing the lifetime analyzer.

### iOS toolchain determinism

10. `00a64368a` — `ci(ios): select compatible Xcode toolchain explicitly`
11. `1f1ac795d` — `ci(release): pin compatible Xcode for iOS gate`

Normal CI and the tag-release mobile gate both select:

```text
/Applications/Xcode_26.0.app/Contents/Developer
```

before installing/building the iOS workload. The iOS gate continues to use explicit RID `iossimulator-arm64`. Android continues to use explicit RID `android-arm64`.

This prevents a rolling hosted-runner default from silently advancing beyond the toolchain accepted by the current .NET iOS workload.

### First handoff synchronization

12. `601baba32` — `docs: refresh v2.0.12 handoff after exact-head CI repairs`

This recorded the first eleven repair commits and the then-current verification boundary.

### Dedicated portable UI regression project

13. `c68bde4f8` — `test(ui): add portable view-model test project`
14. `cb134f27b` — `test(ui): add deterministic repository and service doubles`
15. `702b60238` — `test(ui): cover search debounce and cancellation races`
16. `198567aca` — `test(ui): enforce destructive action and restore confirmations`
17. `8e7c18ee7` — `build: include portable UI tests in core solution`
18. `a54edee17` — `build: include portable UI tests in full solution`

New project:

```text
tests/ContactCore.UI.Tests
```

It is included in both `ContactCore.Core.slnx` and `ContactCore.slnx` and uses the same MSTest/XPlat coverage stack as the other test projects.

The deterministic test doubles do not touch production SQLite, IndexedDB, backups, user profiles, or real contacts.

`MainViewModelSearchTests` now verifies:

- rapid input changes are debounced to the latest query rather than issuing every intermediate term;
- an already-running stale search receives cancellation when a newer term arrives;
- stale results cannot replace the visible list after a newer query wins.

`MainViewModelConfirmationTests` now verifies:

- permanent deletion waits for confirmation when the safety preference is enabled;
- cancelling a pending delete preserves the contact;
- the explicitly disabled delete-confirmation preference permits the documented direct-delete path;
- restore selection queues confirmation before `IBackupService.RestoreBackupAsync` can execute;
- confirming restore invokes the reviewed backup path.

These additions close the roadmap items for search debounce/cancellation view-model tests and destructive-action/restore confirmation view-model tests.

### Documentation synchronization for the test/CI work

19. `b9a59b71c` — `docs(testing): document portable UI regression coverage`
20. `b34c59197` — `docs: regenerate canonical 130-file repository reference`
21. `37f5e3218` — `docs(roadmap): mark portable UI resilience coverage complete`
22. `8750abe3f` — `docs(changelog): record August 23 CI and UI resilience work`
23. `9c646904b` — `docs(ci): document coverage, AOT, and Xcode gates`
24. `738e3e5e0` — `docs(readme): sync CI, tests, browser AOT, and mobile commands`

README, testing, CI/CD, changelog, roadmap, and the repository reference were updated to describe five behavioral test projects, the browser source-generation path, explicit mobile RIDs, and the compatible iOS Xcode selection.

### Repeatable release smoke-test evidence

25. `d0408d45f` — `docs(release): add repeatable smoke-test record template`
26. `c5e12e57e` — `docs: index the release smoke-test record`
27. `60bd03ec2` — `docs(release): integrate exact-SHA smoke-test procedure`
28. `eed939ef4` — `docs(roadmap): complete repeatable release smoke record`
29. `85de4295a` — `docs: regenerate canonical 131-file repository reference`

New document:

```text
docs/release-smoke-test.md
```

The template requires manual evidence to identify the exact candidate SHA and records:

- exact-head CI/CodeQL results;
- fictional/disposable test fixture rules;
- expected release artifacts and checksum verification;
- desktop smoke matrix;
- browser/IndexedDB smoke matrix;
- Android device/emulator smoke matrix;
- iPhone/iPad simulator/device smoke matrix;
- data-safety scenarios;
- accessibility/interaction smoke checks;
- privacy review for screenshots/logs/evidence;
- explicit deviations/skipped checks;
- final APPROVE/HOLD/REJECT decision.

It explicitly prevents an older manual record from being reused as evidence after the release candidate changes.

The roadmap item to publish a repeatable manual release smoke-test record/template is now complete.

## Current architecture and platforms

| Platform | Target/runtime | Persistence | Verification/release posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | core CI + ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | ZIP release |
| Linux x64 | `linux-x64` | SQLite | core CI + tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | tar.gz release |
| macOS Intel | `osx-x64` | SQLite | core CI + tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | tar.gz release |
| Android | `net10.0-android`, `android-arm64` CI RID | SQLite | dedicated workload/Release build gate |
| iPhone/iPad | `net10.0-ios`, `iossimulator-arm64` CI RID | SQLite | dedicated macOS workload/Release build gate |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated WASM build + browser ZIP |
| ChromeOS | Browser route; Android where supported | IndexedDB/SQLite by route | no false separate native ChromeOS target |

Source/build support is intentionally separated from store signing and certification. The repository does not fabricate Android keystores, Apple signing certificates, provisioning profiles, passwords, private keys, notarization, or store approval.

## Solution layout

`ContactCore.slnx` is the complete cross-platform solution. `ContactCore.Core.slnx` is the workload-free solution used by ordinary three-OS CI and CodeQL.

Current project graph:

```text
ContactCore.Domain
ContactCore.Application
ContactCore.Infrastructure
ContactCore.UI
ContactCore.Native
ContactCore.Desktop
ContactCore.Android
ContactCore.iOS
ContactCore.Browser

ContactCore.Domain.Tests
ContactCore.Application.Tests
ContactCore.Infrastructure.Tests
ContactCore.UI.Tests
ContactCore.Desktop.Tests
```

## Product behavior retained and hardened

Implemented behavior includes:

- local-first contact management with no mandatory account/cloud/telemetry dependency;
- rich names, nickname, birthday and notes;
- multiple phones/emails/addresses/organizations;
- groups and tags with exact delimiter-containing names;
- stable contact and contact-owned child identities through ordinary edits;
- safe shared group/tag reassignment semantics;
- explicit unsaved/persisted draft state;
- All/Favorites/Archived/A-Z filters;
- debounced, cancellation-safe free-text search;
- country-code-aware duplicate phone comparison;
- duplicate evidence, preview, explicit survivor choice and confirmation;
- stale-safe duplicate merge;
- CSV/vCard import/export with hardened parsing;
- transactional native imports and persistence;
- native verified SQLite backup/restore with staging/recovery safeguards;
- runtime-only database-key handling and fail-closed requested cipher verification;
- capability-aware Browser behavior that does not claim native SQLite backup/encryption;
- System/Light/Dark theme preference;
- reduced-motion preference;
- delete confirmation preference;
- keyboard shortcuts where supported;
- responsive shared UI on single-view/mobile/browser hosts.

## Browser/WebAssembly state

The Browser target does not reference native SQLite infrastructure. `BrowserContactRepository` implements `IContactRepository` through IndexedDB via .NET/JavaScript interop.

Current Browser persistence behavior:

- loads a complete local contact snapshot;
- rejects malformed serialized state and duplicate root contact identities;
- preserves rich aggregate fields/IDs;
- applies query/filter behavior behind the existing repository contract;
- serializes writes behind a semaphore;
- snapshots in-memory state before mutation;
- restores pre-write state if IndexedDB persistence fails;
- requires both reviewed records for duplicate merge;
- deep-copies repository boundaries;
- uses source-generated JSON type metadata for trimming/AOT safety;
- disposes the owned semaphore correctly.

Automated real-IndexedDB browser integration tests and cross-tab conflict handling remain future work and are not overclaimed.

## Analyzer, compiler and test posture

The quality gate remains strict:

- `TreatWarningsAsErrors` remains enabled globally;
- `AnalysisLevel` remains `latest-recommended`;
- test-only style/micro-optimization exceptions remain scoped to tests;
- Avalonia-specific analyzer exceptions remain narrow;
- Browser AOT/trimming diagnostics are fixed through source-generation rather than broad suppression;
- all five test projects use centrally managed MSTest/test SDK/coverage versions;
- infrastructure test visibility remains limited through `InternalsVisibleTo("ContactCore.Infrastructure.Tests")` rather than widening production APIs.

## CI and release gates

Core matrix on Ubuntu, Windows and macOS:

```text
dotnet restore ContactCore.Core.slnx
dotnet format ContactCore.Core.slnx --verify-no-changes --no-restore
dotnet build ContactCore.Core.slnx -c Release --no-restore
dotnet test ContactCore.Core.slnx -c Release --no-build --collect:"XPlat Code Coverage"
```

Browser:

```text
install wasm-tools
restore ContactCore.Browser
Release build ContactCore.Browser
```

Android:

```text
install android workload
restore ContactCore.Android -r android-arm64
Release build ContactCore.Android -r android-arm64
```

iOS/iPadOS:

```text
select /Applications/Xcode_26.0.app/Contents/Developer
install ios workload
restore ContactCore.iOS -r iossimulator-arm64
Release build ContactCore.iOS -r iossimulator-arm64
```

CodeQL builds/analyzes the workload-free core solution. CI and CodeQL use same-PR concurrency cancellation so obsolete attempts cannot become the merge signal.

The tag release workflow still requires version/tag equality and publishes:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-win-arm64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-linux-arm64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
contactcore-v2.0.12-browser-wasm.zip
SHA256SUMS.txt
```

Android/iOS Release-build gates must succeed before final release creation. Production mobile signing remains external until real secure credentials/policy exist.

## Repository inventory

Current canonical inventory: **131 tracked files**.

The 2026-08-23 additions beyond the earlier cross-platform reference are:

```text
src/ContactCore.Infrastructure/Properties/AssemblyInfo.cs
src/ContactCore.Browser/BrowserJsonContext.cs
tests/ContactCore.UI.Tests/ContactCore.UI.Tests.csproj
tests/ContactCore.UI.Tests/TestDoubles.cs
tests/ContactCore.UI.Tests/MainViewModelSearchTests.cs
tests/ContactCore.UI.Tests/MainViewModelConfirmationTests.cs
docs/release-smoke-test.md
```

`docs/repository-reference.md` is the canonical file-by-file inventory and must change whenever the tracked tree changes.

## Documentation state

The synchronized documentation set includes:

- `README.md`;
- `CHANGELOG.md`;
- `ROADMAP.md`;
- `PRIVACY.md`;
- `SECURITY.md`;
- `SUPPORT.md`;
- `CONTRIBUTING.md`;
- `docs/README.md`;
- `docs/platform-support.md`;
- `docs/setup.md`;
- `docs/development.md`;
- `docs/architecture.md`;
- `docs/data-model.md`;
- `docs/desktop-ui.md`;
- `docs/user-guide.md`;
- `docs/import-export.md`;
- `docs/storage-backup-recovery.md`;
- `docs/security.md`;
- `docs/accessibility.md`;
- `docs/performance.md`;
- `docs/testing.md`;
- `docs/troubleshooting.md`;
- `docs/ci-cd.md`;
- `docs/release.md`;
- `docs/release-smoke-test.md`;
- `docs/maintainer-guide.md`;
- `docs/repository-reference.md`;
- ADRs under `docs/adr/`;
- this handoff.

## Exact-head verification boundary

The coding environment used for these GitHub edits does not provide a trusted local .NET/mobile/WebAssembly workload matrix capable of replacing GitHub Actions, so local success is not invented.

At the time the current documentation sequence was being prepared, exact-head CI/CodeQL runs were being repeatedly superseded by the intentional sequence of new commits. A final merge must therefore use a workflow run corresponding to the final PR #4 head after this handoff/PR-description synchronization—not an older successful or cancelled run.

Required merge conditions:

- core restore/format/build/tests — Ubuntu: success;
- core restore/format/build/tests — Windows: success;
- core restore/format/build/tests — macOS: success;
- Browser Release build: success;
- Android `android-arm64` Release build: success;
- iOS `iossimulator-arm64` Release build with the compatible Xcode selection: success;
- CodeQL: success/no unresolved newly introduced actionable result;
- all results correspond to the latest PR merge candidate.

PR CI validates GitHub's synthetic merge of the PR head with `main`, so the final check must correspond to that actual merge candidate.

## Remaining roadmap after this continuation

Meaningful remaining work is intentionally not mislabeled as complete:

### UX/product

- drag/reorder controls for repeated rich fields;
- dedicated global group/tag taxonomy management with defined rename/delete/orphan semantics;
- general undo/recovery UX for high-impact contact modifications.

### Tests/resilience

- automated real-browser/IndexedDB repository harness;
- forced post-switch native restore verification failure/rollback test;
- deeper restore staging/temp cleanup failure injection;
- stable native/Avalonia integration automation;
- automated accessibility smoke coverage where supported.

### Performance/scale

- reproducible 100/1,000/10,000-contact benchmark harness/results;
- SQL statement amplification measurement;
- Browser IndexedDB snapshot-write benchmarks;
- list projection/pagination evaluation;
- FTS5 evaluation with an ADR/migration/index-sync plan if justified;
- duplicate-candidate optimization before high-scale use;
- streaming CSV/vCard evaluation while preserving import atomicity.

### Encryption/secrets

- select/validate a production-supported SQLCipher-compatible provider if native encrypted-at-rest shipping is chosen;
- provider licensing/native packaging verification per applicable platform;
- encrypted database/backup/restore integration tests;
- native OS credential/secret-store abstraction;
- user-visible verified encryption state only after the runtime can prove it.

### Manual/distribution work

- real product screenshots from verified builds using fictional data;
- representative desktop keyboard/screen-reader/high-DPI/theme audits;
- representative Android/iOS touch/orientation/file-picker/lifecycle/accessibility audits;
- representative browser-engine/profile persistence/accessibility smoke runs;
- Windows signing;
- macOS Developer ID signing/notarization;
- Android production keystore/store publishing;
- iOS signing/provisioning/App Store publishing;
- package-manager/installer/store formats beyond the current archives/source build gates.

Signing/provisioning tasks cannot be truthfully completed without real maintainer-controlled credentials and policies. Manual device/browser checks cannot be truthfully marked complete without actually running them. Those boundaries are preserved rather than fabricated.

## Merge and release procedure

1. Keep PR #4 as the authoritative integration path.
2. Synchronize PR #4 description with the final head/inventory/verification boundary.
3. Run CI and CodeQL on that final PR merge candidate.
4. Fix every actionable failure with a focused commit and repeat exact-head verification.
5. Merge PR #4 only when all required gates are green for the exact final candidate.
6. Confirm `main` contains 2.0.12 metadata after merge.
7. Create `v2.0.12` only from the intended verified merged commit.
8. Confirm six desktop archives, Browser ZIP, mobile build gates, and `SHA256SUMS.txt` in the tag workflow.
9. Complete a copy of `docs/release-smoke-test.md` against the actual released SHA/artifacts and representative environments.
10. Do not claim signed/notarized/store-certified mobile/desktop distribution until a real secure signing pipeline produces and verifies it.

## Current posture

ContactCore 2.0.12 has a deliberate cross-platform source/build architecture for Windows, Linux, macOS, Android, iPhone, iPad, and modern WebAssembly-capable browsers, with ChromeOS covered through supported Browser/Android routes.

The August 23 continuation repaired the concrete CI regressions found on PR #4, added stronger international phone duplicate semantics, made Browser serialization trimming/AOT-safe, made coverage configuration consistent, pinned the compatible iOS toolchain, added dedicated portable UI race/confirmation regression tests, and added a repeatable exact-SHA release smoke-test record.

The remaining immediate release criterion is not more unverified feature claims: it is exact-final-head CI + CodeQL success, followed by the documented merge/tag/release process.
