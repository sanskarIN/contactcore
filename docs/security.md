# Security and Privacy Engineering

ContactCore is an offline-first desktop contact manager. Its security model focuses on local personal-data protection, input handling, transaction/data-integrity boundaries, truthful encryption claims, restore safety, and protection from accidental destructive actions. It is not a network-security product and cannot protect files/process memory from an attacker who already controls the user's operating-system account or device.

For vulnerability reporting see `../SECURITY.md`; for user-facing data practices see `../PRIVACY.md`.

## Sensitive assets

Sensitive assets include the active database and SQLite sidecars, verified backups/recovery files, CSV/vCard data, temporary restore-picker copies, contact values shown on screen, runtime database keys, and settings controlling destructive-action safety.

Backups, exports, picker copies, and recovery files are personal-data copies, not harmless generated artifacts.

## Trust boundaries

### Local operating system

ContactCore trusts OS/runtime filesystem permissions, process isolation, native UI/file-picker behavior, and native dependencies. Same-user malware, administrators, disk forensics against unencrypted storage, compromised runtime/native libraries, and screen capture are outside normal application-layer protection.

### Imported CSV/vCard

Imports are untrusted. They may be malformed, Unicode-heavy, formula-like, oversized, or crafted to stress parsers/downstream spreadsheet software.

### Selected backup database

A `.db` is untrusted until integrity, ContactCore schema/version, and schema-family identity have been verified. Valid SQLite is not automatically valid ContactCore data.

### Duplicate heuristic

Duplicate scoring is a heuristic, not proof that two people are the same. Scores must never automatically trigger destructive persistence.

### Shared group/tag dictionary

Groups/tags are globally shared case-insensitive dictionary rows linked to contacts. A per-contact name edit must not be treated as permission to mutate a shared dictionary identity globally.

### Native SQLite provider

The default app uses ordinary SQLite. A SQLCipher-compatible provider, if deliberately integrated, joins the trusted computing base and adds packaging/licensing/update obligations.

### Public repository/support surfaces

Issues, pull requests, screenshots, logs, and fixtures are public-data boundaries. Real contact databases/exports/keys and identifying screenshots must not cross them.

## Offline-first privacy posture

Normal contact operations use local files/SQLite and require no mandatory account, cloud sync, analytics, telemetry, or advertising service.

Local-first does **not** imply encrypted-at-rest. The default ordinary SQLite build is plaintext unless a compatible encryption provider is deliberately integrated.

## SQL controls

Repository values/IDs are parameterized. Search values intended as literal text escape backslash, `%`, and `_` before entering `LIKE ... ESCAPE '\'` patterns. Developer-controlled table names in fixed loops are constants.

Never concatenate contact/import/search values into SQL text.

## SQLite consistency controls

Factory-opened connections enable foreign keys and a busy timeout. Aggregate writes and multi-contact imports are transactional.

Duplicate merge is a dedicated destructive repository operation. Inside the same transaction it:

1. requires the chosen survivor/primary to still exist;
2. requires the secondary to still exist;
3. writes the complete survivor aggregate;
4. deletes the secondary;
5. requires exactly one secondary row to be deleted;
6. commits only if the whole operation succeeds.

This prevents both half-committed merges and stale-primary resurrection from reviewed UI state.

## Complete-aggregate editor safety

The repository replaces contact-owned child/link state from the complete aggregate supplied on save. A presentation omission can therefore become data loss.

The full editor represents phones, emails, addresses, organizations, groups, and tags, but identity semantics differ:

- phone/email/address/organization rows are contact-owned and retain IDs through ordinary edits;
- unchanged group/tag assignments retain their shared dictionary identity;
- a true per-contact group/tag rename gets a fresh dictionary identity and becomes reassignment;
- a case-only/normalization-equivalent group/tag edit keeps the existing identity/canonical name;
- group/tag names with commas/semicolons remain exact;
- blank new rows are suppressed;
- label-only legacy addresses remain preservable;
- removal of one repeated value/link does not intentionally remove unrelated values.

This prevents a local group/tag edit from reusing a global primary key with a different name, which could otherwise create SQLite conflicts or imply an unintended global taxonomy rename.

Generated GUIDs are not proof of persistence; unsaved drafts are tracked separately so discard never becomes an unintended database delete.

## Duplicate detection and destructive merge controls

The duplicate UI:

1. scans candidates without mutating data;
2. presents confidence and matching reasons;
3. shows both record summaries;
4. requires explicit survivor choice;
5. requires confirmation regardless of permanent-delete preference;
6. calls `ContactService.MergeAsync`, which reloads both contacts;
7. delegates to the stale-safe atomic repository transaction.

If either reviewed record disappeared before persistence, the merge is rejected. The merge algorithm preserves the selected survivor root identity, combines documented unique values, and generates fresh IDs for copied contact-owned secondary children where needed.

There is no general-purpose undo stack. Atomicity prevents partial merge state; it is not undo. Verified backups remain the recovery path for an incorrectly confirmed merge.

## Database schema identity

Schema version 2 uses:

```text
app_metadata['schema_family'] = 'contactcore'
```

Backup/restore validation combines this marker with required structures/version. Future schema versions are rejected rather than treated as downgrade-compatible.

## Backup integrity and restore safety

