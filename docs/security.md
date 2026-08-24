# Security and Privacy Engineering

ContactCore is a local-first cross-platform contact manager. Its security model focuses on personal-data protection, input handling, storage consistency, truthful encryption/backup claims, restore safety, and protection from accidental destructive actions. It is not a network-security product and cannot protect data/process memory from an attacker who already controls the user's device/account/runtime.

For vulnerability reporting see `../SECURITY.md`; for user-facing data practices see `../PRIVACY.md`.

## Sensitive assets

Sensitive assets include:

- native SQLite database and WAL/SHM sidecars;
- native verified backups/recovery artifacts;
- browser IndexedDB contact state;
- browser preference state where it contains behavioral settings;
- CSV/vCard exports/imports;
- temporary picker/restore copies;
- contact values shown on screen;
- runtime native database keys;
- destructive-action safety settings;
- mobile signing credentials/provisioning material **if maintainers add them to a release secret system later**.

Backups, exports, browser snapshots, picker copies, and recovery files are personal-data copies, not harmless generated artifacts.

## Trust boundaries

### Native operating system/runtime

Desktop/Android/iOS trust the host OS/runtime for process isolation, filesystem/app-container permissions, native file-picker behavior, and packaged native dependencies. Same-user malware, privileged administrators, compromised devices/runtimes, disk forensics against unencrypted data, and screen capture are outside application-layer protection.

### Browser origin/profile

WebAssembly trusts the browser engine and origin/profile storage isolation. Contact data is persisted in IndexedDB for the site's origin/profile. Browser policy, extensions, compromised browser/runtime, malicious same-origin code, site-data clearing, private-session teardown, profile deletion, quota eviction, and developer tools with sufficient access are relevant risks.

The browser target must not be described as having the same filesystem/SQLite boundary as native targets.

### Imported CSV/vCard

Imports are untrusted: malformed, Unicode-heavy, formula-like, oversized, or crafted to stress parsers/downstream spreadsheet clients.

### Selected native backup database

A `.db` is untrusted until integrity, ContactCore schema/version, and schema-family identity are verified. Valid SQLite is not automatically valid ContactCore data.

### Duplicate heuristic

Duplicate scoring is advisory. Similarity is not proof two records represent the same person; it must never auto-trigger destructive persistence.

### Shared group/tag identity

SQLite groups/tags are globally shared case-insensitive dictionary rows. A per-contact edit must not silently mutate a shared dictionary identity globally.

### Native SQLite provider

Default native build uses ordinary SQLite. Any SQLCipher-compatible provider deliberately added later joins the trusted computing base and adds licensing/native packaging/update/test obligations.

### Public repository/support surfaces

Issues, PRs, screenshots, logs, fixtures, workflow artifacts, and release discussions are public/support boundaries. Real contacts, databases, browser dumps, exports, keys, signing material, provisioning profiles, and identifying screenshots must not cross them.

## Local-first privacy posture

Normal operations require no mandatory ContactCore account, cloud sync service, analytics, telemetry, or advertising backend.

Native data is stored locally in SQLite. Browser data is stored locally in browser-managed IndexedDB for the active origin/profile. Local-first does **not** imply encrypted-at-rest.

## Native SQL controls

SQLite values/IDs are parameterized. Search values intended as literal text escape backslash, `%`, and `_` before entering `LIKE ... ESCAPE '\'` patterns. Do not concatenate contact/import/search values into SQL.

Connections enable foreign keys and busy timeout. Aggregate writes and bulk import are transactional.

## Native duplicate transaction

`SqliteContactRepository.MergeAsync` in one transaction:

1. requires chosen survivor still exists;
2. requires secondary still exists;
3. writes complete survivor aggregate;
4. deletes secondary;
5. requires exactly one deletion;
6. commits only on complete success.

This prevents half-committed merges and stale-primary resurrection.

## Browser storage controls

`BrowserContactRepository` is separate from native Infrastructure and implements `IContactRepository` over browser-managed persistence.

Security/data-consistency properties:

