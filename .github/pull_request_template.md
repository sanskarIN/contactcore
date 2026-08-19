## Summary

Describe the problem, the user-visible/developer-visible change, and why this is the appropriate solution.

## Architecture and data impact

- Layer(s) changed:
- Contact/domain-model impact:
- SQLite schema/migration impact:
- Import/export compatibility impact:
- Backup/restore/recovery impact:
- Backward/rollback considerations:

If none, say why.

## Privacy and security

Describe changes to local files, secrets, encryption/provider behavior, diagnostics, permissions, networking, external services, or destructive actions. Confirm that examples/tests/screenshots use fictional data.

## Accessibility and UX

For UI changes, describe keyboard/focus/text-scaling/theme/reduced-motion/screen-reader considerations and any manual checks performed. For non-UI changes, state not applicable.

## Verification

- [ ] `dotnet restore ContactCore.slnx`
- [ ] `dotnet format ContactCore.slnx --verify-no-changes --no-restore`
- [ ] `dotnet build ContactCore.slnx -c Release --no-restore`
- [ ] `dotnet test ContactCore.slnx -c Release --no-build --collect:"XPlat Code Coverage"`
- [ ] Focused regression tests were added/updated for changed behavior
- [ ] Final PR-head CI passes on Windows, Ubuntu, and macOS, or failures are explicitly explained
- [ ] Final PR-head CodeQL result was reviewed

If a local command could not be run, explain exactly why rather than checking it as passed.

## Data-safety checklist

- [ ] No real contact data, database, WAL/SHM file, backup/recovery file, or real CSV/vCard export was added
- [ ] No `.env`, database key, token, password, signing private key/certificate, or other secret was added
- [ ] User-controlled SQL/data values remain parameterized and intended `LIKE` text escaping is preserved where relevant
- [ ] Aggregate, batch-import, and destructive multi-row writes remain transactional where relevant
- [ ] The full contact editor preserves root identity, creation time, and existing repeated child identities unless a row is intentionally removed/recreated
- [ ] Newly added/changed contact fields are represented by the editor or explicitly preserved so complete-aggregate saves cannot silently drop them
- [ ] Unsaved drafts remain distinguishable from persisted contacts for destructive actions
- [ ] Duplicate/heuristic workflows remain user-controlled; destructive merge requires survivor choice/confirmation and rejects stale reviewed records
- [ ] Schema changes use a new migration and preserve future-schema/backup identity safety where relevant
- [ ] Restore/data-replacement changes preserve verified staging/recovery/rollback guarantees where relevant
- [ ] Destructive operations do not bypass configured confirmation safeguards

## Documentation

- [ ] `README.md` updated when public capabilities/limitations changed
- [ ] `CHANGELOG.md` updated for notable behavior
- [ ] Relevant `docs/*.md` guides updated
- [ ] `ROADMAP.md` reconciled when work was completed/re-scoped
- [ ] `docs/repository-reference.md` updated for added/removed/renamed files or changed responsibilities
- [ ] ADR added/updated for a durable architecture/storage/security decision when needed
- [ ] `what_changed.md` reflects the continuation/handoff state when applicable

## Screenshots

If UI changed, add screenshots only when they materially help review. Use a disposable profile with clearly fictional contact data and review the full image for private paths, usernames, notifications, or other accidental personal information.

## Known limitations / follow-up

List anything intentionally not completed in this PR so it cannot be mistaken for a finished capability.
