# Changelog

Significant ContactCore changes are recorded here. Version numbers follow semantic-versioning conventions.

## Unreleased

### Added

- Layered .NET 10 solution with Domain, Application, Infrastructure, and Avalonia Desktop projects.
- Contact aggregate with phones, emails, postal addresses, organizations, groups, tags, birthdays, favorites, archive state, notes, and timestamps.
- Validation and Unicode-aware search normalization.
- Contact workflows, duplicate scoring/merge behavior, CSV import/export, and vCard 4.0 import/export.
- Transactional SQLite persistence with schema versioning, foreign keys, indexed search, and aggregate round-tripping.
- Integrity-checked SQLite backup and restore.
- Avalonia desktop workspace with search, filters, editing, favorites/archive actions, CSV export, backup, and About/credit content.
- Domain, application, and SQLite integration test projects.
- CI, CodeQL, Dependabot, release publishing, issue templates, pull-request template, and funding metadata.
- Security, privacy, support, contribution, architecture, setup, testing, release, troubleshooting, accessibility, and performance documentation.

### Fixed

- CSV escaping uses a supported character-search API.
- Desktop save/delete workflows refresh the displayed collection after persistence changes.

## 0.0.0 — 2026-08-19

- Repository initialized.
