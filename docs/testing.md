# Testing

ContactCore uses MSTest for deterministic automated tests. The solution separates tests by architecture layer so failures point to the smallest useful boundary.

## Run everything

```bash
dotnet test ContactCore.slnx -c Release
```

## Run one project

```bash
dotnet test tests/ContactCore.Domain.Tests/ContactCore.Domain.Tests.csproj
dotnet test tests/ContactCore.Application.Tests/ContactCore.Application.Tests.csproj
dotnet test tests/ContactCore.Infrastructure.Tests/ContactCore.Infrastructure.Tests.csproj
```

## Current coverage areas

### Domain

- Display-name behavior
- Email/phone validation
- Unicode/diacritic search normalization
- Phone normalization
- Aggregate deep-copy isolation

### Application

- Save normalization and validation failures
- Duplicate scoring and merge behavior
- CSV quoting/round-trip behavior
- vCard core-field round trips

### Infrastructure

- Idempotent schema initialization
- Full contact aggregate SQLite round trip
- Search by child fields and archive filtering
- Backup creation, integrity checking, and restore

## Adding regression tests

Every fixed bug should receive a test at the lowest layer that can reproduce it. Parser defects belong in codec tests, transaction defects in infrastructure tests, and display-command defects in desktop/view-model tests when a suitable UI test harness is added.

## Test data rules

Use invented names and reserved/non-deliverable addresses such as `example.test`. Do not use a real user's exported contacts, personal phone numbers, private addresses, production databases, credentials, or tokens.

Tests must be deterministic and must not require internet access or production credentials.

## Coverage collection

CI runs:

```bash
dotnet test ContactCore.slnx -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

The raw coverage output is uploaded as a short-lived workflow artifact. A hard numeric coverage threshold is intentionally not enforced yet; adding one should follow measured baseline coverage so it does not incentivize low-value tests.

## Manual desktop checks before release

On Windows, macOS, and Linux where available:

1. Launch from a clean local data directory.
2. Create a contact with email/phone/notes.
3. Restart and verify persistence.
4. Search by name, email, and phone.
5. Favorite, archive, restore, edit, and delete fictional contacts.
6. Export CSV and create a backup.
7. Verify light/dark/system theme rendering.
8. Navigate all primary controls with keyboard only.
9. Resize to the documented minimum window size.
10. Confirm errors do not display raw database/contact contents.

## Future test work

- Avalonia UI/view-model tests
- Parser fuzz/property tests
- Large-dataset performance fixtures
- Upgrade tests for every released migration
- Release-artifact smoke tests
- Accessibility automation where supported
