# ADR 0004: Encryption requests fail closed

- Status: Accepted
- Date: 2026-08-19

## Context

The repository advertises local privacy and includes an environment placeholder for a future SQLCipher-compatible key. Silently accepting a key while opening ordinary SQLite would create a dangerous false sense of encryption.

## Decision

The default build uses ordinary `Microsoft.Data.Sqlite` and makes no encryption claim. If `CONTACTCORE_DATABASE_KEY` is non-empty while no encrypted provider is wired, ContactCore refuses startup with a non-secret error message.

A future encrypted provider must be a separately reviewed infrastructure implementation with key-management, migration, backup/restore, failure-mode, and integration-test coverage.

## Consequences

- Users are not silently downgraded to plaintext after asking for encryption.
- Default storage remains compatible and dependency-light.
- Encrypted storage is unavailable until deliberately implemented.
- Documentation must continue to state that normal SQLite files rely on OS/filesystem protection.
