# ContactCore — v2.0.12 Cross-Platform Final Handoff

## Release checkpoint

ContactCore **2.0.12** is being finalized on the authoritative integration branch/PR.

- Repository: `sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Integration base: `3900063bcdc2f7f0834118abc2580e030f133d73`
- Authoritative branch: `audit/contactcore-20260819`
- Authoritative pull request: **PR #4**
- Cross-platform checkpoint immediately before this handoff update: `fee8a2d5469eb2baa8053d8b1f3b9f1cb71b50a2`
- Version: **2.0.12**
- Intended release tag after merge/exact-head verification: **`v2.0.12`**
- Stack: C# / .NET 10 / Avalonia / SQLite on native targets / IndexedDB on Browser
- License: MIT
- Product posture: private, local-first, cross-platform contact manager
- Project credit: **Made by the Sanskar**

PR #1, PR #3, and temporary PR #12 remain closed without merge as superseded. PR #4 remains the only intended v2.0.12 integration path.

## Cross-platform continuation completed on 2026-08-20

The previous release branch was a strong desktop implementation for Windows/Linux/macOS but did not contain first-class Android, iOS/iPadOS, or browser application heads. This continuation extends the architecture rather than replacing the hardened Domain/Application/Infrastructure behavior.

### New platform structure

Added:

```text
src/ContactCore.UI/
src/ContactCore.Native/
src/ContactCore.Android/
src/ContactCore.iOS/
src/ContactCore.Browser/
ContactCore.Core.slnx
docs/platform-support.md
```

The complete solution now contains:

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
+ 4 existing behavioral test projects
```

`ContactCore.Core.slnx` contains the workload-free shared/native/Desktop/test graph used by ordinary three-OS CI and CodeQL. `ContactCore.slnx` remains the complete cross-platform solution.

## Platform support implemented

| Platform | Target | Local persistence | Automation posture |
|---|---|---|---|
| Windows x64 | `win-x64` | SQLite | CI + automated ZIP release |
| Windows ARM64 | `win-arm64` | SQLite | automated ZIP release |
| Linux x64 | `linux-x64` | SQLite | CI + automated tar.gz release |
| Linux ARM64 | `linux-arm64` | SQLite | automated tar.gz release |
| macOS Intel | `osx-x64` | SQLite | CI + automated tar.gz release |
| macOS Apple Silicon | `osx-arm64` | SQLite | CI + automated tar.gz release |
| Android | `net10.0-android` | SQLite | dedicated Android workload/Release-build gate |
| iPhone/iPad | `net10.0-ios` | SQLite | dedicated macOS iOS workload/Release-build gate |
| Browser/WebAssembly | `net10.0-browser` | IndexedDB | dedicated WASM build + automated browser ZIP |
| ChromeOS route | Browser; Android where device supports it | IndexedDB/SQLite by route | no separate native ChromeOS project |

Cross-platform source/build support is deliberately separated from app-store signing/certification. No Android production keystore, Apple signing certificate, provisioning profile, signing password, or private key is committed or fabricated.

## Shared portable UI

`ContactCore.UI` adds an Avalonia application/presentation layer suitable for single-view mobile/browser lifetimes while retaining compatibility with classic desktop lifetime.

Implemented shared workflows:

- responsive contact list/navigation;
- free-text search with debounce/cancellation;
- All/Favorites/Archived/A-Z filters;
- complete rich contact draft/editor;
- phones/emails/addresses/organizations/groups/tags add/edit/remove;
- stable contact-owned child IDs;
- safe shared group/tag reassignment semantics;
- unsaved-vs-persisted state;
- save/delete/discard;
- duplicate scan/evidence/preview;
- explicit first/second survivor choice;
- confirmation-gated destructive merge;
- CSV/vCard import/export;
- native capability-aware backup/restore;
- System/Light/Dark settings;
- reduced-motion and delete-confirmation preferences;
- in-view confirmation state that does not require a desktop `Window` dialog;
- storage-provider file picking/stream handling;
- keyboard shortcuts on keyboard-capable hosts.

The mature `ContactCore.Desktop` head was not destructively rewritten; it continues to own its existing desktop window/dialog/presentation while sharing Domain/Application/Infrastructure behavior.

## Native composition and mobile persistence

`ContactCore.Native` composes:

```text
AppPaths
JsonAppPreferences
SqliteConnectionFactory
DatabaseMigrator
SqliteContactRepository
ContactService
BackupService
```

Android and iOS/iPadOS reuse that native composition, preserving the hardened SQLite transaction/migration/search/import/merge/backup boundaries instead of introducing a weaker mobile-only data model.

### Android

