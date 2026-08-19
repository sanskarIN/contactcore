# ContactCore Threat Model

## Scope

This document covers the local desktop application, SQLite database, imports/exports, backups, and repository build/release pipeline. ContactCore currently has no application-level cloud service or account system.

## Assets

- Contact names, communication details, addresses, organizations, notes, birthdays, groups, and tags.
- User-created CSV/vCard exports and SQLite backups.
- Data integrity and availability of the active database.
- Release artifacts and source-code integrity.

## Trust boundaries

1. **User input → domain/application:** all typed fields are untrusted until validated.
2. **Import file → parser/domain:** CSV and vCard files may be malformed or malicious.
3. **Application → SQLite:** SQL must remain parameterized and transactions must preserve aggregate consistency.
4. **Application → filesystem:** export/backup paths can fail because of permissions, full disks, races, or hostile replacement.
5. **Repository → CI/dependency ecosystem:** actions and NuGet dependencies are external supply-chain inputs.
6. **Local OS account → database file:** anyone/process with filesystem read access may read an unencrypted normal SQLite database.

## Abuse cases and mitigations

### SQL injection

**Risk:** crafted search or contact text alters database queries.

**Mitigation:** user values are passed as SQLite parameters. Dynamic SQL is limited to internal constant table/column names in generic group/tag helpers, never user-controlled identifiers.

### Malformed import files

**Risk:** malformed quoting, extreme fields, or invalid contact values cause corruption or denial of service.

**Mitigation:** codecs reject structurally invalid input where detected; domain validation runs before normal application saves; import workflows should enforce file-size limits and transactional writes before a release exposes bulk import UI.

### Database corruption / partial writes

**Risk:** crashes or I/O failures leave a partially written contact aggregate.

**Mitigation:** contact plus child collections are written in one SQLite transaction; foreign keys use cascade rules; schema changes are versioned; backups run through SQLite's backup API and an integrity check.

### Backup replacement

**Risk:** restoring a corrupt or attacker-controlled file destroys active data.

**Mitigation:** restore validates the candidate with `PRAGMA integrity_check` before replacement, stages the copy, clears SQLite pools, replaces the active file, and re-runs initialization/version checks. The planned UI must require explicit user confirmation.

### Local confidentiality

**Risk:** another process/user reads the database, export, or backup.

**Mitigation:** ContactCore relies on operating-system account/filesystem protections by default and clearly documents that ordinary SQLite is not encrypted. No false encryption claim is made. Optional encrypted-provider support must fail closed and receive separate security review.

### Sensitive logging

**Risk:** diagnostics expose contact data.

**Mitigation:** user-facing error mapping avoids raw contact content. Contribution rules prohibit logging names, addresses, phone/email data, notes, database contents, secrets, or auth headers. Future structured logging must include explicit redaction.

### CSV formula injection

**Risk:** exported contact text beginning with spreadsheet formula prefixes can execute or trigger external links when opened in spreadsheet software.

**Status:** accepted residual risk for the initial generic CSV codec; Phase 2 should add an explicit spreadsheet-safe export mode that prefixes dangerous cell values while preserving a lossless raw CSV mode.

### Resource exhaustion

**Risk:** huge imports or broad result sets consume excessive memory/CPU.

**Mitigation:** repository search limits are clamped to 1000 per request. Import file-size and record-count limits remain required before bulk import is surfaced in the UI.

### Supply-chain compromise

**Risk:** a malicious dependency/action affects builds or releases.

**Mitigation:** dependencies are centrally pinned, Dependabot is configured, CodeQL/CI run on changes, and release builds are reproducible from repository configuration. Future hardening should pin GitHub Actions to full commit SHAs for high-assurance releases.

## Residual risks

- The active database is not encrypted by default.
- The initial desktop UI does not yet expose safe import-size limits because bulk import UI is not released yet.
- Search currently materializes each matched aggregate through multiple child queries; this affects performance rather than confidentiality/integrity.
- Release artifacts are not yet code-signed by platform-specific certificates.

## Security review triggers

Revisit this threat model when adding cloud sync, network APIs, authentication, plugins, contact sharing, automatic updates, encrypted storage, OS contact integration, reminders/background tasks, or new import formats.
