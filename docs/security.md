# Security and Privacy Engineering

ContactCore is an offline-first desktop contact manager. Its security model is primarily about protecting local personal data from accidental disclosure, injection/input mistakes, misleading encryption claims, corrupt restores, and destructive-operation errors. It is not a network security product and does not claim to defend a compromised operating system from an attacker already able to read the user's files or process memory.

For vulnerability-reporting instructions, see `../SECURITY.md`. For user-facing data practices, see `../PRIVACY.md`.

## Assets

Sensitive assets can include:

- the active `contactcore.db` database;
- WAL/SHM sidecars while SQLite uses them;
- verified backups and pre-restore snapshots;
- failed-restore recovery copies;
- CSV/vCard exports;
- temporary restore-picker files;
- contact values shown on screen;
- runtime database encryption key, when configured;
- settings that influence destructive-action safety.

Backups and exports must be treated as copies of personal data, not as harmless generated artifacts.

## Trust boundaries

### Local operating system

ContactCore trusts the OS/runtime to enforce filesystem permissions, process isolation, and native file-picker behavior. A malicious administrator, malware running as the user, disk-forensics attacker against unencrypted storage, or compromised native dependency is outside the protection offered by normal application-layer controls.

### Imported files

CSV/vCard files are untrusted input. They may be malformed, very large, Unicode-heavy, or intentionally crafted to stress parsers/downstream tools.

### Selected backup files

A `.db` chosen for restore is untrusted until ContactCore verifies it. A valid SQLite file is not automatically a ContactCore database.

### Native SQLite provider

The default app uses normal SQLite. If a user integrates a SQLCipher-compatible provider, the native provider becomes part of the trusted computing base.

### Public repository/support channels

Issues, pull requests, screenshots, logs, and test fixtures are public-data boundaries. Real contact information, databases, exports, and secrets must not cross this boundary.

## Offline-first privacy posture

The application has no mandatory cloud synchronization, account, telemetry, advertising, or analytics dependency. Normal contact operations use local files/SQLite.

This reduces remote data exposure but does **not** automatically encrypt local storage or backups. Local-first and encrypted-at-rest are separate properties.

## SQL injection controls

`SqliteContactRepository` parameterizes contact IDs and values for normal CRUD/search operations. User search text used in `LIKE` expressions is also escaped for backslash, `%`, and `_` so wildcard characters are interpreted literally when appropriate.

Schema/table names used in fixed repository loops are developer-controlled constants, not user input.

Developers must preserve this rule: never concatenate contact/import/search values into SQL text.

## SQLite foreign keys and consistency

Every factory-opened connection enables:

```sql
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 5000;
```

Foreign keys cascade contact deletion into child/link rows. Aggregate and bulk-import writes occur inside SQLite transactions.

A database error while a multi-contact batch is being written rolls back the batch instead of committing the successful prefix.

## Database schema identity

Schema version 2 introduces:

```text
app_metadata['schema_family'] = 'contactcore'
```

Backup/restore verification uses this identity plus required tables and schema version. This helps reject an unrelated valid SQLite file that should not replace ContactCore data.

Future schema versions are rejected instead of being opened as though downgrade compatibility were guaranteed.

## Backup integrity controls

Backup creation uses SQLite's native backup API and then verifies:

- `PRAGMA integrity_check` returns `ok`;
- required ContactCore tables exist;
- schema version is valid/supported;
- current backups contain the ContactCore schema-family marker.

The path is reported as a successful backup only after these checks.

## Restore safety controls

Restore uses a staged sequence:

1. normalize/validate selected path;
2. reject active-database-as-source;
3. open selected backup read-only;
4. verify integrity/ContactCore structure/version;
5. snapshot the current active database to a verified recovery copy when one exists;
6. copy selected backup to staging;
7. migrate staging to the current supported schema;
8. verify staging;
9. clear pools/sidecars and switch staged file into the active path;
10. verify the new active database;
11. on final verification failure, retain the failed copy and restore the verified pre-restore snapshot.

This ordering aims to prevent a corrupt, unrelated, unsupported, or migration-failing backup from silently destroying the only good active copy.

See `storage-backup-recovery.md` for exact behavior.

## Optional database encryption

### Default build

The repository's default SQLite dependency is `Microsoft.Data.Sqlite`. Ordinary SQLite does not provide SQLCipher encryption merely because `PRAGMA key` is issued.

### Fail-closed key request

When `CONTACTCORE_DATABASE_KEY` is non-empty, `SqliteConnectionFactory`:

1. obtains UTF-8 bytes of the runtime key;
2. converts them to hex;
3. issues a hex-literal `PRAGMA key` without interpolating the original string;
4. queries `PRAGMA cipher_version`;
5. closes the connection and throws when no cipher version is returned.

Therefore a key configured against an incompatible provider should cause failure, not a false “encrypted” state.

### Provider responsibility

Production encryption requires a maintained SQLCipher-compatible native SQLite distribution or another deliberately supported provider integration. Maintainers must evaluate licensing, platform packaging, updates, and runtime compatibility.

Do not commit proprietary encryption binaries, license material, or secret keys unless repository licensing/policy explicitly permits them.

### What the application does not guarantee

- It does not protect an encryption key from a process-memory attacker.
- It does not provide an OS secret-store implementation today.
- It does not encrypt ordinary CSV/vCard exports automatically.
- It does not magically encrypt a normal SQLite database without a compatible provider.
- It does not claim cryptographic guarantees beyond those of the integrated provider.

