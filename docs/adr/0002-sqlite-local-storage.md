# ADR 0002: SQLite for local contact storage

- Status: Accepted
- Date: 2026-08-19

## Context

ContactCore is offline-first and should not require a server. It needs transactional multi-table writes, indexed search, schema evolution, durable backups, and portability across Windows, macOS, and Linux.

## Decision

Use SQLite through `Microsoft.Data.Sqlite` as the primary local store. Keep database access behind `IContactRepository` and migration/connection behavior in `SqliteDatabase`.

Store contact roots and repeating fields in normalized tables, enforce foreign keys, use transactions for aggregate writes, track schema versions explicitly, and verify backups/restores with SQLite integrity checks.

## Consequences

- No server/database daemon is required.
- Backups can use SQLite's native backup API.
- SQL/query performance remains observable and tunable.
- Local filesystem access becomes part of the security/privacy boundary.
- Default SQLite is not encryption; confidentiality relies on OS/filesystem protections unless a separately reviewed encrypted provider is configured.
- Very large datasets may require batching/query-plan improvements but do not justify a server by default.