Backup creation uses SQLite's backup API and requires integrity/schema/identity verification before reporting success.

Restore follows:

1. path validation and active-source rejection;
2. read-only source verification;
3. verified pre-restore snapshot of current active data when present;
4. staging copy;
5. supported migration of staging;
6. staging integrity/identity verification;
7. pool/sidecar cleanup and switch;
8. final active-database verification;
9. failed-restored-file retention + recovery-snapshot restoration attempt if final verification fails.

The extra work is intentional because data-loss protection is higher priority than raw replacement speed.

## Optional database encryption

### Default build

`Microsoft.Data.Sqlite` with ordinary SQLite does not become encrypted merely because code issues `PRAGMA key`.

### Fail-closed key request

When `CONTACTCORE_DATABASE_KEY` is non-empty, `SqliteConnectionFactory` applies the runtime key without interpolating the original secret and checks `PRAGMA cipher_version`. If cipher support cannot be proven, the connection is closed and startup fails.

The preferences loader reads the runtime key even on first launch when `settings.json` does not yet exist. The key is excluded from serialized settings.

### Provider responsibility

Shipping encryption requires a maintained compatible provider plus licensing/native packaging/runtime tests on every supported release target. Do not claim encryption-at-rest until provider behavior is actually verified.

### Not guaranteed

ContactCore does not protect keys from a process-memory attacker, provide an OS secret-store today, automatically encrypt CSV/vCard exports, or claim cryptographic properties beyond an integrated provider.

## Preference resilience

`settings.json` stores non-secret preferences. Saves use temporary-file replacement. Malformed JSON falls back to conservative defaults, including permanent-delete confirmation enabled. This is corruption resilience, not tamper authentication.

## Permanent delete controls

Persisted deletion follows the configured confirmation requirement. If confirmation is required but unavailable, deletion is blocked. Unsaved **Delete / discard** performs no repository deletion.

Restore and duplicate merge always require their own confirmation callback in the desktop workflow.

## Import resource limits and atomicity

The desktop reader bounds selected import text at **5,000,000 characters**, uses UTF-8/BOM detection, and rejects oversize input before accepting the complete string.

`ContactService.ImportAsync` deep-copies/normalizes parsed contacts, validates the complete batch, and only then calls one-transaction repository persistence. Alternate future import entry points need their own resource bounds.

## CSV hardening boundary

Current CSV behavior:

- requires at least one recognized ContactCore header; otherwise imports zero contacts with a warning;
- duplicate headers use the first occurrence and warn;
- formula-like text beginning with `=`, `+`, `-`, or `@` after leading whitespace is preserved and warned about;
- quoting/escaping does **not** constitute spreadsheet-formula neutralization.

Treat contact-derived CSV as untrusted data when opening it in spreadsheet software.

## vCard hardening boundary

Focused vCard handling supports the documented subset, line unfolding, supported escaping, escaped structured-name delimiters, common field TYPE mappings, and controlled malformed-card warnings.

Invalid birthday warnings intentionally do not echo the invalid imported value. This is not a claim of full vCard interoperability or sanitization for every external client.

## Diagnostic/validation privacy

`RedactingLog.Sanitize` removes common email/long-number shapes and caps output length before desktop error display. It is defense-in-depth, not a complete PII classifier.

Lower layers should avoid raw contact values/secrets in exceptions. Validation/parser messages identify problems without unnecessarily echoing private values where practical.

## Temporary restore-picker copies

When a storage provider cannot expose a normal local path, Desktop copies the selected backup stream to a unique temporary path and marks it for best-effort deletion after restore. OS/filesystem behavior still controls actual secure deletion semantics.

## Logging and telemetry

There is no mandatory telemetry pipeline. Future logging should default local, avoid raw contact/secret/path payloads where unnecessary, document retention/location, and receive review before any remote telemetry is introduced.

## Dependency, CI, and release security

Package versions are centrally managed; Dependabot and CodeQL are configured; CI is configured across Windows, Ubuntu, and macOS.

The 2.0.12 release workflow validates tag/project-version equality, uses the SDK policy in `global.json`, packages platform artifacts, generates SHA-256 checksums, and grants repository write permission only to the final GitHub Release job.

Checksums are integrity aids, not a substitute for trusted code signing/notarization. Current artifacts are not claimed as signed/notarized.

## Threats not mitigated by the application

Examples include same-user malware/process access, stolen unencrypted storage, weak OS credentials/permissions, insecure backup destinations, malicious spreadsheet behavior after opening CSV, screen capture, compromised build/dependency/native providers, denial-of-service beyond tested limits, and arbitrary tampering by an attacker who can write application/user-data files.

## Security review checklist

For a security/data-safety change:

- identify assets and trust boundaries;
- add failure-path tests;
- verify no new secret persistence;
- verify parameterized SQL/literal wildcard behavior;
- verify correct contact-owned versus shared-dictionary identity semantics;
- verify input bounds/termination;
- verify transactional all-or-nothing and stale-state behavior for multi-row destructive writes;
- verify confirmation direction/text matches the operation;
- verify backup/restore recovery behavior;
- verify errors avoid private payloads;
- review dependency/native licensing;
- run CI/CodeQL on the exact final head;
- update security/user/maintenance documentation.
