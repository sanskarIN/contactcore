# Repository Reference

This is the file-by-file reference for ContactCore. It documents every tracked file at this documentation checkpoint, including configuration, GitHub automation, policies, source code, tests, assets, and documentation.

When a file is added, renamed, moved, or removed, update this reference in the same change. Generated directories such as `bin/`, `obj/`, `TestResults/`, local databases, backups, and ignored secret files are intentionally not part of the tracked-file inventory.

## Repository root and toolchain

### `.editorconfig`

Repository editor/formatting conventions for text/C# files. It supports consistent local/editor formatting and complements `dotnet format`. Changes can affect broad diffs, so avoid mixing style-policy changes with unrelated feature work.

### `.env.example`

Documents optional `CONTACTCORE_DATA_PATH` and `CONTACTCORE_DATABASE_KEY` environment-variable names. It must contain no real secret. The data-path value means a directory; the key is runtime-only and should not be placed in this tracked example.

### `.gitattributes`

Git attribute rules for line-ending/text handling. Keep this stable to avoid accidental whole-repository line-ending rewrites.

### `.gitignore`

Excludes build output, IDE/user files, local environment/secret files, database/runtime artifacts, and other generated data defined there. Update it when new sensitive/generated artifact types are introduced.

### `ContactCore.slnx`

The .NET solution definition. It contains four production projects and four test projects:

- Domain
- Application
- Infrastructure
- Desktop
- corresponding Domain/Application/Infrastructure/Desktop test projects

CI/format/build/test commands operate on this solution.

### `Directory.Build.props`

Shared MSBuild policy for projects: `net10.0`, latest C# language version, nullable/implicit usings, warnings-as-errors, latest-recommended analysis, deterministic build, and CI build metadata.

A change here affects nearly every project and should receive full solution/CI verification.

### `Directory.Packages.props`

Central NuGet package versions. Current packages include Avalonia, Avalonia.Desktop, Fluent theme, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, Microsoft.NET.Test.Sdk, MSTest, and coverlet collector.

Prefer changing dependency versions here instead of scattering versions through project files.

### `global.json`

Pins SDK baseline `10.0.100`, allows `latestFeature` roll-forward, and disables prerelease SDK resolution. CI uses this file directly; release automation currently requests .NET `10.0.x` separately.

## Repository presentation, policy, and governance

### `README.md`

Primary project landing page. Summarizes offline-first purpose, actual current capabilities, compact-editor preservation/full-rich-editing distinction, duplicate-UI limitation, supported release RIDs, toolchain, quick-start commands, data/security notes, documentation links, contribution/license/support/funding information.

It must not overclaim encryption, signing, accessibility certification, test coverage, or rich editor behavior.

### `CHANGELOG.md`

Human-readable release/change history. Records current hardening, rich-field preservation fix, test additions, known limitations, and release/documentation changes under `Unreleased` until a version is tagged.

### `ROADMAP.md`

Forward-looking priorities. It marks compact rich-field preservation/tests complete and keeps full multi-value editing, explicit unsaved-draft state, duplicate merge UX, additional resilience tests, performance, encryption-provider maturity, and release signing/audits as future work.

### `CONTRIBUTING.md`

Contributor entry point: branch/quality/test/documentation expectations and contribution hygiene. The deeper engineering rules live in `docs/development.md` and `docs/maintainer-guide.md`.

### `CODE_OF_CONDUCT.md`

Community behavior expectations and enforcement/contact guidance, including privacy-respecting handling of accidentally shared contact data. Keep contribution/community interactions consistent with this policy.

### `SECURITY.md`

Public vulnerability-reporting policy and supported-version/security-contact guidance. It routes undisclosed vulnerabilities away from public issues and remains consistent with `docs/security.md`.

### `PRIVACY.md`

User-facing privacy posture: local/offline-first data handling, exports/backups, temporary files, runtime-key behavior, no mandatory telemetry/cloud, and responsibilities around local files. Update when any remote service or data collection is introduced.

### `SUPPORT.md`

Support channels and privacy-minimized diagnostic guidance. It discourages posting real contact databases/exports/backups/keys publicly and routes security reports to `SECURITY.md`.

### `LICENSE`

MIT license text governing repository code distribution/use subject to its terms. Do not modify casually; dependency/native-provider licenses remain separate concerns.

### `what_changed.md`

Continuation/handoff ledger for repository work. It records branch/PR/checkpoint, completed implementation/docs, commit history/verification state, exact known limitations, and next work. It must distinguish pending CI from passed CI.

