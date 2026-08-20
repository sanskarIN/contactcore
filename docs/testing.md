# Testing

The automated suite covers:

- domain validation and Unicode-aware normalization;
- duplicate detection and deterministic merge behavior;
- CSV/vCard round trips;
- formula-like CSV values and the explicit spreadsheet-safe export policy;
- vCard line unfolding, escaped text, and missing terminators;
- deterministic randomized CSV/vCard parser inputs;
- SQLite aggregate persistence and cascade deletion;
- backup creation/restore round trips;
- invalid backup rejection without replacing live data;
- rollback when a restored database fails post-restore migration.

Run all tests with:

```bash
dotnet test ContactCore.slnx -c Release
```

The pull-request CI workflow runs restore, formatting verification, release build, and tests on Windows, Linux, and macOS. It collects XPlat coverage and TRX results and uploads per-platform test evidence even when a later step fails. No misleading fixed coverage percentage is claimed in the README.

The authoritative release-hardening gate also includes CodeQL analysis. A release must not be described as verified merely because tests passed on one operating system.