- initialization loads one serialized ContactCore state record from IndexedDB;
- malformed state/duplicate IDs are rejected rather than silently reinterpreted;
- writes are serialized through a repository gate;
- a pre-mutation in-memory copy is retained;
- JavaScript commits the replacement state inside an IndexedDB readwrite transaction;
- failed persistence restores the previous in-memory dictionary and propagates failure;
- duplicate merge requires both reviewed IDs before mutation;
- no native SQLite backup/encryption capability is advertised.

This protects current-instance consistency, not against malicious same-origin JavaScript or cross-tab write conflicts.

### Cross-tab boundary

The current browser implementation does not implement multi-tab optimistic concurrency, locking across browser tabs, or conflict merging. Simultaneous editing in multiple tabs is not a supported collaborative workflow. A future implementation should add version/etag or transaction-conflict semantics before claiming that behavior safe.

## Complete-aggregate editor safety

Saving replaces contact aggregate state. Presentation omission can therefore become data loss. Both desktop and portable editor models represent phones, emails, addresses, organizations, groups, tags, scalar fields, favorite/archive state, birthday, notes, and persisted/unsaved state.

Identity rules:

- phone/email/address/organization rows are contact-owned and retain IDs through ordinary edits;
- unchanged group/tag assignments retain shared identity;
- true per-contact group/tag rename becomes new-identity reassignment;
- normalization-equivalent/case-only group/tag edit preserves canonical identity/name;
- delimiter-containing names remain exact;
- blank new rows are suppressed;
- label-only legacy addresses remain preservable;
- removing one repeated value does not intentionally remove unrelated values.

Generated GUIDs do not prove persistence; unsaved drafts are tracked explicitly.

## Duplicate review controls

Shared/mobile/browser and desktop flows preserve the same high-level safety rule:

1. scan candidates without mutation;
2. show score/reasons/record previews;
3. require explicit survivor choice;
4. require explicit confirmation;
5. reload/merge through `ContactService`;
6. delegate stale-safe storage mutation to the repository implementation.

There is no general undo stack. Storage consistency is not undo.

## Native database schema identity

Current SQLite schema uses ContactCore schema-family metadata and rejects future schema versions. Backup/restore combines identity/version/required-structure/integrity checks.

## Native backup/restore security

Backup uses SQLite's backup API and verifies output before reporting success.

Restore uses source verification, pre-restore snapshot, staging, supported migration, staging verification, pool/sidecar cleanup, switch, final verification, and failed-switch rollback attempt.

See `storage-backup-recovery.md` for exact steps.

## Browser recovery security boundary

Browser has no native SQLite `BackupService`. Shared UI capability flags disable native database backup/restore/encryption claims. Portable CSV/vCard export is available but remains a limited interchange format.

Important risk: browser-local data can disappear because of site-data clearing, private browsing behavior, profile deletion, policy, origin change, or storage eviction. Users needing a portable copy must export deliberately.

A future full-fidelity browser backup should define a versioned signed/validated document format and migration behavior explicitly instead of copying native SQLite terminology.

## Optional native database encryption

### Default

Ordinary `Microsoft.Data.Sqlite` is plaintext SQLite; issuing `PRAGMA key` alone does not prove encryption.

### Fail-closed requested key

When `CONTACTCORE_DATABASE_KEY` is non-empty, `SqliteConnectionFactory` applies the key and requires `PRAGMA cipher_version` evidence. Missing cipher support closes/rejects the connection.

The key is loaded at runtime even on first launch and excluded from normal `settings.json` serialization.

### Provider responsibility

Shipping native encryption requires a maintained provider, license review, native packaging, and runtime verification on each native target. Do not claim encryption-at-rest until verified.

### Browser is separate

Browser IndexedDB is not covered by the native SQLCipher integration. Do not reuse the native “database key” UI/security language for browser storage without a separate browser cryptographic design and review.

## Preferences

Native `settings.json` stores non-secret preferences and uses temp/replacement writes; malformed JSON falls back to safe defaults.

Browser preferences use local browser storage and keep a session fallback if persistence throws. No native database key is stored there.

## Permanent delete / confirmation

