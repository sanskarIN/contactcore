# ContactCore Roadmap

This roadmap distinguishes **implemented/verified source capabilities** from **future work** and from tasks that cannot be truthfully completed without external credentials or manual representative-environment testing.

## Current release line — 2.0.12

### Completed product/data foundations

- [x] Layered Domain/Application/Infrastructure architecture.
- [x] Mature Avalonia Desktop shell for Windows/Linux/macOS.
- [x] Rich contact aggregate: names, nickname, birthday, notes, favorite/archive, phones, emails, addresses, organizations, groups, tags.
- [x] Stable root/contact-owned identities through ordinary edits.
- [x] Safe shared group/tag per-contact reassignment semantics.
- [x] Exact delimiter-containing group/tag names through independent editor rows.
- [x] Native SQLite migrations, foreign keys, indexes, aggregate persistence, transactional bulk import.
- [x] Literal wildcard escaping for native SQLite search.
- [x] Debounced/cancellation-safe portable search.
- [x] Duplicate evidence/review, explicit survivor direction, confirmation and stale-safe atomic merge.
- [x] Conservative country-code-aware phone equivalence shared by duplicate scoring and merge de-duplication.
- [x] CSV/focused-vCard import/export with hardened parsing and explicit spreadsheet-safety boundary.
- [x] Native verified SQLite backup plus staged/verified restore and rollback path.
- [x] Runtime-only database-key request and fail-closed requested cipher verification.
- [x] Local preferences with safe defaults and source-generated JSON metadata for native/mobile AOT safety.
- [x] Privacy-conscious diagnostic redaction/bounds.

### Completed cross-platform source/build architecture

- [x] `ContactCore.UI` portable Avalonia presentation layer.
- [x] Typed compiled production bindings for the shared portable `MainView`.
- [x] `ContactCore.Native` native SQLite composition.
- [x] Android `net10.0-android` application head.
- [x] iOS/iPadOS `net10.0-ios` application head.
- [x] Browser/WebAssembly `net10.0-browser` application head.
- [x] Browser `IContactRepository` implementation backed by IndexedDB.
- [x] Browser source-generated JSON metadata and disposable persistence gate.
- [x] Browser failure rollback to the previous in-memory snapshot.
- [x] Browser capability boundary: no false native SQLite backup/encryption claim.
- [x] ChromeOS support documented through real Browser/Android routes rather than a fabricated native target.

### Completed automated quality/release hardening

- [x] Workload-free `ContactCore.Core.slnx` for normal three-OS CI and CodeQL.
- [x] Complete `ContactCore.slnx` containing all platform heads/tests.
- [x] Five behavioral test projects: Domain, Application, Infrastructure, portable UI, Desktop.
- [x] XPlat coverage collector available to all five test projects.
- [x] Portable search debounce/cancellation regression tests.
- [x] Portable delete/restore confirmation regression tests.
- [x] Native post-switch restore rollback regression coverage.
- [x] Ubuntu/Windows/macOS core restore/format/Release build/test matrix.
- [x] Browser/WebAssembly Release build gate with application-owned AOT/trim hazards removed.
- [x] Android `android-arm64` Release build gate.
- [x] iOS `iossimulator-arm64` Release build gate with explicit Xcode 26.0 selection.
- [x] Simulator-only iOS trim boundary documented after application-owned JSON/XAML trim hazards were removed.
- [x] CodeQL C# analysis on the workload-free core solution.
- [x] Version/tag equality preflight for releases.
- [x] Six desktop release RIDs plus Browser WebAssembly ZIP.
- [x] Android/iOS source build gates before final GitHub Release creation.
- [x] SHA-256 checksum generation for downloadable archives.
- [x] Least-privilege release permissions.
- [x] Repeatable exact-SHA release smoke-test record template.
- [x] Canonical repository reference regenerated to 132 tracked files after final AOT hardening additions.

## Immediate release completion boundary

These are process requirements, not missing application code:

- [ ] Obtain **green CI + CodeQL on the exact final PR #4 synthetic merge candidate** after the final documentation commit.
- [ ] Merge PR #4 into `main` only after that exact-head gate is green.
- [ ] Apply/verify `main` branch protection/ruleset requiring the stable CI/CodeQL checks.
- [ ] Execute and archive the manual `docs/release-smoke-test.md` record against the actual candidate/artifacts, explicitly marking anything not executed.
- [ ] Create `v2.0.12` only from the intended verified merged commit.
- [ ] Verify release workflow output: six desktop archives, Browser ZIP, mobile source-build gates, and `SHA256SUMS.txt`.

Do not convert an older/cancelled workflow into release evidence after the final head changes.

## Product/UX future work

### Repeated-field ordering

- [ ] Add accessible reorder controls for phone/email/address/organization rows.
- [ ] Define whether persisted order is user-significant for all repeated-field categories.
- [ ] Add keyboard/touch-friendly reorder behavior and regression tests.

### Global group/tag taxonomy management

- [ ] Add a dedicated groups/tags management surface.
- [ ] Define global rename semantics explicitly.
- [ ] Define delete/orphan cleanup semantics explicitly.
- [ ] Add relationship-count/safety confirmation before global destructive operations.
- [ ] Add migration/persistence/UI tests for taxonomy operations.

### Undo/recovery UX

- [ ] Design a general undo strategy for high-impact contact edits/deletes/merges.
- [ ] Avoid presenting undo as durable recovery unless its persistence semantics justify that claim.
- [ ] Integrate with existing confirmation/backup safeguards rather than weakening them.

