# ContactCore — v2.0.12 Final Handoff

## Release checkpoint

ContactCore is prepared on the final audit branch as **version 2.0.12**.

- Repository: `https://github.com/sanskarIN/contactcore`
- Visibility: public
- Default branch: `main`
- Current integration base: `3900063bcdc2f7f0834118abc2580e030f133d73`
- Final audit branch: `audit/contactcore-20260819`
- Authoritative integration pull request: **PR #4**
- Version: **2.0.12**
- Intended release tag after merge/verification: **`v2.0.12`**
- Primary stack: C# / .NET 10 / Avalonia / SQLite
- License: MIT
- Product posture: private, offline-first desktop contact manager
- Visible project credit: **Made by the Sanskar**

PR #1 and PR #3 are closed without merge as superseded. Their useful ideas were compared against the hardened implementation and selectively carried forward; they must not be merged later without a fresh review because they overlap older versions of the same architecture/UI/storage work.

## Version 2.0.12 metadata

`Directory.Build.props` is the application-version source of truth:

```text
VersionPrefix        2.0.12
Version              2.0.12
AssemblyVersion      2.0.12.0
FileVersion          2.0.12.0
InformationalVersion 2.0.12
```

The release workflow resolves the project version before publishing and rejects a tag that does not equal `v<Version>`. This source tree is intended to publish from tag **`v2.0.12`**.

## Final product implementation

### Domain

- Contact aggregate with stable GUID identity and timestamps.
- Given/family/nickname fields, birthday, notes, favorite/archive state.
- Repeated phone, email, postal address, organization, group, and tag records.
- Display-name fallback behavior.
- Deep-copy support for editor/import workflows.
- Contact validation with privacy-conscious error text.
- Accent-insensitive normalized search keys and digits-only phone comparison keys.

### Application

- Repository, backup, and preferences abstractions.
- Search/query contract for text, favorites, archived inclusion, tag, group, and starting-letter filters.
- Save workflow with normalization, timestamp refresh, validation, and persistence.
- Whole-batch import normalization/validation before one repository bulk write.
- Indexed import validation issue paths such as `Contact[2].Email`.
- Favorite/archive update workflows.
- Duplicate scoring using normalized name/email/phone/birthday signals.
- Deterministic `ContactMerger` combining documented unique data while preserving the chosen survivor root identity.
- Validated `ContactService.MergeAsync` orchestration that reloads both reviewed records.
- Hardened CSV and focused vCard codecs.

### Infrastructure

- Cross-platform application-data paths with optional `CONTACTCORE_DATA_PATH` directory override.
- SQLite connection factory with foreign keys, busy timeout, access modes, pooling configuration, and optional keyed-provider verification.
- Fail-closed `CONTACTCORE_DATABASE_KEY` behavior: encryption requested without verified cipher support fails rather than silently claiming encrypted storage.
- Runtime database key loaded even on first launch before `settings.json` exists.
- Runtime key excluded from serialized preferences.
- Ordered SQLite migrations and future-schema rejection.
- ContactCore schema-family identity marker.
- Complete aggregate persistence with transactional contact-owned child/link replacement.
- Case-insensitive shared group/tag dictionary/link behavior.
- Safe per-contact group/tag reassignment by dictionary name/identity rather than mutating a shared dictionary primary key to a different name.
- Literal `%`, `_`, and backslash escaping for SQLite `LIKE` search.
- Transactional batch upsert.
- Transactional duplicate merge: survivor update and secondary deletion are one operation.
- Stale duplicate protection checks **both** reviewed contacts inside the merge transaction. A missing secondary cancels/rolls back; a missing primary is rejected and cannot be recreated from stale UI state.
- SQLite-native verified backup creation.
- Integrity/schema/version/identity verification.
- Verified pre-restore recovery snapshots.
- Staged restore migration/verification before active replacement.
- Final restored-database verification with recovery rollback attempt.
- Unique backup/recovery filenames.
- Preferences temp-file/replacement persistence and conservative corrupted-JSON defaults.
- PII-oriented diagnostic redaction/truncation for desktop-visible errors.

### Desktop application

