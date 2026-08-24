# ContactCore Documentation

This directory contains the deep technical, user, maintainer, platform, testing, and release documentation for ContactCore **2.0.12**.

The project is a local-first contact manager with native SQLite persistence on Desktop/Android/iOS and IndexedDB persistence on Browser/WebAssembly. Source/build support is intentionally separated from production signing, store publishing, and manual certification claims.

## Start here

- [`../README.md`](../README.md) — project overview, platform matrix, build commands, privacy/release posture.
- [`platform-support.md`](platform-support.md) — canonical definition of Windows/Linux/macOS/Android/iPhone/iPad/Browser/ChromeOS support and distribution boundaries.
- [`setup.md`](setup.md) — source setup, workloads, paths, and first build.
- [`user-guide.md`](user-guide.md) — end-user workflows.
- [`architecture.md`](architecture.md) — modules, platform composition, persistence/data flows, design rules.
- [`repository-reference.md`](repository-reference.md) — canonical file-by-file inventory (**132 tracked files**).
- [`../what_changed.md`](../what_changed.md) — continuation/audit/release-gate handoff ledger.

## Engineering and data

- [`development.md`](development.md) — contributor engineering workflow and invariants.
- [`data-model.md`](data-model.md) — contact aggregate, relational mapping, identities, indexes, migrations.
- [`desktop-ui.md`](desktop-ui.md) — mature desktop shell/editing/dialog behavior.
- [`import-export.md`](import-export.md) — CSV/vCard contracts, fidelity and parser safety.
- [`storage-backup-recovery.md`](storage-backup-recovery.md) — native SQLite storage, verified backup, staged restore and rollback.
- [`security.md`](security.md) — engineering threat model and controls.
- [`performance.md`](performance.md) — current complexity, non-claims and benchmark priorities.
- [`accessibility.md`](accessibility.md) — keyboard/focus/theme/reduced-motion/manual accessibility checks.
- [`troubleshooting.md`](troubleshooting.md) — safety-first diagnosis.

## Quality and release

- [`testing.md`](testing.md) — Domain/Application/Infrastructure/portable-UI/Desktop behavioral suites and manual test boundaries.
- [`ci-cd.md`](ci-cd.md) — three-OS core CI, Browser/Android/iOS build gates, CodeQL, release automation and exact-head rules.
- [`release.md`](release.md) — version/tag preflight, packages, mobile source gates, signing boundaries and release process.
- [`release-smoke-test.md`](release-smoke-test.md) — repeatable exact-SHA manual release evidence record.
- [`maintainer-guide.md`](maintainer-guide.md) — maintainer invariants for data, UI/AOT, dependencies, CI and release governance.

## Architecture decisions

- [`adr/0001-modular-monolith.md`](adr/0001-modular-monolith.md) — modular-monolith layering.
- [`adr/0002-sqlite-persistence.md`](adr/0002-sqlite-persistence.md) — native SQLite persistence.
- [`adr/0003-encryption-provider.md`](adr/0003-encryption-provider.md) — optional SQLCipher-compatible provider boundary.

## Platform/AOT note

Browser/WebAssembly keeps trimming/AOT diagnostics meaningful and uses source-generated JSON metadata plus typed compiled shared-UI bindings.

The public iOS gate builds the explicit `iossimulator-arm64` RID after selecting the compatible Xcode 26.0 toolchain. The iOS project uses a simulator-only `TrimMode=copy` policy after application-owned trim hazards were removed. This simulator source/runtime gate is not a substitute for future signed-device/App Store trimming, provisioning, distribution, or representative hardware verification.

Android likewise has a source/build gate but no fabricated production keystore/store-publishing claim.

## Documentation rules

When implementation changes, update the relevant canonical documentation in the same change. In particular:

- platform behavior → `platform-support.md`, setup/architecture/CI/release as relevant;
- persistence/schema/backup behavior → data model/storage/security/testing;
- import/export behavior → `import-export.md` and tests;
- UI/accessibility behavior → desktop/user/accessibility/testing docs;
- workflow/release behavior → `ci-cd.md`, `release.md`, changelog/handoff;
- tracked file tree → `repository-reference.md` inventory;
- release candidate evidence → a fresh copy of `release-smoke-test.md` bound to the exact SHA.

Do not mark a manual device/browser/accessibility test complete unless it was actually executed. Do not claim signing/notarization/store certification without real protected credentials and a verified distribution pipeline.

## Privacy rule for examples/evidence

Use fictional/disposable contacts and profiles. Do not publish real contact databases, backups, exports, credentials, signing material, private endpoints, or screenshots containing personal information in documentation, issues, PRs, or release evidence.
