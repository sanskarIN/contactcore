# Security Policy

## Supported versions
Security fixes target the latest released version and the current `main` branch.

## Reporting a vulnerability
Please do not open a public issue for an undisclosed vulnerability. Email **sanskarin@outlook.in** or **sanskarin.business@gmail.com** with a concise description, affected version, and non-destructive reproduction information. Do not include real contact databases or other people's personal data.

## Security design
ContactCore is offline-first, does not require an account, parameterizes database writes/queries, redacts common PII patterns from diagnostic text, and performs backup integrity checks before restore. If a database key is configured, the runtime verifies that a SQLCipher-compatible provider is actually active rather than silently claiming encryption.

See `docs/security.md` for the threat model and encryption-provider integration notes.