Added `src/ContactCore.Android`:

- `net10.0-android` executable;
- application ID/version metadata;
- `Avalonia.Android` package;
- `AvaloniaAndroidApplication<App>`;
- `AvaloniaMainActivity` launcher;
- shared UI + native SQLite composition.

CI installs the Android workload and Release-builds the target.

### iOS/iPadOS

Added `src/ContactCore.iOS`:

- `net10.0-ios` executable;
- `Avalonia.iOS` package;
- `AvaloniaAppDelegate<App>`;
- UIKit main entry point;
- `Info.plist` with iPhone+iPad families/orientations;
- shared UI + native SQLite composition.

CI uses macOS, installs the iOS workload, and Release-builds the target.

## Browser/WebAssembly implementation

The browser target does **not** reference native Infrastructure/SQLite. It implements the same Application repository contract through browser-native storage.

Added:

- `ContactCore.Browser.csproj` using `Microsoft.NET.Sdk.WebAssembly` and `net10.0-browser`;
- Avalonia.Browser application startup;
- .NET 10 `[JSImport]` bridge;
- IndexedDB JavaScript module;
- browser preferences;
- static host page/styles/runtime configuration;
- `BrowserContactRepository`.

### Browser repository guarantees

`BrowserContactRepository`:

- loads the complete local contact snapshot from IndexedDB;
- rejects malformed serialized state / duplicate contact IDs;
- preserves every current rich aggregate field and ID;
- implements Search/Favorite/Archived/Tag/Group/A-Z filtering;
- serializes writes behind a `SemaphoreSlim`;
- snapshots current in-memory state before mutation;
- persists one replacement state through IndexedDB;
- restores pre-write in-memory state if persistence fails;
- requires both reviewed contacts for duplicate merge;
- deep-copies values crossing repository boundaries.

It does not claim cross-tab conflict synchronization. Multi-tab concurrent editing remains a documented future capability rather than an implicit promise.

### Browser backup/encryption boundary

WebAssembly does not have a native `contactcore.db`. Shared capabilities therefore report:

```text
SupportsDatabaseBackups = false
SupportsDatabaseEncryption = false
```

Browser users can export CSV/vCard portable copies, with the existing documented fidelity limitations. Clearing site data/private profiles/origin changes/storage eviction can remove browser-local state.

Native SQLCipher/SQLite backup language is intentionally not reused for browser IndexedDB.

## Central package/build integration

`Directory.Packages.props` now centrally versions:

```text
Avalonia
Avalonia.Desktop
Avalonia.Android
Avalonia.iOS
Avalonia.Browser
Avalonia.Themes.Fluent
CommunityToolkit.Mvvm
Microsoft.Data.Sqlite 10.0.11
MSTest/test SDK/coverage packages
```

The earlier SQLite advisory blocker remains resolved by Microsoft.Data.Sqlite 10.0.11 rather than the vulnerable resolved SQLitePCLRaw 2.1.11 line.

## CI architecture

`.github/workflows/ci.yml` now contains:

### `core-build-test`

Ubuntu + Windows + macOS:

```text
restore ContactCore.Core.slnx
format --verify-no-changes
Release build
MSTest + XPlat Code Coverage
upload TestResults
```

### `browser-build`

Ubuntu:

```text
install wasm-tools
restore ContactCore.Browser
Release build ContactCore.Browser
```

### `android-build`

Ubuntu:

```text
install android workload
restore ContactCore.Android
Release build ContactCore.Android
```

### `ios-build`

macOS:

```text
install ios workload
restore ContactCore.iOS
Release build ContactCore.iOS
```

CI keeps read-only repository permissions and obsolete same-PR runs are cancelled by concurrency policy.

## CodeQL

CodeQL remains C# analysis with current action major versions/minimal permissions, but now restores/builds `ContactCore.Core.slnx` so the Linux security-analysis runner does not depend on unrelated Android/iOS/WebAssembly workloads.

Android/iOS/Browser compilation is independently enforced by CI.

## Release automation

The tag-driven release now has:

### Preflight

- setup from `global.json`;
- resolve project `Version`;
- reject tag unless exactly `v<Version>`.

