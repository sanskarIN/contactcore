# Privacy

ContactCore is designed as an offline-first desktop contact book.

## Data stored locally

ContactCore stores contact records, communication fields, addresses, organizations, groups, tags, favorites/archive state, and timestamps in a SQLite database under the operating system's local application-data folder. Backups and exports are created only when the user requests them.

The application does not require an account and the current implementation does not include analytics, advertising, telemetry, cloud synchronization, or background data upload.

## Network behavior

The application itself does not require network access for normal contact-management workflows. Development and distribution tooling may access package registries and GitHub when restoring dependencies, checking CI, or downloading releases; those activities are separate from the running ContactCore application.

## Exports and backups

CSV exports and SQLite backups may contain personal information. Store them in a trusted location, protect them using operating-system permissions or encrypted storage when appropriate, and do not attach real exports/databases to public GitHub issues.

## Logs and diagnostics

Production-facing error messages are intended to avoid exposing contact content. Contributors must not add logging of raw contact names, email addresses, phone numbers, notes, database contents, authentication data, or secrets.

## Deletion

Deleting a contact removes the record and its child data from the active SQLite database through foreign-key cascades. Existing external backups or exports are separate copies and must be deleted independently by the user.

## Contact

Privacy questions can be sent to `sanskarin@outlook.in` or `supportramsandesh@gmail.com`.

**Made by the Sanskar**
