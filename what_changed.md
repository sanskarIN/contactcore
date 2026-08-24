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