- Avalonia three-column desktop experience with search, browsing, list, and detail surface.
- Full editor for the entire currently persisted Contact aggregate.
- Multiple phone/email rows with editable label/value/field kind.
- Address and organization add/edit/remove rows.
- Group/tag add/edit/remove rows.
- Exact group/tag names containing commas or semicolons; no delimiter-splitting shortcut.
- Root contact ID and creation timestamp preserved through ordinary editing.
- Contact-owned phone/email/address/organization IDs preserved while their rows remain.
- Unchanged group/tag assignments preserve their **shared dictionary identity**.
- A true per-contact group/tag rename receives a new dictionary identity and becomes reassignment instead of reusing one shared global ID with a different name.
- Case-only/normalization-equivalent group/tag edits keep the existing dictionary identity and canonical stored name.
- Blank newly added rich rows suppressed during draft conversion.
- Existing label-only address remains representable/preservable.
- Explicit `IsPersisted` draft state; generated GUID is not evidence that a new draft exists in SQLite.
- Delete/discard on an unsaved draft performs no database deletion and needs no permanent-delete confirmation.
- Persisted deletion follows the configured confirmation safeguard.
- Race-safe 180 ms debounced search with cancellation of superseded operations.
- `Ctrl+F` search focus, `Ctrl+N` new contact, `Esc` close/cancel.
- `Ctrl+S` restricted to an active contact editor so other surfaces cannot accidentally save stale contact state.
- Native CSV/vCard import/export pickers.
- Desktop import text bound of 5,000,000 characters.
- Stream-backed restore picker support through temporary local copies with best-effort cleanup.
- Data Tools surface for import/export/backup/restore.
- Settings/About/privacy surface.
- System/Light/Dark theme selection.
- Reduced-motion preference.
- Visible focus styling.
- Interactive duplicate-review surface with candidate list, score, reasons, side-by-side summaries, merge explanation, explicit survivor choice, and confirmation.
- Both duplicate merge directions available: keep first or keep second.

## Important bugs fixed during the final audit

1. Corrected the compile-time duplicate-merger reference from nonexistent `OrganizationAffiliation` to the real `ContactOrganization` type.
2. Fixed first-launch database-key handling so `CONTACTCORE_DATABASE_KEY` is not ignored when settings do not exist yet.
3. Fixed unsaved new-contact deletion so discarding a draft cannot become a database delete.
4. Fixed `Ctrl+S` so contact save cannot be invoked outside the editor.
5. Fixed group/tag data loss from delimiter-separated editor text by replacing it with independent exact rows.
6. Fixed **shared group/tag rename identity**: preserving an old shared dictionary ID while changing its name could cause SQLite primary-key conflicts or imply an unintended global rename. The editor now retains original-name metadata, keeps identity only for unchanged/equivalent assignments, and gives a true per-contact rename a fresh dictionary identity.
7. Fixed blank new address rows so they do not become empty persisted records while preserving legitimate legacy label-only rows.
8. Hardened duplicate persistence so survivor update + secondary deletion are atomic.
9. Hardened duplicate concurrency so a missing secondary cancels/rolls back the merge.
10. Hardened duplicate concurrency again so a missing chosen primary is never recreated from a stale reviewed snapshot.
11. Hardened CSV header handling so unrelated files do not create meaningless unnamed contacts.
12. Hardened duplicate CSV-header handling so the first supported column wins with a warning rather than ambiguous/crashing behavior.
13. Added spreadsheet-formula-prefix warnings while preserving original contact text instead of silently modifying data.
14. Hardened supported vCard escaping, structured-name delimiters, common TYPE mapping, nested/unterminated-card behavior, and birthday-warning privacy.

## Regression coverage added/expanded

### Domain tests

Validation, privacy-preserving messages, Unicode search normalization, display/deep-copy/phone normalization, and boundary behavior represented by the suite.

### Application tests

- Scalar, phone, and email save normalization.
- Address, organization, optional whitespace-to-null organization fields, group, and tag normalization.
- Whole-batch import validation before persistence.
- Indexed import issue fields.
- Import deep-copy/non-mutation behavior.
- Shared import update timestamps and one bulk repository call.
- Search query trimming while preserving filters.
- Duplicate scoring/merge identity safeguards.
- CSV/vCard baseline and parser-hardening regressions.

### Infrastructure tests

- Base and full-rich SQLite aggregate round trip.
- Complete-aggregate replacement behavior.
- **Shared group/tag reassignment after rename** against already persisted old dictionary rows, proving new dictionary identities persist without conflicting with old global identities.
- Favorites/search/tag/group/StartsWith filters.
- Literal SQL LIKE metacharacter handling.
- Cascade delete.
- Bulk rollback.
- Atomic duplicate merge success.
- Missing-secondary rollback.
- Missing-primary rejection/non-resurrection while preserving the remaining secondary contact.
- Backup/restore verification, identity, legacy migration and future-schema boundaries.
- Missing/self restore-source guards.
- Preferences/first-run runtime key behavior.
- Application paths and redaction behavior.

### Desktop tests

- Root ID/timestamp/favorite/archive preservation.
- Persisted versus unsaved draft state.
- Exact birthday parsing.
- Full repeated-field editing with contact-owned child-ID preservation.
- Unchanged group/tag shared identity preservation.
- True group/tag rename → fresh shared dictionary identity.
- Case-only/equivalent group/tag edit → existing identity/canonical name retained.
- Selective row removal.
- Exact comma/semicolon group/tag names.
- Label-only legacy address preservation.
- Blank rich-row suppression.
- No mutation of the source aggregate while editing a draft.

## Group/tag shared-dictionary boundary

Groups and tags are not contact-owned child tables; they are shared case-insensitive dictionaries linked to contacts.

Therefore 2.0.12 intentionally uses this rule:

