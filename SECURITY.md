# Security Policy

Security reports are welcome. ContactCore manages personal contact data, so reports involving data loss, unintended disclosure, unsafe backup/restore, injection, encryption-state confusion, or dependency/release compromise are treated seriously.

For the detailed technical threat model and current controls, see [`docs/security.md`](docs/security.md).

## Supported versions

Until a formal long-term-support matrix exists, security fixes target:

- the latest released version, when one exists;
- the current `main` branch for upcoming fixes.

Older tags/branches may not receive fixes unless a release note explicitly states otherwise.

## Reporting a vulnerability privately

Do **not** open a public issue, discussion, pull request, or social-media post for an undisclosed vulnerability.

Contact:

- **sanskarin@outlook.in**
- **sanskarin.business@gmail.com**

Include only the information needed to reproduce/understand the issue:

- affected ContactCore version/commit;
- operating system/architecture;
- concise vulnerability description;
- security/data-loss impact;
- minimal reproduction steps using fictional data;
- whether special environment variables/native providers are required;
- suggested mitigation, if you have one.

Do not send real user contact databases, real exports, other people's personal information, passwords, encryption keys, signing material, or unrelated secrets. If a database-shaped reproducer is genuinely necessary, create the smallest possible one containing fictional records only.

## What to expect

A maintainer will evaluate whether the issue is reproducible and in scope. Please allow time for analysis and a safe fix before public disclosure. Complex issues may require coordinated dependency/platform investigation.

A report may be closed as not-applicable when it relies entirely on an already-compromised OS/user account or another condition outside the application's stated threat model, but such reports can still reveal useful hardening opportunities.

## In-scope examples

Examples include:

- SQL injection or query construction that changes intended semantics;
- contact data exposed through application diagnostics unexpectedly;
- database encryption reported/effectively treated as enabled when no compatible cipher provider is active;
- runtime database key written into normal settings/logs;
- corrupt/unrelated/future-schema backup replacing valid active data despite intended restore checks;
- restore rollback/snapshot behavior that can destroy the only good copy;
- import parser/resource issue bypassing intended limits with practical impact;
- destructive action bypassing configured confirmation because of application logic;
- unsafe release/workflow permission or secret exposure;
- dependency vulnerability that is reachable in ContactCore;
- path/file handling issue allowing unintended overwrite/read within the application's privileges;
- privacy leak introduced by a new network/telemetry behavior.

## Usually out of scope without an application-specific weakness

- someone with full access to the user's OS account reading an unencrypted local database;
- malware reading process memory/environment variables;
- screen capture/shoulder surfing;
- insecure permissions deliberately applied by the OS/user to an export/backup destination;
- social engineering unrelated to ContactCore code;
- denial-of-service requiring already privileged local machine control with no meaningful ContactCore-specific impact;
- claims that ordinary default SQLite is not encrypted (this is documented behavior, not a vulnerability).

## Current security posture

ContactCore currently includes these relevant controls:

- offline-first operation with no mandatory account/cloud/telemetry/advertising dependency;
- parameterized SQLite data values;
- literal `LIKE` wildcard escaping for search text;
- foreign-key enforcement and transactional aggregate/bulk writes;
- future-schema rejection;
- ContactCore schema-family marker;
- SQLite-native backup creation plus integrity/version/identity verification;
- staged restore with pre-restore verified snapshot and rollback path;
- fail-closed optional SQLCipher-compatible provider check when a database key is requested;
- runtime database key excluded from normal `settings.json` persistence;
- bounded desktop text imports (5,000,000 characters);
- validation messages that avoid echoing invalid phone/email values;
- diagnostic redaction of common email/phone/long-number patterns;
- permanent-delete confirmation enabled by default;
- confirmation-required desktop restore;
- cross-platform CI, CodeQL, and Dependabot.

These controls reduce risk but are not claims of formal security certification.

## Encryption clarification

The default build uses ordinary SQLite. Setting `CONTACTCORE_DATABASE_KEY` only requests keyed SQLite behavior; ContactCore verifies `PRAGMA cipher_version` and refuses to proceed if a compatible provider is not actually loaded.

Therefore:

- default plaintext SQLite is expected unless a supported compatible provider is integrated;
- a configured key with ordinary SQLite should cause failure, not silent plaintext success;
- encryption strength/licensing/platform behavior belongs to the integrated provider and must be tested separately.

See `docs/adr/0003-encryption-provider.md`.

## Public disclosure after a fix

Once a fix is available and users have a reasonable opportunity to update, a maintainer may publish a concise advisory/changelog entry describing impact, affected versions, fixed versions, and migration/mitigation steps without unnecessarily exposing private reporter information.

## Security-related pull requests

Do not submit a public fix PR for an undisclosed issue before coordination if the diff would reveal an exploitable vulnerability. Use private reporting first.

For normal hardening that is not an undisclosed vulnerability, public PRs are welcome and should include tests plus security/documentation updates.

## Dependency reports

If reporting a CVE/security advisory in a dependency, include the package/action/native component name, affected version range, upstream advisory identifier, and evidence the vulnerable path is relevant to ContactCore. Automated scanner output alone may be insufficient to establish exploitability.

## Contact-data privacy during investigation

The preferred security reproducer is always generated fictional data. Never request or provide a real user's entire contact database merely to make investigation convenient.
