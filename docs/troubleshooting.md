# Troubleshooting

## The requested .NET SDK cannot be found

From the repository root:

```bash
dotnet --list-sdks
dotnet --version
```

Compare the result with `global.json`. Install a compatible .NET 10 SDK. Do not delete `global.json` merely to make the build use an unrelated SDK.

## Restore fails

Try:

```bash
dotnet nuget locals all --clear
dotnet restore ContactCore.slnx
```

Then check network/proxy settings and the NuGet service status. Do not commit credentials or private feed tokens into NuGet configuration files.

## Build fails because warnings are errors

This is intentional. Read the full warning, fix the source/configuration cause, then rerun:

```bash
dotnet build ContactCore.slnx -c Release
```

Avoid global `NoWarn` entries unless an issue has been investigated, documented, and narrowly justified.

## Formatting check fails

Run:

```bash
dotnet format ContactCore.slnx
dotnet format ContactCore.slnx --verify-no-changes
```

Commit only the formatting changes related to your branch; avoid mixing unrelated repository-wide churn into feature commits.

## Application starts but the database cannot be opened

ContactCore creates its database under the current user's local application-data directory. Common causes include:

- insufficient filesystem permission;
- read-only/full storage;
- another process holding an unusual exclusive lock;
- endpoint-security software blocking the location;
- a damaged database.

Do not delete the database first. Copy it to a safe location, preserve any existing backups, and diagnose with fictional/test data where possible.

## `database is locked`

ContactCore uses short-lived connections and a SQLite busy timeout. If locking persists:

1. Ensure only one experimental/debug tool is writing the database.
2. Close external SQLite browsers/editors.
3. Restart ContactCore.
4. Preserve a backup before attempting repair.

Do not disable transactions or foreign keys to work around locking.

## Backup creation fails

Check that the destination folder exists or can be created, has free space, and is writable. ContactCore's backup service verifies the generated database with `PRAGMA integrity_check`; an integrity failure should be investigated rather than bypassed.

## Restore fails

Restore rejects a missing, corrupt, unsupported, or same-as-active source. Keep the active database and backup file unchanged until the failure is understood. Never overwrite the only known-good backup during troubleshooting.

## Search misses a record

Current search covers given/family name, nickname, email, phone, organization name/title, group, and tag. Archived records are hidden unless the Archived filter is enabled. Favorites-only filtering can also narrow results.

## UI looks wrong under a theme

The application requests the system theme through Avalonia's default theme variant. Capture a screenshot using fictional data, record OS/theme/scaling settings, and open a UI bug if text becomes unreadable, focus is invisible, or controls clip at supported window sizes.

## CI differs from local behavior

Compare:

- exact commit SHA;
- output of `dotnet --info`;
- operating system;
- restored package graph;
- Release vs Debug configuration.

Run the same commands as `.github/workflows/ci.yml` before assuming CI is defective.

## Requesting support

Follow `SUPPORT.md`. Never attach a real contact database/export or unsanitized logs to a public issue.