## GitHub community/configuration

### `.github/FUNDING.yml`

GitHub funding configuration for project sponsorship/support links.

### `.github/ISSUE_TEMPLATE/bug_report.yml`

Structured bug-report form. Helps request reproducible environment/behavior details while avoiding uncontrolled free-form reports. Any request for diagnostics should remain privacy-safe.

### `.github/ISSUE_TEMPLATE/feature_request.yml`

Structured feature-request form for user problem/use case/proposal context.

### `.github/pull_request_template.md`

PR checklist/template for describing changes, tests, docs, and review information. Keep aligned with `CONTRIBUTING.md` and current quality gates.

### `.github/dependabot.yml`

Dependabot update configuration. Automated dependency proposals still require compatibility/security/license review and normal CI.

## GitHub Actions

### `.github/workflows/ci.yml`

Cross-platform quality workflow for pushes/PRs involving `main`. Runs restore, format verification, Release build, and full tests with XPlat coverage on Ubuntu, Windows, and macOS. Uploads per-OS TestResults and cancels obsolete runs for the same PR/ref.

### `.github/workflows/codeql.yml`

C# CodeQL workflow for pushes/PRs to `main` plus a weekly schedule. Uses least-required `contents: read` / `security-events: write` permissions and cancels obsolete runs.

### `.github/workflows/release.yml`

Tag-triggered (`v*.*.*`) release workflow. Tests and self-contained single-file publishes for `win-x64`, `linux-x64`, `osx-x64`, and `osx-arm64`, then attaches artifacts to a GitHub Release. It currently does not implement/document code signing or notarization.

## Documentation hub and guides

### `docs/README.md`

Documentation index and navigation hub for users, contributors, maintainers, reviewers, and security auditors. Also states documentation principles: code is authoritative, do not overclaim verification, privacy-safe examples, failure-path documentation, and file-level traceability.

### `docs/user-guide.md`

End-user workflows for startup, compact editor scope and rich-field preservation, search/filters, favorites/archive/delete, duplicate detection, import/export, backup/restore, settings, paths, encryption, privacy habits, shortcuts, and accessibility notes.

### `docs/setup.md`

Source setup/installation guide: SDK resolution, clone/restore/build/run/test, data directory override, runtime key behavior, platform notes, disposable development profiles, and IDE/CLI parity.

### `docs/architecture.md`

Detailed layered architecture, dependency direction, startup/read/write/import/backup/restore flows, compact-editor aggregate-preservation invariant, schema ownership, connection/preferences/platform-service/error boundaries, security principles, test architecture, and evolution rules.

### `docs/data-model.md`

Domain-to-SQLite mapping reference for contact scalar fields, child records, groups/tags, tables, relationships, indexes, timestamps, normalization, transactional write model, schema identity, and migrations.

### `docs/desktop-ui.md`

Exact current Avalonia UI/view-model behavior: composition, three-column layout, search/debounce, browse modes, compact draft fields, preservation of unexposed rich data, destructive actions, unsaved-draft delete caveat, duplicate-command limitation, data tools, file-picker limits, settings, shortcuts, focus/style, callbacks, error handling, and UI-test priorities.

### `docs/import-export.md`

CSV/vCard codec contract: supported columns/properties, escaping/parsing, warnings, validation/atomic persistence, field-fidelity limits, Unicode/input considerations, spreadsheet-formula caveat, duplicate behavior, and requirements for new formats.

### `docs/storage-backup-recovery.md`

Local paths, SQLite connection policy, keyed-SQLite boundary, aggregate atomicity, backup verification, complete staged restore/rollback sequence, recovery artifacts, non-guarantees, operational recommendations, and storage-change checklist.

### `docs/security.md`

Engineering threat model and controls: assets/trust boundaries, offline-first posture, SQL safety, schema identity, backup/restore controls, optional encryption, secret handling, import limits, CSV risk, redaction limitations, temporary files, dependency/release security, unmitigated threats, review checklist.

### `docs/accessibility.md`

Implemented keyboard/focus/theme/reduced-motion/text behavior; risks/limitations; Windows/macOS/Linux manual test matrices; keyboard/screen-reader/scaling scenarios; contributor requirements; boundaries of automated accessibility testing.

### `docs/performance.md`

Current search/loading/duplicate/import/export/backup characteristics, known scaling costs, explicit non-claims, benchmark scenarios, optimization priorities, profiling guidance, and regression review rules.

