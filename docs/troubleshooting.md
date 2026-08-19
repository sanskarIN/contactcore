# Troubleshooting

## SDK not found
Install .NET 10 and confirm `dotnet --info` lists a compatible 10.0 SDK.

## App fails after setting CONTACTCORE_DATABASE_KEY
This is intentional when the process is using ordinary SQLite. Remove the variable to use the unencrypted local database, or install/configure a supported SQLCipher-compatible native provider as described in `docs/security.md`.

## Database opens slowly or reports locked
Close other processes using the same database. ContactCore enables a SQLite busy timeout and WAL-compatible connection handling, but cannot safely override another process holding an exclusive lock.

## Restore rejected
The restore path runs SQLite `integrity_check` first. Create a new valid backup or inspect the damaged file with SQLite recovery tooling on a copy, never the only copy.
