# ContactCore Roadmap

The roadmap is directional rather than a promise of dates. Security, data integrity, and regressions take priority over feature count.

## Phase 1 — Reliable local core

- [x] Layered solution and strict build settings
- [x] Contact aggregate and validation
- [x] Local SQLite schema and migrations
- [x] Create/edit/delete/search/favorite/archive workflows
- [x] CSV and vCard codecs
- [x] Duplicate detection and deterministic merge primitive
- [x] Backup and restore service
- [x] Initial Avalonia desktop workspace
- [x] Unit/integration tests and CI skeleton

## Phase 2 — Complete contact workflows

- [ ] Multi-value phone/email/address editor UI
- [ ] Organization, group, and tag management UI
- [ ] Birthday browsing and optional local reminders
- [ ] Duplicate-review and interactive merge screen
- [ ] User-selected CSV/vCard import/export paths via native file picker
- [ ] Backup restore UI with explicit confirmation and preview metadata
- [ ] Undo window for destructive contact deletion
- [ ] Settings page for appearance, accessibility, data, privacy, backup, and About

## Phase 3 — UX and accessibility hardening

- [ ] Responsive compact layout for narrow desktop windows
- [ ] Full keyboard-shortcut map and command palette
- [ ] Empty/loading/error/success states for every primary workflow
- [ ] Theme preference persistence for light/dark/system
- [ ] Screen-reader review on supported desktop platforms
- [ ] Reduced-motion behavior where animation is introduced
- [ ] Externalized UI strings and localization-ready resource catalog

## Phase 4 — Reliability and performance

- [ ] Batch materialization for large search results to remove remaining N+1 reads
- [ ] Query-plan benchmarks for 10k/100k fictional contacts
- [ ] Property-based/fuzz coverage for CSV/vCard parsers
- [ ] Migration tests from every released schema version
- [ ] Crash-safe import transaction and duplicate-resolution workflow
- [ ] Backup retention and verification utility

## Phase 5 — Release quality

- [ ] Real screenshots using fictional sample data
- [ ] Platform packaging/signing guidance
- [ ] Clean-checkout release-candidate audit on Windows, macOS, and Linux
- [ ] Documentation link checker and dependency/license audit
- [ ] First tagged public release

## Non-goals unless requirements change

- Mandatory cloud accounts
- Advertising or behavioral analytics
- Hidden telemetry
- Uploading contact data to a server by default

**Made by the Sanskar**
