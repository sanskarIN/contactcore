# Release Smoke-Test Record

Use this document as the repeatable manual verification record for a ContactCore release candidate. Copy it for an actual release run, fill it with the exact tested commit/artifact/device details, and keep all sample contact data fictional.

A completed record is evidence of what was actually exercised; it is not permission to claim platforms, signing, accessibility, or store certification that were not tested.

## 1. Release identity

- ContactCore version: `2.0.12`
- Candidate tag: `v2.0.12`
- Git commit SHA: ______________________________
- Pull request / release URL: ______________________________
- Test date (UTC): ______________________________
- Tester / reviewer: ______________________________
- CI run URL: ______________________________
- CodeQL run URL: ______________________________
- Result: `PASS / PASS WITH NOTES / FAIL`

### Exact-head automated gate

Record the status for the same candidate SHA:

| Gate | Result | Evidence / notes |
|---|---|---|
| Ubuntu core restore/format/build/tests |  |  |
| Windows core restore/format/build/tests |  |  |
| macOS core restore/format/build/tests |  |  |
| Browser/WebAssembly Release build |  |  |
| Android `android-arm64` Release build |  |  |
| iOS `iossimulator-arm64` Release build |  |  |
| CodeQL |  |  |

Do not substitute a successful run from an older SHA.

## 2. Privacy-safe test fixture

Create a disposable profile/database/origin containing fictional contacts only. Include enough variation to exercise the rich data model without using copied real-world contact data.

Suggested fictional fixture:

- Ada Example — multiple phones/emails, one address, one organization, one group, one tag.
- Grace Sample — favorite, birthday, notes, archived-state transition.
- Alex Duplicate — a pair whose local phone numbers differ only by a country-code prefix.
- Delimiter Demo — group/tag names containing commas and semicolons.
- Unicode Example — accented/non-ASCII name and notes.

Record fixture location/profile name without exposing a private user path: ______________________________

Confirm before testing:

- [ ] No real contacts were imported.
- [ ] No real database, backup, export, screenshot, signing key, certificate, or secret is used.
- [ ] The disposable profile can be deleted after verification.

## 3. Release-artifact verification

For a tag-produced release, confirm the expected assets exist:

- [ ] `contactcore-v2.0.12-win-x64.zip`
- [ ] `contactcore-v2.0.12-win-arm64.zip`
- [ ] `contactcore-v2.0.12-linux-x64.tar.gz`
- [ ] `contactcore-v2.0.12-linux-arm64.tar.gz`
- [ ] `contactcore-v2.0.12-osx-x64.tar.gz`
- [ ] `contactcore-v2.0.12-osx-arm64.tar.gz`
- [ ] `contactcore-v2.0.12-browser-wasm.zip`
- [ ] `SHA256SUMS.txt`

Checksum verification command/result: ______________________________

Remember: checksum verification checks byte integrity against the published manifest. It does not mean an artifact is code-signed, notarized, or store-certified.

## 4. Desktop smoke matrix

Complete the applicable rows for each representative desktop environment tested.

| Check | Windows | Linux | macOS | Notes |
|---|---|---|---|---|
| App launches from packaged output |  |  |  |  |
| First-run local database initializes |  |  |  |  |
| Existing disposable profile reopens |  |  |  |  |
| Create rich contact |  |  |  |  |
| Edit/save rich repeated fields |  |  |  |  |
| Add/remove phone/email/address/org rows |  |  |  |  |
| Group/tag delimiter names round-trip |  |  |  |  |
| Favorite/archive filters |  |  |  |  |
| A–Z navigation |  |  |  |  |
| Search updates without stale result overwrite |  |  |  |  |
| Duplicate pair evidence/preview |  |  |  |  |
| Merge keeping first record |  |  |  |  |
| Merge keeping second record |  |  |  |  |
| Merge cancellation preserves records |  |  |  |  |
| CSV import/export |  |  |  |  |
| vCard import/export |  |  |  |  |
| Verified database backup |  |  |  |  |
| Restore confirmation |  |  |  |  |
| Verified restore from disposable backup |  |  |  |  |
| Unsaved contact discard |  |  |  |  |
| Permanent-delete confirmation/cancellation |  |  |  |  |
| System/Light/Dark theme |  |  |  |  |
| Reduced-motion preference persistence |  |  |  |  |
| Keyboard shortcuts |  |  |  |  |
| Visible focus indication |  |  |  |  |

Desktop environments tested:

- Windows version / architecture: ______________________________
- Linux distribution / architecture / desktop: ______________________________
- macOS version / architecture: ______________________________

## 5. Browser/WebAssembly smoke matrix

Serve the published browser artifact from an HTTP(S) origin using a disposable browser profile. Do not validate persistence using a normal profile containing personal browser data.

| Check | Result | Notes |
|---|---|---|
| Application boots from static publish output |  |  |
| Empty first-run IndexedDB initializes |  |  |
| Rich contact create/edit/save |  |  |
| Reload preserves contact aggregate |  |  |
| Favorite/archive/A–Z filters |  |  |
| Search/debounce behavior |  |  |
| Duplicate review and both survivor directions |  |  |
| CSV import/export picker path |  |  |
| vCard import/export picker path |  |  |
| Theme/preferences survive reload where persistent storage is allowed |  |  |
| Native SQLite backup/restore controls are unavailable as documented |  |  |
| Native database encryption is not falsely claimed |  |  |
| Blocked/unavailable browser storage fails usefully |  |  |
| Clearing disposable site data removes local browser data as expected |  |  |

