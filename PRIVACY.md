# Privacy

ContactCore is designed for local-first contact management. The application has no mandatory cloud account, telemetry endpoint, advertising SDK, or analytics dependency. Contact records are stored in the user's local application-data directory. Data leaves the app only through an explicit export/backup action or through operating-system behavior the user controls.

Contact data can include names, phone numbers, email addresses, postal addresses, notes, birthdays, organizations, groups, and tags. Treat backups and exports as sensitive. Do not attach real databases to public issues.

Optional database encryption is fail-closed: setting a key without a SQLCipher-compatible native SQLite provider causes startup to fail instead of continuing with plaintext storage. See `docs/security.md`.
