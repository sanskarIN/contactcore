# Security and encryption

## Threat model
ContactCore protects against accidental cloud disclosure by being offline-first, against common SQL injection by parameterized statements, against partially restored data by staging/integrity checks, and against misleading encryption configuration by fail-closed verification.

## Database encryption option
The open-source default build uses ordinary SQLite. If `CONTACTCORE_DATABASE_KEY` is set, `SqliteConnectionFactory` sends a hex-encoded `PRAGMA key` and then queries `PRAGMA cipher_version`. If no cipher version is reported, startup fails. This means users may integrate a maintained SQLCipher-compatible native SQLite distribution without ContactCore ever silently treating plaintext SQLite as encrypted.

As of August 2026, the formerly convenient community encryption bundles in SQLitePCLRaw are deprecated. For production encryption, use a currently supported vendor/native build (for example official SQLCipher builds from its maintainer) or a carefully maintained internal native build. Do not commit proprietary binaries or license keys to this repository.

## Secrets
No app secret is required. Database keys should be supplied at runtime or by an OS secret store integration; `.env` is ignored and `.env.example` contains names only.
