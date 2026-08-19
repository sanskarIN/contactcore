# ContactCore Threat Model

## Scope

ContactCore is an offline-first desktop address book. The primary assets are local contact records, backups/exports, optional database-encryption keys, application settings, and the integrity of migrations/releases. The default design has no mandatory account, analytics service, advertising SDK, or cloud synchronization backend.

This document models risks in the application and repository. It does not claim to protect a device that is already fully compromised by an administrator/root-level attacker.

## Trust boundaries

1. **User input → application** — names, phone numbers, email addresses, notes, tags, group names, file paths, CSV, and vCard content are untrusted.
2. **Application → SQLite** — all values must cross through parameterized statements or controlled schema SQL.
3. **External files → import/restore** — CSV/vCard files and SQLite backups may be malformed, oversized, corrupted, or deliberately hostile.
4. **Application → filesystem** — local database, settings, backups, and exports can contain sensitive information.
5. **Application → operating system** — file pickers, external links, native SQLite provider loading, and platform packaging depend on OS trust.
6. **Repository → build/release pipeline** — dependencies, Actions, release tags, and signing material are software-supply-chain boundaries.

## Threats and controls

### Accidental disclosure of contacts

**Threat:** a cloud-only design, telemetry, logs, screenshots, bug reports, or exported files expose personal contact data.

**Controls:**

- Offline-first architecture and no mandatory cloud account.
- No analytics/advertising dependency in the baseline.
- PII-oriented diagnostic redaction for common email/phone patterns.
- Documentation repeatedly warns against uploading real databases to public issues/discussions.
- Screenshots and fixtures must use fictional data.
- Backups/exports are explicit user actions.

**Residual risk:** users can intentionally copy/export sensitive files, and OS backup/sync products may include the local data directory according to the user's own configuration.

### SQL injection

**Threat:** crafted search/contact/group/tag text changes SQL behavior.

**Controls:** user-controlled values are passed as SQLite parameters. Dynamic table names are limited to a hard-coded internal allow-list during aggregate child replacement. LIKE wildcards are escaped before binding.

### Malicious or corrupted backup

**Threat:** restoring an unrelated or corrupted SQLite file destroys the current address book or causes unsafe migration behavior.

**Controls:**

- Restore opens the candidate first.
- SQLite integrity checking runs before replacement.
- A recognizable ContactCore `contacts` table is required.
- The existing database is backed up before replacement.
- Replacement uses a staging file.
- Migrations use transactions and integrity checks.
- Databases with schema versions newer than the application supports are rejected rather than downgraded.

**Residual risk:** an attacker who can arbitrarily replace files in the application data directory already has significant local filesystem access. Users should keep independent backups.

### Import parser abuse

**Threat:** malformed CSV/vCard content crashes the app, consumes excessive resources, or injects dangerous content into later exports.

**Controls:** parsers are deterministic and have malformed/randomized-input tests; imported values are validated before persistence. The app does not evaluate imported content as code.

**Planned hardening:** explicit configurable import-size limits and deeper property/fuzz testing before a stable release.

### Spreadsheet formula injection

**Threat:** CSV data beginning with spreadsheet formula markers becomes active when a user opens an export in spreadsheet software.

**Current posture:** ContactCore treats CSV as a data interchange format and quotes fields according to CSV rules, but quoting alone does not neutralize every spreadsheet application's formula interpretation.

**Required stable-release control:** either provide a spreadsheet-safe export mode that prefixes formula-leading cells or document CSV as machine interchange and recommend vCard for ordinary contact exchange. Do not claim formula-safety until the control is implemented and tested.

### Database encryption misconfiguration

**Threat:** a user configures an encryption key and assumes the database is encrypted while ordinary SQLite silently ignores `PRAGMA key`.

**Controls:** after applying the key, ContactCore queries `PRAGMA cipher_version`. If a SQLCipher-compatible provider is not actually active, startup fails. Backups and restore probes use the same configured connection factory so encrypted-mode operations cannot silently downgrade to plaintext.

**Residual risk:** the repository does not ship a cross-platform SQLCipher binary by default. Packaging a maintained provider remains release work and can have licensing/distribution implications.

### Secret leakage

**Threat:** encryption keys, signing secrets, API tokens, real data, or private endpoints enter Git history or logs.

**Controls:** `.gitignore` excludes common secret/database/artifact files; `.env.example` contains names/placeholders only; application preferences do not persist the database key; documentation requires GitHub secret scanning/push protection where available; workflows use repository-provided tokens rather than committed credentials.

### Dependency and workflow compromise

**Threat:** malicious or compromised dependencies/Actions execute during builds or releases.

**Controls:** central version pinning, Dependabot, CodeQL, limited workflow permissions, reviewable GitHub Actions YAML, and branch-protection guidance. Release signing credentials are intentionally absent until a protected signing design exists.

**Planned hardening:** pin third-party Actions by full commit SHA for higher-assurance releases after verifying maintained upstream revisions.

### Destructive UI action

**Threat:** accidental permanent deletion causes data loss.

**Controls:** archive is available as a reversible alternative; permanent deletion can require explicit confirmation; SQLite child rows cascade only after the parent delete is intentionally executed; backup tooling is built in.

### Denial of service / large datasets

**Threat:** huge contact sets, pathological duplicate buckets, or oversized imports cause slow UI or excessive memory use.

**Controls:** debounced search, indexed common fields, cancellation-aware I/O, virtualized list control, and duplicate candidate blocking rather than unconditional all-pairs comparison.

**Planned hardening:** measured database materialization benchmarks, import-size budgets, and batch child-loading for very large result sets.

## Privacy principles

- Local by default.
- No forced sign-in for core functionality.
- No donation requirement for functionality.
- No real contact data in repository fixtures.
- Minimize diagnostic content and redact likely PII.
- Be explicit when users export or back up sensitive data.

## Security validation checklist

Before a stable release:

- Run CI on Windows, Linux, and macOS.
- Run CodeQL and dependency/security checks.
- Verify no secrets or real databases are tracked.
- Test malformed CSV/vCard inputs and oversized inputs.
- Exercise migration paths from every released schema version.
- Exercise backup/restore, including corrupted and wrong-schema databases.
- Validate SQLCipher-compatible packaging on every platform before advertising encrypted builds.
- Review all external-link/file-picker interactions.
- Test delete/archive/restore behavior manually.
- Review release artifacts and signing/notarization claims.

Report vulnerabilities privately according to `SECURITY.md`.
