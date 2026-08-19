# Architecture

ContactCore uses a modular-monolith layout:

- `ContactCore.Domain`: contact entities, value records, validation, normalization. No infrastructure dependencies.
- `ContactCore.Application`: repository/preferences/backup abstractions plus use cases, duplicate detection/merge, CSV and vCard codecs.
- `ContactCore.Infrastructure`: SQLite persistence, migrations, backups, settings, app paths, and diagnostic redaction.
- `ContactCore.Desktop`: Avalonia presentation and composition root.
- `tests/*`: deterministic unit and integration tests.

The dependency direction is Desktop → Infrastructure/Application → Domain, while Infrastructure implements abstractions owned by Application. SQLite child collections use transactionally replaced rows so a contact write is atomic. Schema changes are versioned in `DatabaseMigrator`.