```text
unchanged/equivalent assignment → keep shared dictionary identity
true per-contact rename          → new dictionary identity / reassignment
```

Ordinary per-contact editing does not silently perform a global taxonomy rename. Old dictionary rows can remain orphaned after their final link is removed. A future global taxonomy-management feature must define true global rename/delete/orphan-cleanup semantics explicitly.

## CSV/vCard fidelity boundary

CSV and vCard remain interoperability formats, **not full-fidelity ContactCore backups**.

CSV exports a selected scalar field set plus the first phone/email. Formula-like text is preserved rather than spreadsheet-neutralized; warnings/documentation explain downstream spreadsheet risk.

Focused vCard supports the documented subset and common escaping/type behavior. It does not claim complete support for every vCard property, address, organization, media/custom field, ContactCore group/tag/identity/timestamp/archive/favorite field, encoding, or external-client extension.

Use a verified SQLite backup when full ContactCore recovery fidelity is required.

## Release pipeline for 2.0.12

The tag-driven workflow is hardened as follows:

1. trigger pattern `v*.*.*`;
2. preflight installs the SDK from `global.json`;
3. preflight resolves the project `Version` with MSBuild;
4. tag must exactly equal `v<Version>`;
5. publish matrix: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`;
6. every target restores and runs the solution tests in Release before publish;
7. self-contained/single-file-targeted output is packaged as Windows `.zip` and Linux/macOS `.tar.gz`;
8. Unix packaging occurs before Actions artifact upload so executable metadata is retained in the tar archive;
9. final job generates `SHA256SUMS.txt`;
10. final GitHub Release attaches all four archives plus checksums;
11. workflow defaults to `contents: read`; only release creation receives `contents: write`;
12. release SDK selection uses `global.json` rather than a separate version selector.

Expected package names:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
SHA256SUMS.txt
```

Checksums provide byte-integrity checking relative to the published checksum manifest; they are not a substitute for platform code signing/notarization.

## Documentation completion

The synchronized documentation set covers project overview, user workflows, setup, architecture, data model, desktop UI, import/export, storage/backup/recovery, security/privacy engineering, accessibility, performance, development, testing, CI/CD, release engineering, troubleshooting, maintainers, ADRs, governance/support, changelog/roadmap, and the canonical **94-file repository reference**.

Temporary test/reference addenda created during the audit were folded into canonical documentation and removed.

## Pull-request reconciliation

- **PR #4** — authoritative v2.0.12 integration branch into `main`.
- **PR #1** — closed without merge as superseded.
- **PR #3** — closed without merge as superseded by the stronger PR #4 implementation.

Do not merge the old overlapping branches after #4 without a fresh conflict/data-safety review.

## Verification status

The coding execution environment used for this work does **not** have the .NET SDK installed and cannot download it because outbound DNS/network access is unavailable. Therefore local `dotnet restore`, `dotnet format`, `dotnet build`, and `dotnet test` results must not be invented or reported as locally passed.

The authoritative gate is GitHub Actions on the **exact final PR #4 head**:

- CI: restore + format verification + Release build + tests on Ubuntu, Windows, and macOS.
- CodeQL: C# analysis on the same final pull-request head.

This handoff is intentionally written before the final checks settle. If final CI/CodeQL exposes an actionable defect, fix it in PR #4 and repeat the exact-head verification; do not merge based on an older green/cancelled run.

## Remaining non-blocking roadmap after 2.0.12

- drag/drop or other persisted reorder UX for repeated rich fields;
- dedicated global group/tag taxonomy-management UI including explicit global rename/delete/orphan cleanup;
- general undo/recovery UX beyond verified backups;
- forced post-switch restore-verification failure injection and deeper cleanup-failure tests;
- search-debounce/confirmation/Avalonia-native integration tests where stable;
- accessibility smoke automation plus manual screen-reader/high-DPI/platform audits;
- generated-data performance benchmarks and list/query/duplicate-candidate scale optimization;
- officially selected/tested SQLCipher-compatible provider and OS secret-store abstraction if direct encryption support is shipped;
- real fictional-data screenshots after verified release builds;
- Windows signing and macOS Developer ID/notarization when credentials/policy exist;
- installer/package-manager formats beyond portable release archives;
- repeatable manual release smoke-test record.

These are future maturity items and must not be described as implemented 2.0.12 capabilities.

## Final merge/release procedure

1. Freeze PR #4 except for fixes required by final checks.
2. Verify CI and CodeQL on its exact final head.
3. Fix any actionable failure in small commits and repeat verification.
4. Merge PR #4 into `main` while preserving granular commit history.
5. Verify merged `main` state.
6. Create/push annotated tag `v2.0.12` from the intended verified `main` commit.
7. Inspect all four packaged artifacts and `SHA256SUMS.txt` from the release workflow.
8. Perform documented fictional-data smoke tests before claiming platform/accessibility coverage.

No release documentation should claim that 2.0.12 artifacts are signed/notarized unless those mechanisms are actually implemented and verified.