## Secret handling

ContactCore requires no application API secret for normal use.

`JsonAppPreferences` reads `CONTACTCORE_DATABASE_KEY` from the process environment and exposes it at runtime through the preference abstraction, but its serialized settings model excludes `DatabaseKey`. Tests assert that the key/property name are not written to `settings.json`.

`.env.example` contains variable names/documentation only. Never put a real key into tracked files.

For stronger deployments, an OS credential/secret-store adapter would be preferable to a long-lived environment variable; that integration remains future work.

## Preferences integrity

Settings save through a temporary file followed by replacement. Malformed JSON loads with conservative defaults, including confirmation before permanent delete enabled.

This is resilience rather than an authentication mechanism: a local attacker able to modify the user's settings file can still change non-secret preferences.

## Destructive-action confirmation

Permanent contact deletion defaults to confirmation-on. If confirmation is enabled but the desktop confirmation callback is unavailable, deletion is blocked.

Restore always requires a confirmation callback in the desktop workflow and informs the user that a pre-restore snapshot will be retained.

These controls address accidental actions, not malicious local automation with code execution in the user's context.

## Import resource limits

The native desktop import reader limits selected text to 5,000,000 characters. It reads UTF-8 with BOM detection and throws a controlled error when the limit would be exceeded.

The parser itself operates on an in-memory string, so this desktop limit is an important resource bound. Alternate future import entry points must enforce their own appropriate limits rather than assuming every caller passes through `MainWindow`.

## Import validation and atomicity

Parsing and domain validation are separate. `ContactService.ImportAsync` deep-copies and normalizes contacts, validates the complete batch, and rejects it before repository persistence when any issue exists.

The SQLite repository then writes the batch in one transaction. This reduces partial-import data integrity risk.

## CSV spreadsheet risk

The CSV codec performs standards-style quoting but currently does not neutralize spreadsheet formula prefixes. If an export contains contact text beginning with a formula-significant character, downstream spreadsheet software may interpret it according to its own rules.

Users should treat exported CSV as untrusted data when opening it in a spreadsheet. If formula neutralization is introduced, its compatibility policy and tests must be explicit rather than silently modifying contact values.

## vCard limitations

The vCard implementation is a focused subset. Unknown/unsupported properties are generally ignored. An unterminated card is ignored with a warning.

Do not claim full RFC interoperability or assume unsupported property encodings are sanitized for every external application.

## Diagnostic message redaction

`RedactingLog.Sanitize`:

- replaces email-shaped text with `[email-redacted]`;
- replaces long phone/number-shaped text with `[number-redacted]`;
- truncates output beyond 2,000 characters.

The desktop uses this before displaying caught exception messages.

This is **defense-in-depth, not a complete PII classifier**. Names, addresses, unusual identifier formats, or secrets that do not match these patterns can remain. Lower layers should therefore avoid placing sensitive payloads in exception messages in the first place.

## Validation-message privacy

Email/phone validation messages state that the field is invalid without echoing the submitted value. Tests explicitly assert that invalid example values do not appear in validation message text.

## File-picker temporary restore copies

Some Avalonia storage providers may not expose a normal local filesystem path. The desktop restore picker then copies the chosen stream into a unique system temporary file and marks it for deletion after the restore attempt.

Deletion is best-effort for common I/O/authorization errors. Sensitive temporary-file lifecycle therefore still depends on OS/filesystem behavior. A future hardening pass can evaluate stronger secure-temp handling per platform.

## Logging and telemetry

The current application does not contain a mandatory telemetry pipeline. If logging is added later:

- default to local-only diagnostic output;
- avoid contact contents/keys/database paths where unnecessary;
- use structured event categories rather than payload dumps;
- document retention and location;
- require explicit design review before remote telemetry.

## Dependency security

- Package versions are centrally managed.
- Dependabot is configured.
- CodeQL analyzes C# on pushes/PRs to `main` and weekly.
- CI builds/tests across Windows, Ubuntu, and macOS.

Dependency updates must still be reviewed for native/provider/licensing and behavior changes. An automated update PR is not evidence that a package is safe or compatible.

## Release security

Current release automation publishes self-contained single-file artifacts for Windows x64, Linux x64, macOS x64, and macOS arm64. It does not currently document code signing/notarization steps.

Do not describe release files as signed or notarized unless those operations are implemented and verified. Download reputation warnings are expected for unsigned binaries on some platforms.

## Threats not currently mitigated by the application

Examples include:

- malware or another same-user process reading files/process memory;
- stolen unencrypted device/storage;
- weak OS account password or permissions;
- insecure backup destinations selected by the user;
- malicious spreadsheet behavior after opening a CSV export;
- screen capture/shoulder surfing;
- compromised compiler/dependency/native provider/build runner;
- denial of service within limits not yet benchmarked;
- arbitrary tampering by an attacker with write access to the application files and user data.

These should be stated plainly rather than implying “offline-first” solves all local security concerns.

## Security review checklist

For a security-sensitive change:

- identify the asset and trust boundary;
- add failure-path tests;
- confirm no new secret persistence;
- confirm SQL remains parameterized;
- confirm import/resource bounds;
- confirm rollback/recovery behavior for data replacement;
- confirm errors avoid sensitive payloads;
- review native/dependency licensing and update channel;
- run CI/CodeQL on the final branch head;
- update this document and `SECURITY.md` where needed.
