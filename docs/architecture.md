# Architecture

ContactCore is a modular monolith. The goal is a small number of clear boundaries rather than premature services.

## Dependency direction

```text
ContactCore.Domain
       ↑
ContactCore.Application
       ↑
ContactCore.Infrastructure
       ↑
ContactCore.Desktop
```

`ContactCore.Desktop` also references Domain/Application directly for presentation models and workflows. Domain never references UI, persistence, or platform code.

## Domain

`src/ContactCore.Domain` contains the contact aggregate, communication/address/organization/group/tag value records, validation rules, and text/phone normalization. Domain behavior must stay deterministic and testable without a database or UI framework.

## Application

`src/ContactCore.Application` owns user-oriented workflows and abstractions:

- `IContactRepository` for persistence;
- `IBackupService` for durable snapshots;
- `ContactService` for normalization, validation, save/search/favorite/archive/delete operations;
- `DuplicateService` for scoring and deterministic merge primitives;
- CSV and vCard codecs for portable data interchange.

Application code does not know that SQLite or Avalonia exists.

## Infrastructure

`src/ContactCore.Infrastructure` implements application interfaces with SQLite and the local filesystem.

`SqliteDatabase` owns connection configuration and schema migrations. `SqliteContactRepository` writes each aggregate in a transaction and uses parameterized SQL for all user data. `SqliteBackupService` uses SQLite's backup facility, verifies candidates with `PRAGMA integrity_check`, and stages restores before replacing the active database.

## Desktop

`src/ContactCore.Desktop` is the composition root. `App.axaml.cs` creates the database and services, while `MainWindowViewModel` exposes commands/properties to the Avalonia shell. The UI should not duplicate validation, duplicate-scoring, or storage rules.

## Persistence model

The SQLite schema stores the contact root in `contacts`, one-to-many values in `phones`, `emails`, `addresses`, and `organizations`, and normalized many-to-many relationships for `groups` and `tags`. Foreign keys cascade child deletion. Schema versions are tracked by `schema_migrations`.

Dates are stored in invariant ISO representations. GUIDs are stored in canonical text form. Boolean values use constrained integers.

## Concurrency and transactions

Each repository operation owns a short-lived connection. Aggregate upserts use one explicit transaction. SQLite busy timeout is configured for local contention. Higher-level long-running transactions are intentionally avoided.

## Error boundaries

Infrastructure exceptions remain available to engineering logs/tests, but desktop-facing errors are converted to safe status messages that do not include contact data. Validation failures use `ContactValidationException` with field-level issues suitable for UI display.

## Evolution rules

- Persistent schema changes require an ordered migration and migration test.
- New external systems require an application interface before an infrastructure adapter.
- New contact fields should be modeled in Domain first.
- Network access, cloud synchronization, authentication, or encrypted databases require an ADR and threat-model update before implementation.

See `docs/adr/` for individual architectural decisions.
