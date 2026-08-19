# Security Engineering Notes

This document describes controls implemented in the codebase and rules for future changes. Vulnerability reporting instructions are in `SECURITY.md`; broader risks are in `THREAT_MODEL.md`.

## Input validation

`ContactService.SaveAsync` normalizes user-editable strings and invokes `ContactValidation` before persistence. Email parsing uses `System.Net.Mail.MailAddress`; phone input is constrained to a conservative length/character pattern; major text fields have explicit length limits.

Import codecs operate on untrusted text. Any UI that exposes bulk import must add file-size and record-count limits before passing content to a codec.

## SQL safety

All contact/search values are passed through SQLite parameters. Search wildcard characters are escaped before use with `LIKE ... ESCAPE '\\'`. Dynamic table/column fragments in group/tag helper methods are internal constant arguments only and must never be replaced with user-provided identifiers.

SQLite foreign keys are enabled on opened connections. Contact aggregate writes use an explicit transaction.

## Filesystem safety

Backup and export paths are local paths selected/constructed by application workflows. Backup candidates are opened separately and checked with `PRAGMA integrity_check`. Restore stages a copy before replacing the active database and re-runs schema initialization afterward.

The application must not automatically follow arbitrary URLs from imported contact data without an explicit user action and safe URI handling.

## Secrets

ContactCore currently has no application API keys or account credentials. `.env.example` contains placeholders only. Future secrets must come from environment variables, OS credential storage, or CI secret stores and must never be committed.

## Privacy and logs

Do not log:

- contact names;
- email addresses or phone numbers;
- postal addresses;
- notes or birthdays;
- raw import/export contents;
- database contents;
- secrets, tokens, auth headers, or signing material.

Structured logs, if added, should use event IDs and non-sensitive metadata such as operation type, elapsed time, result category, record count, and sanitized exception class.

## Database encryption

The default Microsoft.Data.Sqlite configuration is **not** advertised as encrypted. Filesystem/OS protections are the baseline. If SQLCipher or another provider is added, the adapter must:

1. verify encryption support at startup;
2. fail closed when the provider/key is unavailable;
3. avoid exposing keys in command lines/logs/config files;
4. define backup/restore migration behavior;
5. receive dedicated integration tests and threat-model review.

## Dependency and CI security

- NuGet versions are centrally managed.
- Dependabot covers NuGet and GitHub Actions.
- CodeQL analyzes C# on pushes, pull requests, and a weekly schedule.
- CI runs format/build/tests.
- Release jobs run tests before publishing.

Future high-assurance hardening should pin external GitHub Actions to immutable commit SHAs and add a license/SBOM step.

## Security checklist for changes

Ask whether the change:

- introduces network access;
- handles a new untrusted file/data format;
- changes database schema or migration behavior;
- adds OS permissions/integration;
- changes deletion/backup/restore semantics;
- stores a new category of personal data;
- introduces a new dependency or native binary;
- weakens input limits, transactionality, or error redaction.

If yes, update tests, documentation, and `THREAT_MODEL.md` before merge.
