# ContactCore — v2.0.12 Final Cross-Platform Handoff

## Release checkpoint

ContactCore **2.0.12** is being finalized through the repository's single authoritative integration path.

- Repository: `sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Integration base: `3900063bcdc2f7f0834118abc2580e030f133d73`
- Authoritative branch: `audit/contactcore-20260819`
- Authoritative pull request: **PR #4**
- Final continuation checkpoint before exact-head verification: `7b87188a1b3ba02b94000cc3c789fce4e8a3067d`
- Version: **2.0.12**
- Intended tag after verified merge: **`v2.0.12`**
- Stack: C# / .NET 10 / Avalonia 12.1.1 / SQLite on native targets / IndexedDB in Browser
- License: MIT
- Product posture: private, local-first, cross-platform contact manager
- Project credit: **Made by the Sanskar**
- Canonical tracked-file inventory: **132 files**

Older overlapping integration attempts remain superseded. PR #4 is the intended v2.0.12 merge path.

## 2026-08-25 next-version continuation

The next feature line has been started without changing the still-verifying v2.0.12 release candidate. The new branch is based on the exact PR #4 source head `4005b19fddeda7989cc52522aadd0bc91fece8e3`, so the v2.1.0 delta is isolated from the 331-commit v2.0.12 integration history.

### v2.0.12 verification status at this checkpoint

The targeted rerun of the previously cancelled iOS job is active on the same PR #4 source head. In the rerun:

- Ubuntu core restore/format/Release build/tests: **success**;
- Windows core restore/format/Release build/tests: **success**;
- macOS core restore/format/Release build/tests: **success**;
- Browser/WebAssembly Release build: **success**;
- Android `android-arm64` Release build: **success**;
- CodeQL on the exact source head: **success**;
- iOS `iossimulator-arm64`: setup, checkout, .NET setup, Xcode selection, workload installation, and restore are **success**; the final simulator build step is still **in progress** at this checkpoint.

PR #4 remains deliberately unmerged until that final iOS build step succeeds. No green-release claim is made early.

### v2.1.0 branch and draft PR

- Branch: `release/contactcore-2.1.0`
- Draft pull request: **PR #17**
- Initial base: `audit/contactcore-20260819`
- Version metadata: **2.1.0**

PR #17 is intentionally based on the v2.0.12 integration branch while PR #4 is still open. The repository CI workflow only runs `pull_request` events whose base is `main`, so PR #17 will remain draft and unverified until PR #4 lands and PR #17 is retargeted to `main`. The workflow trigger is not being weakened merely to manufacture an early green check.

### v2.1.0 commits so far

1. `0d4c8db18` — `chore(version): start ContactCore 2.1.0`
2. `c653e4470` — `feat(ui): add repeated-field reorder commands`
3. `e736e94e1` — `test(ui): cover repeated-field reordering`
4. `4d7c83917` — `feat(ui): expose repeated-field reorder controls`
5. `f8cc530bc` — `build(deps): update Microsoft.NET.Test.Sdk to 18.9.0`
6. `0ee5e0a58` — `docs: add ContactCore 2.1.0 work plan`

### Repeated-field reordering

The portable contact draft now has move-up/move-down commands for:

- phone numbers;
- email addresses;
- addresses;
- organizations;
- groups;
- tags.

The commands use `ObservableCollection<T>.Move`, do nothing at collection boundaries, and do not regenerate the moved row's identity or content. `ToContact()` already enumerates these collections in their current order, so saved aggregate order follows the order selected in the editor.

The shared compiled-binding Avalonia editor exposes explicit **Move up** and **Move down** controls beside the existing remove controls. Because this lives in `ContactCore.UI`, the same implementation is shared by the portable desktop/mobile/browser experience instead of creating divergent platform-specific ordering logic.

Explicit buttons are the initial accessible cross-platform interaction. Pointer drag/drop can be evaluated later only if keyboard, touch, screen-reader, focus, trimming, and AOT behavior remain correct across supported targets.

### Reordering regression coverage

New test file:

```text
tests/ContactCore.UI.Tests/ContactDraftViewModelReorderTests.cs
```

Coverage includes:

- first-row move-up boundary no-op;
- last-row move-down boundary no-op;
- phone move-down;
- email move-up;
- address move-down;
- organization move-up;
- group move-down;
- tag move-up;
- emitted phone/email save order after reordering.

These tests are committed but are not yet represented as CI-green 2.1.0 evidence because PR #17 has not yet been retargeted to `main`.

### Dependency maintenance

The 2.1.0 line updates centrally managed `Microsoft.NET.Test.Sdk` from **18.8.1** to **18.9.0**. The existing v2.0.12 release candidate is left unchanged so dependency maintenance does not invalidate the release-gate evidence being collected for PR #4.

### Documentation and inventory delta

New 2.1.0 files currently add two tracked paths relative to the 132-file v2.0.12 baseline:

```text
tests/ContactCore.UI.Tests/ContactDraftViewModelReorderTests.cs
docs/next-version-2.1.0.md
```

Therefore the current next-version branch has a **134-file logical inventory before this handoff update is counted as a path change**; `what_changed.md` itself already existed in the baseline and does not add a new tracked path. The canonical `docs/repository-reference.md` remains the 132-file v2.0.12 release inventory until the 2.1.0 documentation pass deliberately regenerates it.

### Next integration sequence

1. Finish the exact-head PR #4 iOS simulator gate.
2. Merge PR #4 to `main` only if all required exact-head checks are green.
3. Retarget draft PR #17 to `main`, which will trigger the repository's normal CI and CodeQL policy.
4. Fix any real 2.1.0 regression reported by those checks before marking PR #17 ready.
5. Continue measured 2.1.0 work: taxonomy management, undo/recovery, browser resilience, performance benchmarks, and distribution/security improvements without fabricating external signing or device-certification evidence.

## 2026-08-24 continuation

This continuation was driven by the latest real PR #4 runner evidence. Product scope was not expanded ahead of release correctness; the work concentrated on concrete CI failures, AOT/trim safety, accurate release boundaries, and documentation synchronization.

### Production/code commits

1. `4b569e88b` — `fix(domain): tighten country-code phone equivalence`
2. `bdbe1abd4` — `fix(browser): seal source-generated JSON context`
3. `f5cdee18f` — `feat(infrastructure): add trim-safe preferences JSON context`
4. `6cd4df346` — `fix(infrastructure): use generated preferences serialization`
5. `4d78c92f7` — `fix(ui): compile shared bindings for trimmed targets`
6. `d32df6eff` — `fix(ios): use copy trim mode for simulator gate`

### Documentation synchronization commits

7. `e38853cd3` — `docs: regenerate canonical 132-file repository reference`
8. `c371ceda2` — `docs(ci): document final mobile and AOT gate behavior`
9. `7a00d5113` — `docs: record August 24 final release-gate hardening`
10. `5502c91f2` — `docs(release): clarify iOS simulator verification boundary`
11. `bceeefde4` — `docs(changelog): record August 24 release-gate fixes`
12. `350aaabae` — `docs(maintainers): record simulator and final-head release rules`
13. `d9d6259a5` — `docs(readme): sync final AOT and iOS simulator posture`
14. `ea1d33e03` — `docs(platforms): clarify iOS simulator versus distribution support`
15. `b0d6cbd38` — `docs: sync documentation index with 132-file final gate`
16. `7b87188a1` — `docs(roadmap): close final 2.0.12 release-gate hardening items`

A later optional attempt to rewrite `docs/testing.md` encountered a stale-content SHA conflict. It was deliberately not forced; no user/source data was at risk and the existing testing guide remains tracked. The canonical CI/release/platform/maintainer/handoff documents already describe the final verification boundary.

### Phone-equivalence correction

`PhoneKey` remains a digits-only normalization primitive. `PhoneEquivalent` now accepts a suffix-based country-code equivalence only when:

- exact normalized equality did not already match;
- the shorter representation contains at least **10 digits**;
- the longer representation differs by no more than three leading digits;
- the longer representation ends with the shorter representation.

This deliberately prefers a false-negative duplicate over a destructive false-positive merge. It fixes the regression where `+91 98765 43210` was incorrectly considered equivalent to `876543210`.

Duplicate scoring and merge de-duplication continue to use the same equivalence rule.

### Browser/AOT hardening

`BrowserJsonContext` is sealed to satisfy `CA1852` rather than suppressing the analyzer.

The portable production `MainView.axaml` now has:

- root `x:DataType`;
- explicit compiled bindings;
- typed item templates for contact rows, alphabet entries, rich-field editor rows, and duplicate-pair rows.

This removes application-owned reflection-binding trim dependencies from Browser/iOS builds.

Browser persistence continues to use source-generated JSON metadata and IndexedDB. Native SQLite backup/encryption capabilities remain unavailable on Browser by design.

### Native preferences AOT hardening

New tracked file:

```text
src/ContactCore.Infrastructure/JsonAppPreferencesContext.cs
```

`JsonAppPreferences` now serializes/deserializes through generated `JsonTypeInfo` metadata. Database keys remain runtime-only and are not serialized into settings.

### iOS simulator boundary

The current public iOS gate selects:

```text
/Applications/Xcode_26.0.app/Contents/Developer
```

and builds:

```text
ios simulator RID: iossimulator-arm64
```

`ContactCore.iOS.csproj` applies `TrimMode=copy` **only to simulator RIDs**.

This was introduced only after application-owned trim hazards were removed through generated JSON metadata and compiled XAML bindings. The simulator gate verifies source/runtime integration. It does **not** claim production device trimming, Apple signing, provisioning, TestFlight/App Store acceptance, or representative physical-device certification.

Production Apple distribution remains a future protected pipeline requiring real maintainer-controlled credentials.

## Verification evidence

For code checkpoint `d32df6effb93d3312c7306cfebd5a966744ad3f5`, GitHub Actions confirmed before subsequent documentation commits superseded that run:

- Ubuntu core restore/format/Release build/tests: **success**;
- Windows core restore/format/Release build/tests: **success**;
- macOS core restore/format/Release build/tests: **success**;
- Browser/WebAssembly Release build: **success**;
- Android `android-arm64` Release build: **success**;
- CodeQL: **success**;
- iOS simulator: still executing when documentation synchronization intentionally changed the PR head.

Because PR workflow concurrency cancels obsolete attempts, **none of the above is the final merge approval**. The merge signal must come from CI + CodeQL for the exact final PR #4 synthetic merge candidate after this handoff checkpoint.

## Current architecture and platforms

| Platform | Target/runtime | Persistence | Current posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | core CI + ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | ZIP release |
| Linux x64 | `linux-x64` | SQLite | core CI + tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | tar.gz release |
| macOS Intel | `osx-x64` | SQLite | core CI + tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | core CI + tar.gz release |
| Android | `net10.0-android`, `android-arm64` CI RID | SQLite | source/build gate; production signing separate |
| iPhone/iPad | `net10.0-ios`, `iossimulator-arm64` CI RID | SQLite | unsigned simulator source/runtime gate; device distribution separate |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated WASM build + static ZIP |
| ChromeOS | Browser route; Android where supported | route-dependent | no fabricated native ChromeOS target |

## Solution layout

`ContactCore.slnx` is the complete solution. `ContactCore.Core.slnx` is the workload-free quality/CodeQL solution.

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

## Product/data behavior retained

Implemented behavior includes:

- local-first contacts with no mandatory account/cloud/telemetry dependency;
- rich contact fields and repeated rows;
- stable contact/contact-owned identities;
- safe shared group/tag reassignment;
- unsaved-versus-persisted draft safety;
- All/Favorites/Archived/A–Z filters;
- debounce/cancellation-safe search;
- conservative duplicate detection and stale-safe merge;
- hardened CSV/focused-vCard import/export;
- transactional native imports/persistence;
- verified native backup/restore with recovery/rollback safeguards;
- runtime-only database-key handling with fail-closed requested cipher verification;
- System/Light/Dark/reduced-motion/delete-confirmation preferences;
- responsive shared UI on Android/iOS/Browser and mature Desktop UI.

## Quality posture

The repository keeps:

- `TreatWarningsAsErrors` globally enabled;
- `AnalysisLevel=latest-recommended`;
- Browser AOT/trimming diagnostics active;
- generated JSON metadata for Browser and native preferences;
- compiled production shared-UI bindings;
- five behavioral test projects with shared XPlat coverage collection;
- three-OS core matrix;
- Browser/Android/iOS build gates;
- CodeQL.

No general analyzer/trimming gate was disabled to hide the August 24 failures.

## Repository inventory

Canonical tracked-file count: **132**.

Later additions beyond the earlier cross-platform reference include:

```text
src/ContactCore.Infrastructure/Properties/AssemblyInfo.cs
src/ContactCore.Infrastructure/JsonAppPreferencesContext.cs
src/ContactCore.Browser/BrowserJsonContext.cs
tests/ContactCore.UI.Tests/ContactCore.UI.Tests.csproj
tests/ContactCore.UI.Tests/TestDoubles.cs
tests/ContactCore.UI.Tests/MainViewModelSearchTests.cs
tests/ContactCore.UI.Tests/MainViewModelConfirmationTests.cs
docs/release-smoke-test.md
```

See `docs/repository-reference.md` for the canonical file-by-file inventory.

## Required exact-final-head merge gate

Before PR #4 may merge, the same current synthetic merge candidate must have:

- Ubuntu core restore/format/build/tests: success;
- Windows core restore/format/build/tests: success;
- macOS core restore/format/build/tests: success;
- Browser/WebAssembly Release build: success;
- Android `android-arm64` Release build: success;
- iOS `iossimulator-arm64` Release build using compatible Xcode and simulator trim policy: success;
- CodeQL: success/no unresolved newly introduced actionable finding.

An older green/cancelled/superseded run is not sufficient.

## Release process after merge

1. Confirm the verified PR head is merged to `main`.
2. Apply/verify `main` branch protection/ruleset requiring the stable checks.
3. Complete `docs/release-smoke-test.md` against the actual candidate/artifacts or explicitly mark sections not executed.
4. Create `v2.0.12` only from the intended verified merged commit.
5. Confirm release workflow output:
   - six desktop archives;
   - Browser WebAssembly ZIP;
   - Android/iOS source build gates;
   - `SHA256SUMS.txt`.
6. Keep signing/notarization/store claims separate until real protected pipelines exist.

## Remaining non-blocking roadmap

### Product/UX

- repeated-field drag/reorder;
- global group/tag taxonomy management;
- general undo/recovery UX.

### Browser/resilience

- real IndexedDB automation harness;
- cross-tab conflict strategy/tests;
- deeper native restore cleanup/failure injection.

### Performance

- reproducible 100/1,000/10,000-contact benchmarks;
- SQL amplification measurement;
- Browser snapshot-write benchmarks;
- pagination/list projection evaluation;
- FTS5 ADR/evaluation if justified;
- duplicate-candidate optimization;
- streaming import evaluation while preserving atomicity.

### Security/distribution

- production SQLCipher provider/packaging/licensing if selected;
- native secure secret-store abstraction;
- Windows signing/installers;
- macOS signing/notarization;
- Android production signing/store publishing;
- iOS signed device/TestFlight/App Store pipeline;
- additional package-manager formats.

### Manual verification

- representative desktop keyboard/screen-reader/high-DPI/theme checks;
- Android/iOS touch/orientation/file-picker/lifecycle/accessibility checks;
- Browser persistence/accessibility checks across representative engines/profiles;
- real product screenshots using only fictional data.

These items are deliberately not mislabeled as complete.

## Current posture

The remaining immediate task is **verification, not speculative feature expansion**: run CI + CodeQL on the exact final PR #4 head, fix any real remaining failure, then merge through the documented path. Production signing and manual representative-device/browser checks remain explicit external requirements rather than fabricated completion claims.
