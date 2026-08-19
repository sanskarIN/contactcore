# ADR 0002: SQLite persistence

Status: Accepted

SQLite provides transactional, portable, offline persistence without a server. Contact aggregates are normalized into related tables, schema changes are tracked by explicit migrations, foreign keys are enabled per connection, and child updates occur in the same transaction as the parent.