Persisted deletion follows confirmation policy. Unsaved discard performs no repository deletion. Duplicate merge has its own confirmation. Native restore has its own confirmation. Portable UI uses an in-view confirmation state so single-view platforms do not depend on a desktop `Window` dialog.

## Import bounds and atomicity

Avalonia picker integration bounds imported text at **5,000,000 characters**. `ContactService.ImportAsync` deep-copies, normalizes, validates the entire parsed batch before repository persistence.

Native bulk persistence uses one SQLite transaction. Browser bulk persistence uses one gated snapshot replacement and restores prior in-memory state on IndexedDB failure.

## CSV boundary

- requires recognized ContactCore header(s), otherwise imports zero with warning;
- duplicate headers use first occurrence and warn;
- formula-like text starting with `=`, `+`, `-`, or `@` after whitespace is preserved and warned;
- quoting/escaping is not spreadsheet-formula neutralization.

Treat CSV data as untrusted when opening in spreadsheet software.

## vCard boundary

Focused vCard handling supports documented subset, line unfolding, escaping, escaped structured-name delimiters, common TYPE mapping, and controlled warnings. It is not full vCard sanitization/interoperability.

## Diagnostics/privacy

Shared/desktop error presentation avoids unnecessary raw paths/private values. Lower layers should avoid embedding contact payloads/secrets in exceptions. `RedactingLog` is defense-in-depth rather than a complete PII detector.

## Temporary picker copies

When a native storage provider cannot return a local backup path, shared/desktop picker code may copy a selected backup stream to a unique temporary path for restore and then attempt cleanup. Secure deletion semantics remain OS/filesystem-dependent.

Browser import/export normally operates through browser-provided storage streams and does not create a native ContactCore DB restore path.

## Logging and telemetry

No mandatory telemetry pipeline is present. Any future remote logging/analytics/sync requires explicit privacy/security review, minimal payloads, retention/location documentation, and user-facing policy changes.

## Dependency/CI/release security

Package versions are centrally managed; Dependabot and CodeQL are configured.

CI verifies:

- core restore/format/build/test on Ubuntu, Windows, macOS;
- Browser WebAssembly Release build after `wasm-tools` installation;
- Android Release build after Android workload installation;
- iOS Release build on macOS after iOS workload installation;
- CodeQL on workload-free core solution.

Release preflight checks tag/source version. Desktop/browser packages receive checksums. Android/iOS are build-gated without committing private signing credentials.

Checksums are integrity aids, not code-signing authenticity. Current artifacts are not claimed signed/notarized/store-certified.

## Mobile signing security

Production Android/iOS signing material is security-sensitive. If automation is added:

- store credentials only in a suitable encrypted CI secret/identity system;
- never expose signing secrets to untrusted pull-request code;
- use environment/repository protection appropriate to release jobs;
- minimize credential scope/lifetime;
- document signer identity/verification;
- avoid logging secrets/decoded certificates/profiles;
- review third-party signing actions/tooling before use.

## Threats not mitigated

Examples:

- same-user malware/process memory access;
- compromised/rooted/jailbroken/administrator-controlled device;
- stolen unencrypted native storage;
- malicious/compromised browser engine/extensions/same-origin code;
- browser site-data deletion/eviction/private-session loss;
- cross-tab conflicting browser writes;
- insecure backup/export destination;
- malicious spreadsheet behavior after CSV open;
- screen capture/shoulder surfing;
- compromised dependencies/build agents/native providers;
- denial-of-service beyond tested limits;
- arbitrary file/browser-state tampering by an attacker with sufficient local privileges.

## Security review checklist

For security/data-safety/platform changes:

- identify native vs browser assets/trust boundaries;
- add failure-path tests/build gates;
- verify no new secret persistence;
- verify native SQL parameterization/literal wildcard behavior;
- verify browser state validation/write rollback where affected;
- verify aggregate identity semantics;
- verify input bounds/termination;
- verify multi-record destructive operations remain stale-safe/storage-consistent;
- verify confirmation direction/text;
- verify native backup/restore recovery or browser capability boundary;
- verify errors avoid private payloads;
- review dependency/native/mobile signing licensing/security;
- run exact-head platform CI + CodeQL;
- update security/user/platform/maintenance documentation.
