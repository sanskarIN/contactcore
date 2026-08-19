# Roadmap

This roadmap distinguishes **implemented** behavior from future intent. A checked item means the capability exists in the repository at the current checkpoint; it does not automatically mean every release platform has been manually verified or that every edge case is complete.

## 0.1 — Foundation and MVP

- [x] Layered Domain/Application/Infrastructure/Desktop solution.
- [x] Avalonia desktop shell.
- [x] SQLite persistence and ordered migrations.
- [x] Contact create/edit/delete fundamentals.
- [x] Favorites and archive model/UI filters.
- [x] Search across names/phones/emails and A–Z navigation.
- [x] Rich domain/storage model for multiple phones/emails/addresses/organizations/groups/tags.
- [x] CSV and focused vCard 4.0 codecs.
- [x] Duplicate scoring and application-layer merge engine.
- [x] Verified SQLite-native backup creation.
- [x] Unit/integration test projects plus cross-platform CI definitions.
- [x] CodeQL and Dependabot repository automation.

## 0.2 — Data safety and desktop workflow hardening

- [x] Transactional bulk import (`UpsertManyAsync`) with rollback of successful prefix on failure.
- [x] Whole-batch normalization/validation before import persistence.
- [x] Literal SQL `LIKE` wildcard escaping for user search text.
- [x] Future-schema rejection.
- [x] ContactCore schema-family identity marker.
- [x] Read-only restore-source verification before active data changes.
- [x] Verified pre-restore recovery snapshot.
- [x] Staged restore migration and verification before switch.
- [x] Final active-database verification and rollback path.
- [x] Unique backup/recovery artifact names.
- [x] Runtime database key excluded from persisted preferences.
- [x] Atomic-ish preferences write through temp-file replacement.
- [x] Safe defaults for corrupted settings.
- [x] Permanent-delete confirmation preference, enabled by default.
- [x] Restore confirmation.
- [x] Native file-picker import/export UI.
- [x] Native/stream-backed backup picker handling.
- [x] 5,000,000-character desktop import bound.
- [x] Data tools view for import/export/backup/restore.
- [x] Dedicated Settings/About/privacy surface.
- [x] System/Light/Dark theme switching.
- [x] Reduced-motion preference persistence.
- [x] Desktop keyboard shortcuts and explicit visible focus styles.
- [x] Desktop draft regression test project.
- [x] Compact-editor preservation of additional phones/emails, addresses, organizations, groups, and tags.
- [x] Regression tests proving compact phone/email edits and clears preserve the rest of the rich aggregate.

## 0.3 — Documentation completeness

- [x] Documentation hub/index.
- [x] Deep user/setup/architecture/data-model guides.
- [x] Deep desktop UI guide with exact current limitations.
- [x] Import/export format and security limitations.
- [x] Storage/backup/recovery guide with failure paths.
- [x] Expanded threat model/security guide.
- [x] Expanded testing/accessibility/performance/CI/release/troubleshooting guides.
- [x] Maintainer engineering guide.
- [x] Expanded ADRs for modular monolith, SQLite, and encryption-provider boundary.
- [x] Exhaustive tracked-file repository reference.
- [ ] Keep root README/changelog/policy docs synchronized on every later behavior change.

## 0.4 — Rich UX completion

The data-loss risk from saving a rich contact through the compact editor is now protected by deep-copy preservation and regression tests. The remaining work is direct rich-field editing and stronger workflow UX.

- [ ] Full multi-value phone/email editor.
- [ ] Address editor.
- [ ] Organization editor.
- [ ] Group/tag assignment and management screens.
- [ ] Add/edit/remove/reorder tests for every rich field as controls are introduced.
- [ ] Explicit unsaved/new-contact state so destructive actions present as Cancel rather than permanent delete of a non-persisted ID.
- [ ] Interactive duplicate candidate list.
- [ ] Duplicate comparison/merge-preview dialog.
- [ ] User-confirmed merge workflow wired to `ContactMerger`.
- [ ] Undo/recovery UX for high-impact contact modifications where practical.

## 0.5 — Test and resilience expansion

- [ ] Test literal search characters `%`, `_`, and backslash.
- [ ] Complete tag/group/StartsWith repository filter tests.
- [ ] Full address/organization/group/tag repository round-trip tests.
- [ ] Restore rejection test for valid non-ContactCore SQLite file.
- [ ] Forced post-switch restore verification failure/rollback test.
- [ ] Restore staging/temp cleanup failure-path tests.
- [ ] Missing/same-active-path backup restore tests.
- [ ] ContactService normalization and indexed import-validation tests.
- [ ] Search debounce/cancellation view-model tests.
- [ ] Destructive-action and restore confirmation view-model tests.
- [ ] Native/Avalonia integration tests where stable and valuable.
- [ ] Accessibility smoke automation where supported, backed by manual audits.

## 0.6 — Performance and scale

- [ ] Reproducible generated-data benchmarks at 100/1,000/10,000 contacts.
- [ ] Measure root + child SQL statement amplification.
- [ ] Evaluate lightweight list projections and fetch-full-on-selection.
- [ ] Evaluate pagination/incremental loading.
- [ ] Evaluate FTS5 only with an ADR and migration/index-sync plan.
- [ ] Optimize duplicate candidate generation before quadratic scan at high counts.
- [ ] Evaluate streaming CSV/vCard encode/decode while preserving import atomicity.

## 0.7 — Encryption and secret-storage maturity

- [ ] Select/document an officially supported SQLCipher-compatible provider if the project chooses to ship encryption directly.
- [ ] Validate provider licensing and native packaging for every release RID.
- [ ] Add encrypted database/backup/restore integration tests per supported platform.
- [ ] Add an OS credential/secret-store abstraction for runtime database key retrieval.
- [ ] Add user-visible verified encryption state only after provider detection can prove it.

## 0.8 — Release hardening

- [ ] Capture real product screenshots using fictional data after verified release builds.
- [ ] Manual keyboard/screen-reader/high-DPI/theme audit on supported platforms.
- [ ] Windows signing pipeline when credentials/policy are available.
- [ ] macOS Developer ID signing and notarization when credentials/policy are available.
- [ ] Decide installer/package formats per platform.
- [ ] Align release workflow SDK resolution directly with `global.json`.
- [ ] Publish a repeatable release smoke-test record/template.

## Future product exploration

Only after data-preservation and release fundamentals are strong:

- [ ] Optional contact photos with local storage/privacy rules.
- [ ] More complete vCard interoperability.
- [ ] User-configurable custom fields.
- [ ] Optional local reminders/birthday views.
- [ ] Explicitly opt-in synchronization architecture only if it can preserve the offline-first product identity.

Any future cloud/sync feature requires a new privacy/security architecture review and must not silently replace the local-first default.
