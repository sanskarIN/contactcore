# ADR 0001: Layered modular monolith

- Status: Accepted
- Date: 2026-08-19

## Context

ContactCore needs clear separation between contact rules, workflows, local persistence, and desktop UI without the operational complexity of services or distributed infrastructure.

## Decision

Use one solution with four production projects:

1. `ContactCore.Domain` — entities/value records, validation, pure normalization.
2. `ContactCore.Application` — workflows and infrastructure interfaces.
3. `ContactCore.Infrastructure` — SQLite/filesystem adapters.
4. `ContactCore.Desktop` — Avalonia UI and composition root.

Dependencies point inward toward Domain/Application. Infrastructure details do not leak into Domain.

## Consequences

- Domain/application tests run without Avalonia or a production database.
- SQLite can be replaced or supplemented through application interfaces.
- UI code remains focused on presentation state.
- Cross-cutting changes may still touch several projects, but the boundary makes those responsibilities explicit.
- Microservices are rejected unless future requirements demonstrate a real independent scaling/deployment boundary.