### `docs/development.md`

Contributor engineering workflow: shared build policy, package management, layer placement, complete-aggregate preservation, migration/import/UI/security rules, formatting/quality commands, test placement, Git discipline, PR requirements, documentation definition of done.

### `docs/testing.md`

Test architecture and concrete coverage map for every test source file, including compact-editor rich-child preservation tests, plus recommended missing cases, deterministic/temp-data rules, coverage policy, manual release verification matrix, CI-only failure diagnosis, and regression-test process.

### `docs/ci-cd.md`

CI, CodeQL, release workflow triggers/matrices/permissions/concurrency/artifacts, quality gate, SDK consistency, dependency automation, failure diagnosis, and workflow-change checklist.

### `docs/release.md`

End-to-end release process: current RIDs, tag trigger, pre-release checks, local quality pass, publishing sequence, SDK consistency, artifact smoke tests, signing/notarization truth, schema compatibility, notes/screenshots, failure/rollback/post-release guidance.

### `docs/troubleshooting.md`

Safety-first diagnosis for SDK/build/format/startup, encryption mismatch, paths/preferences/search, current rich-field preservation plus older-build regression recovery, unsaved-draft delete UX, imports, backups/restores/recovery artifacts, duplicate/theme/motion behavior, platform CI issues, diagnostic bundle, and last-resort disposable-profile reset.

### `docs/maintainer-guide.md`

Long-term ownership guide: invariants, branch/review, fields/migrations, backup/restore, encryption, preferences, import/export, compact-editor preservation/full-rich-editor evolution, search/duplicates/accessibility, dependencies, actions, releases, security reports, documentation and repository hygiene, deprecation.

## Architecture decision records

### `docs/adr/0001-modular-monolith.md`

Accepted decision to keep ContactCore a layered modular monolith rather than one coupled project or distributed services. Records context, project responsibilities, consequences, alternatives, guardrails, and revisit conditions.

### `docs/adr/0002-sqlite-persistence.md`

Accepted decision to use local SQLite behind Application abstractions. Records connection policy, migrations, aggregate mapping/search, backup interaction, tradeoffs, alternatives, guardrails, and revisit criteria.

### `docs/adr/0003-encryption-provider.md`

Accepted decision to keep encryption-provider integration optional but fail closed when a runtime key is requested. Records provider/key/backup implications, secret-persistence decision, tradeoffs, alternatives, guardrails, and future revisit triggers.

## Domain source

### `src/ContactCore.Domain/ContactCore.Domain.csproj`

Minimal Domain project definition. Relies on shared repository build settings and intentionally has no framework/infrastructure package dependency.

### `src/ContactCore.Domain/ContactModels.cs`

Core contact types: `ContactFieldKind`, phone/email/address/organization/group/tag records, and `Contact`. Implements display-name fallback and `DeepCopy`. Changes here can affect persistence, codecs, merge behavior, UI, and migrations. `Id` and `CreatedAt` are init-only identity/history fields, so callers constructing transformed contacts must supply them during initialization.

### `src/ContactCore.Domain/ContactValidation.cs`

Domain validation policy for name/note lengths, email format/length, and phone syntax/length. Returns field-oriented `ValidationIssue` records and deliberately avoids echoing invalid phone/email values in messages.

### `src/ContactCore.Domain/TextNormalizer.cs`

Normalization helpers used in duplicate matching/merge: accent-insensitive/lowercase Unicode search key and digits-only phone key. Changes can alter duplicate behavior and require scoring/regression review.

## Application source

### `src/ContactCore.Application/ContactCore.Application.csproj`

Application project definition and Domain project reference.

### `src/ContactCore.Application/Abstractions.cs`

Cross-layer contracts and query model: `ContactQuery`, `IContactRepository`, `IBackupService`, and `IAppPreferences`. Infrastructure implements these contracts; interface changes affect multiple projects/tests.

### `src/ContactCore.Application/ContactService.cs`

Use-case boundary for initialization/search/save/import/favorite/archive/delete. Normalizes user data, updates timestamps, validates before persistence, performs whole-batch import validation, and delegates to repository abstractions.

### `src/ContactCore.Application/DuplicateDetector.cs`

Duplicate candidate scoring/comparison and deterministic `ContactMerger`. Includes threshold clamping, null/self-merge safeguards, normalized equality, de-duplication, fresh IDs for copied child records, note/favorite merge behavior, and timestamp update.

### `src/ContactCore.Application/ImportExport.cs`