Browser/profile/origin tested:

- Browser + version: ______________________________
- OS: ______________________________
- Origin: ______________________________
- Private/incognito mode used? ______________________________

If more than one engine is tested, add one completed copy of this section per engine/profile.

## 6. Android smoke matrix

The public repository's automated gate proves source/build compatibility, not production signing or Play Store certification. Complete this section only for devices/emulators actually tested.

| Check | Result | Notes |
|---|---|---|
| Startup |  |  |
| Local SQLite persistence after restart |  |  |
| Rich contact editing on touch |  |  |
| Repeated-field scrolling/layout |  |  |
| Search/filter/duplicates |  |  |
| Import/export file picker |  |  |
| Destructive confirmation usability |  |  |
| Portrait/landscape behavior |  |  |
| Background/resume lifecycle |  |  |
| Software keyboard/input |  |  |
| Theme/contrast/focus/labels accessibility smoke |  |  |

Android device/emulator details: ______________________________

Signing state: `debug / test / production / not evaluated`

Do not mark Play Store readiness unless an actual secure store-signing and publishing review has been completed separately.

## 7. iPhone/iPad smoke matrix

The public repository's automated gate uses `iossimulator-arm64` and an explicitly compatible Xcode toolchain. Complete this section only for simulators/devices actually tested.

| Check | Result | Notes |
|---|---|---|
| Startup |  |  |
| Local SQLite persistence after restart |  |  |
| Rich contact editing on touch |  |  |
| Repeated-field scrolling/layout |  |  |
| Search/filter/duplicates |  |  |
| Import/export file picker |  |  |
| Destructive confirmation usability |  |  |
| iPhone portrait/landscape behavior where supported |  |  |
| iPad layout/orientation behavior |  |  |
| Background/resume lifecycle |  |  |
| Software/hardware keyboard input where applicable |  |  |
| Theme/contrast/focus/labels accessibility smoke |  |  |

Apple test environment:

- macOS: ______________________________
- Xcode: ______________________________
- Simulator/device: ______________________________
- iOS/iPadOS: ______________________________

Signing/provisioning state: `simulator only / development / distribution / not evaluated`

Do not mark App Store readiness unless real signing/provisioning and store validation have been completed separately.

## 8. Data-safety scenarios

Use only disposable data.

- [ ] Invalid contact input is rejected without partial persistence.
- [ ] Malformed/unsupported import input produces controlled warnings/errors.
- [ ] Import validation occurs before the batch is persisted.
- [ ] Duplicate merge does not proceed before explicit confirmation.
- [ ] Cancelling permanent delete preserves the contact.
- [ ] Cancelling restore preserves the active database.
- [ ] Native backup restore is performed only with a verified ContactCore backup.
- [ ] A failed operation does not expose a private filesystem path in user-visible diagnostics.
- [ ] Runtime database keys/signing credentials are absent from exported logs/screenshots/artifacts.

Observed recovery artifact names/locations, redacted to non-private form: ______________________________

## 9. Accessibility and interaction record

This section is a smoke record, not a formal accessibility certification.

- [ ] Keyboard traversal checked on a keyboard-capable desktop target.
- [ ] Visible focus checked.
- [ ] Light and Dark themes checked for obvious unreadable text/controls.
- [ ] Reduced-motion setting checked where visible motion applies.
- [ ] Screen-reader labels/reading order sampled where a supported reader/device is available.
- [ ] High-DPI/scaling sampled on desktop where available.
- [ ] Touch targets/scrolling sampled on mobile where available.
- [ ] Small-window/narrow-layout behavior sampled.

Assistive technology / scaling details: ______________________________

## 10. Privacy review before screenshots or public evidence

For every screenshot, screen recording, uploaded log, or attached test artifact:

- [ ] Contacts are obviously fictional.
- [ ] No real email address, phone number, street address, or note is visible.
- [ ] No username/private filesystem path is visible.
- [ ] No notification or unrelated application content is visible.
- [ ] No database key, token, certificate, signing identity, or provisioning information is visible.
- [ ] Image/file metadata has been reviewed where applicable.

Public evidence links: ______________________________

## 11. Deviations and known failures

Record every skipped test, failure, workaround, or environment-specific limitation. Do not silently convert `NOT TESTED` into `PASS`.

| Platform / area | Status | Issue / limitation | Follow-up |
|---|---|---|---|
|  |  |  |  |
|  |  |  |  |

## 12. Release decision

- Exact candidate SHA: ______________________________
- Automated gates green for this SHA? `YES / NO`
- Required manual smoke tests complete? `YES / NO / PARTIAL`
- Known blockers: ______________________________
- Non-blocking limitations accepted for this release: ______________________________
- Release decision: `APPROVE / HOLD / REJECT`
- Reviewer/sign-off: ______________________________
- Decision date (UTC): ______________________________

If the decision is `HOLD` or `REJECT`, fix the issue and create a new record against the new exact candidate SHA. Never reuse a prior completed record as evidence for changed code.
