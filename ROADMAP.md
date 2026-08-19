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

- [x] Transactional bulk import (`UpsertManyAsync`) with rollback on failure.
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
- [x] Runtime database key loaded on first launch even when settings do not yet exist.
- [x] Preferences temp-file replacement and safe defaults for corrupted JSON.
- [x] Permanent-delete confirmation preference, enabled by default.
- [x] Restore confirmation.
- [x] Native file-picker import/export UI.
- [x] Native/stream-backed backup picker handling.
- [x] 5,000,000-character desktop import bound.
- [x] Data Tools view for import/export/backup/restore.
- [x] Dedicated Settings/About/privacy surface.
- [x] System/Light/Dark theme switching.
- [x] Reduced-motion preference persistence.
- [x] Desktop keyboard shortcuts and explicit visible focus styles.
- [x] `Ctrl+S` restricted to the active contact editor.
- [x] Desktop draft regression test project.
- [x] Full rich-field editor for multiple phones/emails/addresses/organizations/groups/tags.
- [x] Stable repeated-row identity preservation through edit/save.
- [x] Explicit unsaved/persisted draft state and safe unsaved discard.
- [x] Atomic duplicate survivor-update + secondary-delete persistence.
- [x] Interactive duplicate review with evidence, preview, survivor choice, and confirmation.

## 0.3 — Documentation completeness

- [x] Documentation hub/index.
- [x] Deep user/setup/architecture/data-model guides.
- [x] Deep desktop UI guide aligned with the full editor/duplicate workflow.
- [x] Import/export format and security limitations.
- [x] Storage/backup/recovery guide with failure paths.
- [x] Expanded threat model/security guide.
- [x] Expanded testing/accessibility/performance/CI/release/troubleshooting guides.
- [x] Maintainer engineering guide.
- [x] ADRs for modular monolith, SQLite, and encryption-provider boundary.
- [x] Exhaustive tracked-file repository reference.
- [x] Root README/changelog/roadmap synchronized for this checkpoint; future behavior changes must keep them synchronized.

## 0.4 — Rich UX completion

The prior compact-editor preservation phase has been superseded by a complete editor for the repeated collections represented by the current domain model.

- [x] Full multi-value phone/email add/edit/remove editor.
- [x] Address add/edit/remove editor.
- [x] Organization add/edit/remove editor.
- [x] Per-contact group/tag add/edit/remove assignment.
- [x] Exact delimiter-containing group/tag names without text-splitting loss.
- [x] Add/edit/remove and blank-row regression tests for current rich controls.
- [x] Explicit unsaved/new-contact state.
- [x] Interactive duplicate candidate list.
- [x] Side-by-side duplicate comparison/merge preview.
- [x] User-confirmed merge workflow wired through `ContactService`/`ContactMerger`.
- [x] Explicit user choice of which duplicate record survives.
- [x] One-transaction duplicate merge/delete with rollback if the secondary row disappeared.
- [ ] Drag/drop or other reorder controls for repeated rich fields.
- [ ] Dedicated global group/tag taxonomy-management screen.
- [ ] Undo/recovery UX for high-impact contact modifications where practical.

## 0.5 — Test and resilience expansion

- [x] Test literal search characters `%`, `_`, and backslash.
- [x] Tag/group/StartsWith repository filter tests.
- [x] Full address/organization/group/tag repository round-trip/replacement tests.
- [x] Restore rejection test for valid non-ContactCore SQLite file.
- [x] Missing-backup and same-active-path restore tests.
- [x] Atomic duplicate-merge success and missing-secondary rollback tests.
- [x] Import parser tests for unsupported/duplicate CSV headers, formula-prefix warnings, escaped vCard fields, TYPE mapping, and non-echoing birthday warnings.
- [x] Desktop rich-field tests for IDs, exact group/tag names, blank rows, removal semantics, and label-only legacy address preservation.
- [x] App-path environment/fallback tests.
- [x] Redaction truncation/PII-shape tests.
- [ ] Forced post-switch restore verification failure/rollback test.
- [ ] Restore staging/temp cleanup failure-path tests beyond current successful/invalid-source flows.
- [ ] ContactService normalization and indexed import-validation unit tests for every rich field.
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
- [ ] Align release workflow SDK resolution directly with `global.json` if the workflow still duplicates SDK selection.
- [ ] Publish a repeatable release smoke-test record/template.

## Future product exploration

Only after data-preservation, test, scale, and release fundamentals remain strong:

- [ ] Optional contact photos with local storage/privacy rules.
- [ ] More complete vCard interoperability.
- [ ] User-configurable custom fields.
- [ ] Optional local reminders/birthday views.
- [ ] Explicitly opt-in synchronization architecture only if it preserves the offline-first product identity.

Any future cloud/sync feature requires a new privacy/security architecture review and must not silently replace the local-first default.