### Six desktop packages

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-win-arm64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-linux-arm64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
```

### Browser package

```text
contactcore-v2.0.12-browser-wasm.zip
```

### Mobile release gate

- Android Release build must succeed;
- iOS Release build must succeed;
- production mobile package signing is intentionally not done without secure maintainer credentials.

### Final release

The final release waits for desktop/browser/mobile build jobs, downloads packaged desktop/browser assets, produces `SHA256SUMS.txt`, and only that job receives `contents: write`.

Artifacts are not claimed as signed/notarized/store-certified.

## Existing core product/data-safety work preserved

The cross-platform continuation preserves the already-hardened 2.0.12 behavior:

- rich aggregate contact model;
- stable root/child identities;
- safe shared group/tag rename-as-reassignment;
- explicit unsaved draft state;
- race-safe search;
- literal SQLite wildcard handling;
- batch import validation/atomic native persistence;
- hardened CSV/vCard parsing/warnings;
- duplicate evidence/survivor selection;
- stale-safe duplicate merge;
- native verified backup/restore staging/rollback;
- runtime-only native database key;
- fail-closed requested native cipher verification;
- permanent-delete/restore/merge confirmation safeguards;
- path/error privacy hardening;
- existing four behavioral test projects.

## Documentation synchronized

Cross-platform documentation now includes/synchronizes:

- `README.md`;
- `CHANGELOG.md`;
- `ROADMAP.md`;
- `docs/README.md`;
- `docs/platform-support.md`;
- `docs/setup.md`;
- `docs/architecture.md`;
- `docs/ci-cd.md`;
- `docs/release.md`;
- `docs/testing.md`;
- `docs/storage-backup-recovery.md`;
- `docs/security.md`;
- `docs/troubleshooting.md`;
- `docs/repository-reference.md`;
- this handoff.

### Repository inventory

The previous canonical reference documented 94 tracked files. This continuation adds exactly 30 tracked files and deletes none:

```text
1  ContactCore.Core.slnx
1  docs/platform-support.md
9  ContactCore.UI files
2  ContactCore.Native files
3  ContactCore.Android files
4  ContactCore.iOS files
10 ContactCore.Browser files
--
30 additions
```

Current canonical inventory: **124 tracked files**.

## Verification boundary

The coding execution environment used for this continuation does not contain a usable .NET toolchain and outbound network/DNS is unavailable there, so local restore/build/test results are **not invented**.

GitHub Actions is the authoritative verification environment.

During the cross-platform work, every code/document commit started a new PR #4 CI/CodeQL attempt and the concurrency policy correctly cancelled/superseded obsolete queued attempts. The final exact-head run must be the one associated with the commit containing this handoff.

Required merge gate:

- `core-build-test` — Ubuntu success;
- `core-build-test` — Windows success;
- `core-build-test` — macOS success;
- Browser Release build success;
- Android Release build success;
- iOS Release build success;
- CodeQL success/no unresolved newly introduced actionable finding;
- exact final PR #4 head, not an older run.

If GitHub Actions finds an actionable compiler/workload/analyzer/test defect, fix it on PR #4 and repeat exact-head verification. Do not merge based on queued/cancelled/superseded runs.

## Remaining non-blocking roadmap

The following are deliberately **not** represented as completed by this cross-platform change:

- drag/drop repeated-field ordering;
- global group/tag taxonomy management/orphan cleanup;
- general undo UX;
- deeper native restore failure injection;
- automated real-IndexedDB browser test harness;
- cross-tab browser concurrency/conflict resolution;
- broader native Avalonia/device integration automation;
- representative manual phone/tablet/browser accessibility/lifecycle audits;
- high-scale benchmarks/duplicate-candidate optimization;
- production SQLCipher + OS secret-store integration;
- Windows/macOS code signing/notarization;
- Android production keystore/store publishing;
- Apple signing/provisioning/App Store publishing;
- installer/package-manager/store formats beyond current desktop/browser packages/mobile build targets.

These are future release/quality/product tasks, not hidden missing implementation in the current cross-platform source architecture.

## Merge/release procedure

1. Keep PR #4 as the only authoritative integration path.
2. Allow CI/CodeQL to complete on the exact handoff commit.
3. Fix any actionable failures on the same branch with small commits.
4. Repeat exact-head checks after every code/workflow change.
5. Merge PR #4 only after required checks are green and documentation still matches the final head.
6. Pull/update `main` and confirm 2.0.12 metadata.
7. Create/push `v2.0.12` only from the intended verified merged release commit.
8. Confirm six desktop packages, browser WASM ZIP, mobile build gates, and `SHA256SUMS.txt` in the release workflow.
9. Do not claim Android/iOS store packages until a secure signing/provisioning pipeline actually produces them.

## Final cross-platform posture

ContactCore 2.0.12 now has a deliberate cross-platform architecture and source/build path for:

**Windows, Linux, macOS, Android, iPhone, iPad, and modern WebAssembly-capable browsers**, with ChromeOS covered through browser and compatible Android routes.

Native targets use SQLite; browser uses IndexedDB. Platform differences are surfaced through capability metadata instead of hidden behind inaccurate feature claims.
