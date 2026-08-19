# Security Policy

## Supported versions

Security fixes are applied to the current `main` branch and the latest published release. Older pre-release builds may not receive backports.

## Reporting a vulnerability

Please do **not** open a public issue for a suspected vulnerability that could expose contact data, corrupt databases, bypass expected local protections, or leak secrets.

Report security concerns privately to:

- `sanskarin@outlook.in`
- `supportramsandesh@gmail.com`

Include the affected version or commit, operating system, reproduction steps using fictional data, expected impact, and any proposed mitigation. Do not send real contact databases, credentials, tokens, or other personal data.

The maintainer will acknowledge the report when practical, investigate reproducible issues, coordinate a fix and release, and credit reporters who request attribution when disclosure is safe.

## Security model

ContactCore is local-first and does not require an account, telemetry service, or cloud synchronization. Its primary security responsibilities are protecting the integrity and confidentiality of locally stored contact data, safely parsing imported files, preventing SQL injection, avoiding accidental disclosure through logs or diagnostics, and producing trustworthy backups.

Database encryption is not claimed unless a supported encrypted SQLite provider is explicitly configured and verified. A normal ContactCore database should be treated as readable by any process or user account that can read the database file.

See `THREAT_MODEL.md` and `docs/security.md` for trust boundaries, mitigations, and hardening guidance.
