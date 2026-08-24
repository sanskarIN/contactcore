# ADR 0002: Use SQLite for Local Contact Persistence

- **Status:** Accepted
- **Scope:** Primary local persistence
- **Related:** ADR 0001, ADR 0003

## Context

ContactCore needs a durable local store for structured contacts with repeated phones/emails/addresses/organizations and many-to-many groups/tags. The store must support transactions, migrations, indexed search/filter queries, cross-platform desktop packaging, and reliable backup/restore without requiring a server.

The product is explicitly offline-first. Requiring PostgreSQL, SQL Server, a cloud database, or a background server process would add installation and privacy complexity inconsistent with normal desktop use.

A flat JSON/document file would be simple initially but would make transactional multi-record updates, relationships, indexing/querying, migrations, and safe backup of concurrent writes more difficult as the model grows.

## Decision

Use SQLite as the primary local database through `Microsoft.Data.Sqlite`.

The concrete persistence implementation lives in `ContactCore.Infrastructure` behind `IContactRepository` owned by Application.

## Connection policy

`SqliteConnectionFactory` centralizes connection creation. Normal active-database connections use:

- full normalized data-source path;
- `ReadWriteCreate` mode;
- shared cache;
- pooling enabled;
- `PRAGMA foreign_keys = ON`;
- `PRAGMA busy_timeout = 5000`.

One-off backup/probe paths can disable pooling and use read-only mode where appropriate.

Optional keyed-SQLite handling is governed by ADR 0003.

## Schema management

`DatabaseMigrator` creates/uses `schema_migrations` and applies ordered integer migrations.

Current history:

- **Version 1:** contact/root/child/group/tag schema and core indexes.
- **Version 2:** `app_metadata` with `schema_family = contactcore`.

A database reporting a migration version greater than the running build's `LatestSchemaVersion` is rejected. ContactCore does not attempt automatic downgrade migrations.

Each pending migration runs in a transaction and records an application timestamp after the migration SQL succeeds.

## Data mapping

`Contact` is treated as a complete aggregate.

`contacts` stores scalar root fields. Contact-owned repeated values live in child tables. Groups/tags are shared dictionary rows linked through join tables.

For an upsert, the repository:

1. upserts the root contact;
2. deletes that contact's current owned child/link rows;
3. inserts the supplied aggregate's child/link rows;
4. commits all changes together.

`UpsertManyAsync` performs this process for the complete supplied batch inside one SQLite transaction.

This replacement strategy favors deterministic aggregate state over complex per-child diffing.

## Search model

Repository search composes fixed SQL clauses for:

- archived visibility;
- favorite-only;
- free text across names, phone, and email;
- exact case-insensitive tag/group name;
- starting letter.

Data values are parameterized. User free-text is escaped for SQL `LIKE` wildcard metacharacters before being wrapped in the intended `%...%` pattern.

## Backup/restore interaction

SQLite's native backup API is used for verified backups instead of relying on raw copying of a possibly active WAL database file.

Restore treats a selected SQLite file as untrusted until integrity, required table, migration-version, and ContactCore identity checks pass. Restore stages/migrates/verifies before switching the active file and retains a rollback snapshot.

The schema-family marker introduced in version 2 exists partly to make this restore boundary safer.

## Consequences

### Positive

- No database server to install/manage.
- Mature transactional storage.
- Cross-platform availability.
- Foreign keys and indexes support the relational contact model.
- Parameterized SQL is straightforward.
- Native backup API supports consistent snapshots.
- Single database file is convenient for user-managed local backups, while still requiring sidecar-aware backup logic when active.
- Migrations can evolve the schema explicitly.

### Negative

- Native SQLite/provider behavior is part of desktop packaging/runtime compatibility.
- SQLite is not a multi-user network database.
- Current child loading causes several queries per returned contact and may need optimization for large data sets.
- Leading-wildcard search is not equivalent to a full-text index.
- A complete aggregate replacement can drop child data if an upstream editor fails to preserve hidden fields; UI/application code must account for this contract.
- Cross-process write contention is limited compared with server databases, though the app is primarily designed as one local desktop process.

## Alternatives considered

### JSON file

Rejected as the primary store because atomic relational updates, querying/indexing, migrations, and many-to-many relationships would need to be recreated manually.

### Embedded document database

Not selected because SQLite already meets current query/transaction/portability needs with a familiar ecosystem and simple file model.

### External relational server

Rejected for normal desktop use because it adds deployment/credential/network complexity and conflicts with the simple offline-first experience.

### ORM over SQLite

Not required currently. Explicit SQL keeps migrations and aggregate write/search behavior transparent. An ORM could be reconsidered if model/query complexity grows enough to justify it, but migration/data-safety behavior would still need explicit review.

## Guardrails

- Do not construct differently configured SQLite connections throughout the codebase; use the factory.
- Do not interpolate untrusted values into SQL.
- Keep foreign keys enabled.
- Preserve transaction boundaries for aggregate and bulk writes.
- Add numbered migrations; do not mutate released migration meaning.
- Preserve future-version rejection.
- Keep backup/restore identity/integrity checks in sync with schema evolution.
- Treat editor preservation of complete aggregates as a correctness requirement.

## When to revisit

Revisit if measured data volume/search needs exceed the current SQLite design, multi-user concurrent/network access becomes a product requirement, or platform support makes the native provider impractical.

Before replacing SQLite, define a data migration/export path and preserve the local-first privacy model unless the product explicitly changes direction.