## Browser future work

### Real IndexedDB automation

- [ ] Build a real-browser harness that boots the WebAssembly app or repository interop against actual IndexedDB.
- [ ] Verify reload persistence, malformed state, transaction failure, storage blocked/denied behavior, and origin/profile assumptions.
- [ ] Keep test fixtures synthetic and disposable.

### Cross-tab/conflict behavior

- [ ] Define expected behavior when multiple tabs modify the same stored snapshot.
- [ ] Add versioning/conflict detection or another explicit strategy before claiming robust multi-tab editing.
- [ ] Add automated tests for the chosen model.

## Native backup/restore future resilience

- [x] Stage/verify before active replacement.
- [x] Verified pre-restore recovery snapshot.
- [x] Post-switch verification and rollback path.
- [x] Regression coverage for forced post-switch verification failure.
- [ ] Add deeper failure injection around staging copy, temp cleanup, sidecar cleanup, backup-copy failures, and rollback-copy failures.
- [ ] Define user-visible recovery guidance for the remaining rare multi-failure branches.

## Performance/scale roadmap

- [ ] Add reproducible benchmarks for 100 contacts.
- [ ] Add reproducible benchmarks for 1,000 contacts.
- [ ] Add reproducible benchmarks for 10,000 contacts.
- [ ] Measure SQL statement amplification during aggregate saves/imports.
- [ ] Benchmark Browser full-snapshot IndexedDB writes.
- [ ] Evaluate list projection/pagination before large-list claims.
- [ ] Evaluate duplicate candidate generation beyond pairwise in-memory scans.
- [ ] Evaluate FTS5 only with an ADR covering migration/index synchronization/recovery.
- [ ] Evaluate streaming CSV/vCard while preserving whole-batch validation/atomicity guarantees.

## Encryption/secrets roadmap

The public repository currently has a fail-closed **provider boundary**, not a claim that the ordinary SQLite build is encrypted at rest.

- [ ] Select a production-supported SQLCipher-compatible provider if encrypted-at-rest native distribution is chosen.
- [ ] Review provider license/native packaging for Windows/Linux/macOS/Android/iOS.
- [ ] Add encrypted database create/open/migration tests.
- [ ] Add encrypted backup/restore tests.
- [ ] Add native OS credential/secret-store abstraction.
- [ ] Expose user-visible verified encryption state only when runtime verification can prove it.

## Accessibility/manual quality roadmap

Automated source/view-model tests are not a substitute for representative platform accessibility testing.

- [ ] Desktop keyboard/focus audit on representative Windows/Linux/macOS builds.
- [ ] Desktop screen-reader checks where applicable.
- [ ] High-DPI/scaling/theme audit.
- [ ] Android TalkBack/touch/input/orientation/lifecycle audit.
- [ ] iPhone/iPad VoiceOver/touch/input/orientation/lifecycle audit.
- [ ] Browser keyboard/screen-reader/zoom/storage-profile audit on representative engines.
- [ ] Add stable automation for accessibility/lifecycle behavior where the platform tooling supports trustworthy checks.

## Packaging/signing/distribution roadmap

These tasks require real maintainer-controlled credentials/policies and must not be fabricated in public source.

### Windows

- [ ] Choose installer/package format if desired.
- [ ] Add protected Authenticode signing pipeline.
- [ ] Add installer signing/verification documentation.
- [ ] Consider package-manager distribution after signed artifacts exist.

### macOS

- [ ] Add Developer ID signing.
- [ ] Add notarization/stapling.
- [ ] Verify Intel/Apple Silicon signed artifacts on representative systems.
- [ ] Consider DMG/pkg/Homebrew distribution after policy is defined.

### Linux

- [ ] Evaluate AppImage/Flatpak/Snap/deb/rpm based on actual distribution goals.
- [ ] Add appropriate package signing where applicable.

### Android

- [ ] Configure a real protected production keystore/signing policy.
- [ ] Produce/verify production AAB/APK as appropriate.
- [ ] Complete Play Store listing/privacy/data-safety requirements.
- [ ] Perform representative signed-package installation/device tests.

### iPhone/iPad

- [ ] Configure real Apple signing certificates/provisioning/App Store credentials.
- [ ] Add a protected signed **device** build pipeline with the intended production trim/link policy.
- [ ] Verify application-owned and third-party linker behavior in that actual distribution configuration.
- [ ] Complete TestFlight/App Store validation and representative physical-device checks.

## Documentation roadmap

- [x] Platform support guide.
- [x] Setup/development guides.
- [x] Architecture/data model guides.
- [x] Desktop/user/import-export/storage/security/accessibility/performance/testing/troubleshooting guides.
- [x] CI/CD and release guides.
- [x] Release smoke-test record template.
- [x] Maintainer guide.
- [x] ADRs for architecture/SQLite/encryption-provider boundary.
- [x] Canonical file-by-file repository reference.
- [x] Detailed `what_changed.md` continuation ledger.
- [ ] Keep all canonical docs synchronized as future behavior changes.

## Principles for future roadmap completion

- Do not call a roadmap item complete merely because a stub/project/file exists.
- Require behavioral verification appropriate to the risk of the feature.
- Prefer false-negative duplicate detection over destructive false-positive merges.
- Keep storage/recovery operations transactional or safely recoverable.
- Keep real user data out of tests/issues/docs/release evidence.
- Keep signing credentials/secrets out of source.
- Preserve the distinction between source/build support, automated verification, manual representative testing, and signed/store distribution.