`ImportResult`, `ContactCsvCodec`, and `VCardCodec`. Handles focused CSV/vCard serialization/parsing and warnings. Does not persist by itself; service/repository perform normalization/validation/atomic write.

## Infrastructure source

### `src/ContactCore.Infrastructure/ContactCore.Infrastructure.csproj`

Infrastructure project definition. References Domain/Application and concrete SQLite dependency as configured centrally.

### `src/ContactCore.Infrastructure/AppPaths.cs`

Resolves/creates the ContactCore data directory and derives active database, settings, and backup paths. Honors optional directory override and fallback behavior.

### `src/ContactCore.Infrastructure/SqliteConnectionFactory.cs`

Single connection-configuration boundary. Controls paths, read/write/read-only mode, pooling, shared cache, foreign keys, busy timeout, and optional keyed-SQLite fail-closed `cipher_version` verification.

### `src/ContactCore.Infrastructure/DatabaseMigrator.cs`

Schema authority. Creates migration tracking, applies ordered transactional migrations, creates ContactCore relational schema/indexes and schema-family identity, rejects future schemas, and validates identity after migration.

### `src/ContactCore.Infrastructure/SqliteContactRepository.cs`

Concrete `IContactRepository`: count/get/search/delete, literal-wildcard escaping, parameterized filters, transactional single/bulk upsert, aggregate child/link replacement, group/tag relationship insertion, and aggregate materialization. Its replacement semantics are why upstream partial editors must preserve unedited child collections.

### `src/ContactCore.Infrastructure/BackupService.cs`

Verified backup/restore implementation. Uses SQLite backup API, integrity/schema/identity/version checks, pre-restore snapshot, staging migration/verification, pool/sidecar handling, active replacement, final verification, rollback and recovery artifacts.

### `src/ContactCore.Infrastructure/JsonAppPreferences.cs`

Local JSON implementation of `IAppPreferences`. Loads/saves theme/reduced-motion/delete-confirmation, normalizes themes, tolerates corrupt JSON with safe defaults, writes via temp/replace, and deliberately excludes runtime database key from persisted JSON.

### `src/ContactCore.Infrastructure/RedactingLog.cs`

Defense-in-depth sanitizer for UI-visible error text. Redacts email-shaped and long number/phone-shaped substrings and truncates to 2,000 characters. It is not a complete PII/secret scrubber.

## Desktop source and assets

### `src/ContactCore.Desktop/ContactCore.Desktop.csproj`

Avalonia executable project definition and project/package references. Packaging/publish changes here can affect all release RIDs.

### `src/ContactCore.Desktop/Program.cs`

Desktop process entry point that configures/starts the Avalonia application lifetime.

### `src/ContactCore.Desktop/App.axaml`

Avalonia application XAML resources/theme/style inclusion root.

### `src/ContactCore.Desktop/App.axaml.cs`

Desktop composition root. Creates paths/preferences/SQLite repository/service/backup/view model, applies initial/runtime theme selection, assigns MainWindow, and starts initialization.

### `src/ContactCore.Desktop/MainWindow.axaml`

Primary visual tree: top search/actions, browse sidebar, contact list, editor, Settings, Data tools, backup/restore/import/export controls, status/footer. The current editor binds one visible phone/email field even though the underlying aggregate can contain richer collections.

### `src/ContactCore.Desktop/MainWindow.axaml.cs`

Platform adapter for view-model delegates: native import/export/backup pickers, 5,000,000-character bounded UTF-8 import read, stream-backed restore temp copy, modal confirmation, and `Ctrl+N`/`Ctrl+S`/`Ctrl+F`/Escape shortcuts.

### `src/ContactCore.Desktop/ViewModels.cs`

`PickedTextFile`, list item/draft/main view models. Handles draft conversion, browse/search/debounce, selection, save, duplicate summary, import/export, backup creation, settings, status messaging, and refresh. `ContactDraftViewModel` retains a deep-copy baseline of the complete contact, constructs outgoing identity fields safely in an object initializer, copies every child collection, and then applies compact first-phone/first-email edits so unexposed data survives.

### `src/ContactCore.Desktop/DataSafetyCommands.cs`

Partial `MainWindowViewModel` destructive/recovery commands. Implements confirmation-aware permanent delete and confirmation-required restore plus temporary picker-file cleanup/status/refresh behavior. Current unsaved drafts already have generated IDs, so explicit new/persisted state remains a future UX refinement.

### `src/ContactCore.Desktop/ConfirmDialog.axaml`

