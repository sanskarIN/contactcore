# Security and Privacy Engineering

ContactCore is an offline-first desktop contact manager. Its security model focuses on local personal-data protection, input handling, transaction/data-integrity boundaries, truthful encryption claims, restore safety, and protection from accidental destructive actions. It is not a network-security product and cannot protect files/process memory from an attacker who already controls the user's operating-system account or device.

For vulnerability reporting see `../SECURITY.md`; for user-facing data practices see `../PRIVACY.md`.

## Sensitive assets

Sensitive assets can include:

- `contactcore.db` and SQLite WAL/SHM sidecars;
- verified backups and pre-restore snapshots;
- failed-restore/recovery copies;
- CSV/vCard exports and import files;
- temporary restore-picker files;
- contact values shown on screen;
- runtime database encryption keys when configured;
- settings that influence destructive-action safety.

Backups, exports, picker copies, and recovery files are personal-data copies, not harmless generated artifacts.

## Trust boundaries

### Local operating system

ContactCore trusts OS/runtime filesystem permissions, process isolation, native UI/file-picker behavior, and native dependencies. Same-user malware, administrators, disk-forensics against unencrypted storage, compromised runtime/native libraries, and screen capture are outside normal application-layer protection.

### Imported CSV/vCard

Imports are untrusted. They may be malformed, Unicode-heavy, formula-like, oversized, or crafted to stress parsers/downstream spreadsheet software.

### Selected backup database

A `.db` is untrusted until integrity, ContactCore schema/version, and schema-family identity have been verified. Valid SQLite is not automatically valid ContactCore data.

### Duplicate heuristic

Duplicate scoring is also a trust boundary of a different kind: it is a heuristic, not proof that two people are the same. Scores must never automatically trigger destructive persistence.

### Native SQLite provider

The default app uses ordinary SQLite. A SQLCipher-compatible provider, when deliberately integrated, joins the trusted computing base and introduces packaging/licensing/update requirements.

### Public repository/support surfaces

Issues, pull requests, screenshots, logs, and fixtures are public-data boundaries. Real contact databases/exports/keys and identifying screenshots must not cross them.

## Offline-first privacy posture

Normal contact operations use local files/SQLite and require no mandatory account, cloud sync, analytics, telemetry, or advertising service.

Local-first does **not** imply encrypted-at-rest. The default ordinary SQLite build is plaintext unless a compatible encryption provider is deliberately integrated.

## SQL controls

Repository values/IDs are parameterized. Search values intended as literal text escape backslash, `%`, and `_` before entering `LIKE ... ESCAPE '\'` patterns.

Developer-controlled table names in fixed loops are constants. Never concatenate contact/import/search values into SQL text.

## SQLite consistency controls

Factory-opened connections enable foreign keys and a busy timeout. Aggregate writes and multi-contact imports are transactional.

The repository also exposes a dedicated atomic duplicate-merge operation: it writes the complete chosen survivor aggregate and deletes the secondary contact inside the **same SQLite transaction**. The delete must affect exactly one secondary row; otherwise the transaction throws/rolls back so only half the destructive operation is never intentionally committed.

## Complete-aggregate editor safety

The repository replaces contact-owned child/link rows from the complete aggregate supplied on save. A presentation bug that omits an existing child can therefore become data loss.

The current full editor represents phones, emails, addresses, organizations, groups, and tags and preserves existing child IDs for rows that remain. Regression tests cover removal isolation, exact delimiter-containing group/tag names, blank-row suppression, label-only address preservation, and source-aggregate non-mutation.

A new/alternate editor must either represent every persisted field or explicitly preserve unsupported fields. Generated GUIDs are not proof of persistence; unsaved drafts are tracked separately so discard never becomes an unintended database delete.

## Duplicate detection and destructive merge controls

The duplicate UI:

1. scans candidates without mutating data;
2. presents confidence and matching reasons;
3. shows both record summaries;
4. requires the user to choose which record survives;
5. requires confirmation regardless of permanent-delete preference;
6. calls the validated Application merge workflow;
7. commits survivor update + secondary deletion atomically.

The merge algorithm preserves the selected survivor identity, combines documented unique child values, and generates fresh IDs for copied secondary child rows where needed.

There is no general-purpose undo stack. Atomicity prevents partial merge state; it is not undo. Verified backups remain the recovery path for an incorrectly confirmed destructive merge.

## Database schema identity

Schema version 2 uses:

```text
app_metadata['schema_family'] = 'contactcore'
```

Backup/restore validation combines this identity with required tables and schema version. Future schema versions are rejected instead of being opened as though downgrade compatibility were guaranteed.

## Backup integrity

Backup creation uses SQLite's backup API, then requires integrity/schema/identity verification before reporting success. A path is not considered a successful ContactCore backup merely because a file was created.

## Restore safety

Restore follows:

1. path validation and active-source rejection;
2. read-only source verification;
3. verified pre-restore snapshot of current active data when present;
4. staging copy;
5. migration of staging to the supported schema;
6. staging integrity/identity verification;
7. pool/sidecar cleanup and switch;
8. final active-database verification;
9. failed-restored-file retention + recovery-snapshot restoration attempt if final verification fails.