Modal confirmation dialog visual definition used by delete/restore workflows.

### `src/ContactCore.Desktop/ConfirmDialog.axaml.cs`

Dialog code-behind that exposes the message and closes with affirmative/cancel result semantics.

### `src/ContactCore.Desktop/Styles/DesignSystem.axaml`

Central desktop styles for top/sidebar/list/detail/status surfaces, logo/avatar/settings cards, field/section/status text, primary/alphabet buttons, and visible focus borders using dynamic theme resources.

### `src/ContactCore.Desktop/Assets/logo.svg`

Tracked vector logo used by the desktop/project README. Keep it free of embedded private metadata and verify rendering across intended surfaces when changed.

## Domain tests

### `tests/ContactCore.Domain.Tests/ContactCore.Domain.Tests.csproj`

Domain MSTest project configuration and references.

### `tests/ContactCore.Domain.Tests/ContactValidationTests.cs`

Tests valid contact validation, invalid-email reporting, non-echoing validation messages, and search normalization for accents/case/whitespace.

## Application tests

### `tests/ContactCore.Application.Tests/ContactCore.Application.Tests.csproj`

Application MSTest project configuration and references.

### `tests/ContactCore.Application.Tests/DuplicateDetectorTests.cs`

Tests high-confidence shared email/name comparison, phone de-duplication, fresh IDs for copied secondary child rows, and self-merge rejection.

### `tests/ContactCore.Application.Tests/ImportExportTests.cs`

Tests CSV round trip with commas/quotes/newlines, focused vCard round trip, and seeded randomized-Unicode CSV parser robustness.

## Infrastructure tests

### `tests/ContactCore.Infrastructure.Tests/ContactCore.Infrastructure.Tests.csproj`

Infrastructure MSTest project configuration and references.

### `tests/ContactCore.Infrastructure.Tests/SqliteRepositoryTests.cs`

Temporary-database integration tests for aggregate child round trip/search, cascade delete, and rollback of the entire bulk upsert when a later child write fails.

### `tests/ContactCore.Infrastructure.Tests/BackupServiceTests.cs`

Temporary-database tests for verified restore, invalid-backup preservation of active data, legacy schema migration, future-schema rejection, and unique consecutive backup names.

### `tests/ContactCore.Infrastructure.Tests/JsonAppPreferencesTests.cs`

Tests non-persistence of database key, safe defaults after malformed JSON, and normalization of unsupported themes.

## Desktop tests

### `tests/ContactCore.Desktop.Tests/ContactCore.Desktop.Tests.csproj`

Desktop MSTest project configuration and reference to the desktop code for non-visual view-model testing.

### `tests/ContactCore.Desktop.Tests/ContactDraftViewModelTests.cs`

Tests preservation of ID/creation timestamp/favorite/archive flags, rejection of non-ISO birthday input, preservation of additional phones/emails/addresses/organizations/groups/tags while editing the visible primary phone/email, non-mutation of the loaded source aggregate, and retention of remaining values when the visible primary phone/email is cleared.

## Cross-file change map

Use this map when deciding what else must change with a source edit:

| Change | Usually review/update |
|---|---|
| Contact field/model | Domain validation/deep copy, migration, repository, merger, codecs, desktop draft/UI, tests, data-model docs |
| SQLite schema | Migrator, repository, backup identity/compatibility, tests, data/storage/security/release docs, changelog |
| Import format | Codec, service validation path, desktop size/file picker, tests, import/user/security docs |
| Backup/restore | BackupService, connection/migrator, DataSafetyCommands/MainWindow picker, tests, storage/security/troubleshooting docs |
| Preference | Abstraction, JsonAppPreferences, App/Main VM/XAML, tests, user/desktop/accessibility docs |
| Search | ContactQuery/service/repository, desktop debounce/filter, tests, performance/user docs |
| Desktop field | ViewModels/XAML/styles, aggregate-preservation logic, Application/Domain if behavior changes, desktop tests, accessibility/user docs |
| Dependency | Directory.Packages.props/project file, CI/release, tests, setup/security/license review |
| Workflow | `.github/workflows/*`, CI/release docs, permissions/security review |
| New/removed file | This repository reference and relevant documentation index/maintainer handoff |

## Verification note

This reference describes responsibilities and current implementation contracts; it does not mean every recommended test or roadmap feature is complete. Verification status belongs in CI/CodeQL, tests, release notes, and `what_changed.md`, not in file existence alone.