This sequence is intentionally more expensive than raw replacement because data-loss protection is the priority.

## Optional database encryption

### Default build

`Microsoft.Data.Sqlite` with ordinary SQLite does not become encrypted merely because code issues `PRAGMA key`.

### Fail-closed key request

When `CONTACTCORE_DATABASE_KEY` is non-empty, `SqliteConnectionFactory` applies the runtime key without interpolating the original secret and queries `PRAGMA cipher_version`. If cipher support cannot be proven, the connection is closed and startup fails.

The preferences loader reads this runtime key even on first launch when `settings.json` does not yet exist. The key is excluded from the serialized settings model.

### Provider responsibility

Shipping encryption requires a maintained compatible provider plus licensing/native packaging/runtime tests on every supported release target. Do not claim encryption-at-rest until provider behavior is actually verified.

### Not guaranteed

ContactCore does not protect keys from a process-memory attacker, provide an OS secret-store today, automatically encrypt CSV/vCard exports, or claim cryptographic properties beyond an integrated provider.

## Preference resilience

`settings.json` stores non-secret preferences. Saves use a temporary file followed by replacement. Malformed JSON falls back to conservative defaults, including permanent-delete confirmation enabled.

This is corruption resilience, not tamper authentication.

## Permanent delete controls

For persisted contacts, permanent deletion follows the configured confirmation requirement. If confirmation is required but unavailable, the action is blocked.

For an unsaved draft, **Delete / discard** performs no repository deletion.

Restore and duplicate merge always require their own confirmation callback in the desktop workflow.

## Import resource limits and atomicity

The desktop text reader bounds selected import text at **5,000,000 characters**, uses UTF-8/BOM detection, and rejects oversize input before accepting the complete string.

`ContactService.ImportAsync` deep-copies/normalizes the parsed contacts, validates the complete batch, and only then calls one-transaction repository persistence. A validation or write failure does not intentionally leave a successful prefix committed.

Alternate future import entry points must impose their own resource bounds rather than assuming every caller passes through `MainWindow`.

## CSV hardening boundary

Current CSV behavior:

- requires at least one recognized ContactCore header; otherwise imports zero contacts with a warning;
- duplicate headers use the first occurrence and warn;
- formula-like text beginning with `=`, `+`, `-`, or `@` after leading whitespace is preserved and warned about;
- quoting/escaping does **not** constitute spreadsheet-formula neutralization.

Users should treat contact-derived CSV as untrusted data when opening it in spreadsheet software. Do not claim formula mitigation unless a deliberate compatibility policy and tests are added.

## vCard hardening boundary

Focused vCard handling supports the documented subset, line unfolding, supported escaping, escaped delimiters in structured names, common field TYPE mappings, and controlled malformed-card warnings.

Invalid birthday warnings intentionally do not echo the invalid imported value. Unknown/unsupported vCard properties are not proof of full interoperability or sanitization for every external client.

## Diagnostic/validation privacy

`RedactingLog.Sanitize` removes common email/long-number shapes and caps output length before desktop error display. It is defense-in-depth, not a complete PII classifier.

Lower layers should avoid inserting raw contact values/secrets into exceptions. Validation messages and hardened parser warnings identify the error without intentionally echoing invalid private values where practical.

## Temporary restore-picker copies

When a storage provider cannot expose a normal local path, Desktop copies the selected backup stream to a unique temporary path and marks it for best-effort deletion after the restore attempt.

OS/filesystem behavior still controls actual secure deletion semantics. Treat leftover temporary files as potentially sensitive.

## Logging and telemetry

There is no mandatory telemetry pipeline. Any future logging should default local, avoid raw contact/secret/path payloads where unnecessary, document retention/location, and require review before remote telemetry.

## Dependency and CI security

Package versions are centrally managed; Dependabot and CodeQL are configured; CI is configured across Windows, Ubuntu, and macOS.

Automated analysis does not replace review of native-provider licensing, dependency behavior, release artifacts, or final-commit verification.

## Release security

Release automation publishes self-contained/single-file artifacts for Windows x64, Linux x64, macOS x64, and macOS arm64. Current repository documentation does not claim signing or notarization.

Do not describe artifacts as signed/notarized until those steps are implemented and verified.

## Threats not mitigated by the application

Examples include same-user malware/process access, stolen unencrypted storage, weak OS credentials/permissions, insecure backup destinations, malicious spreadsheet behavior after opening CSV, screen capture, compromised build/dependency/native providers, denial-of-service beyond tested limits, and arbitrary tampering by an attacker who can write application/user-data files.

## Security review checklist

For a security/data-safety change:

- identify assets and trust boundaries;
- add failure-path tests;
- verify no new secret persistence;
- verify parameterized SQL/literal wildcard behavior;
- verify input bounds/termination;
- verify transactional all-or-nothing behavior for multi-row destructive writes;
- verify confirmation direction/text matches the destructive operation;
- verify backup/restore recovery behavior;
- verify errors avoid private payloads;
- review dependency/native licensing;
- run CI/CodeQL on the final head;
- update security/user/maintenance documentation.
