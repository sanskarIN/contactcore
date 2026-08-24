# ContactCore Release Smoke-Test Record

Use this document as a **template** for each release candidate. Copy it to an external/internal release record or a versioned evidence location as appropriate, then fill it using only fictional/disposable data.

A completed record is valid only for the exact SHA/artifacts/environment it names. Do not reuse an older record after the candidate changes.

## Candidate identity

- Version: `2.0.12`
- Intended tag: `v2.0.12`
- Candidate commit SHA: `______________________________`
- PR / merge commit: `______________________________`
- Test date/time: `______________________________`
- Tester/maintainer: `______________________________`
- Decision: `APPROVE / HOLD / REJECT`

## Safety/privacy fixture rule

- [ ] All contacts used are obviously fictional.
- [ ] No real contact database/backup/export was used.
- [ ] Screenshots contain no real names/emails/phone numbers/paths/notifications.
- [ ] Logs/evidence contain no credentials, keys, signing material, private endpoints, or personal data.
- [ ] Browser testing uses a disposable profile/origin where destructive/storage tests are performed.
- [ ] Native destructive/restore tests use a disposable application profile/database.

## Exact-head automated gate evidence

Record the workflow URLs/run IDs for the **same final synthetic PR merge candidate or merged commit**.

| Gate | Run/job evidence | Result | Notes |
|---|---|---|---|
| Ubuntu core restore/format/build/tests |  | PASS / FAIL |  |
| Windows core restore/format/build/tests |  | PASS / FAIL |  |
| macOS core restore/format/build/tests |  | PASS / FAIL |  |
| Browser/WebAssembly Release build |  | PASS / FAIL |  |
| Android `android-arm64` Release build |  | PASS / FAIL |  |
| iOS `iossimulator-arm64` Release build |  | PASS / FAIL | Xcode 26.0 + simulator-only trim policy |
| CodeQL |  | PASS / FAIL |  |

- [ ] No row above is copied from an older/superseded PR head.
- [ ] The iOS simulator result is recorded only as source/runtime integration evidence, not production device/App Store signing/trimming certification.
- [ ] Browser Release build kept application-owned AOT/trimming diagnostics active.

## Artifact/checksum evidence

Expected automated downloadable artifacts:

```text
contactcore-v2.0.12-win-x64.zip
contactcore-v2.0.12-win-arm64.zip
contactcore-v2.0.12-linux-x64.tar.gz
contactcore-v2.0.12-linux-arm64.tar.gz
contactcore-v2.0.12-osx-x64.tar.gz
contactcore-v2.0.12-osx-arm64.tar.gz
contactcore-v2.0.12-browser-wasm.zip
SHA256SUMS.txt
```

| Artifact | Present | Checksum verified | Notes |
|---|---|---|---|
| Windows x64 | YES / NO | YES / NO |  |
| Windows ARM64 | YES / NO | YES / NO |  |
| Linux x64 | YES / NO | YES / NO |  |
| Linux ARM64 | YES / NO | YES / NO |  |
| macOS Intel | YES / NO | YES / NO |  |
| macOS Apple Silicon | YES / NO | YES / NO |  |
| Browser WebAssembly | YES / NO | YES / NO |  |
| SHA256SUMS | YES / NO | N/A |  |

Do not mark Android/iOS store packages present unless a separate real signed distribution pipeline actually produced them.

## Desktop smoke matrix

For every platform actually exercised, record exact OS/architecture/package.

| Platform/environment | Startup | Rich edit/save | Search/filter | Duplicate merge | Import/export | Backup/restore | Theme/focus | Result/notes |
|---|---|---|---|---|---|---|---|---|
| Windows |  |  |  |  |  |  |  |  |
| Linux |  |  |  |  |  |  |  |  |
| macOS |  |  |  |  |  |  |  |  |

Suggested fictional workflow:

1. Create several contacts with repeated phones/emails/addresses/organizations/groups/tags.
2. Edit/remove/add repeated fields and verify retained fields/IDs behavior indirectly through UI persistence.
3. Exercise Favorites/Archived/A–Z/free-text search.
4. Create a deliberate duplicate with a legitimate international country-code variant and verify evidence/merge.
5. Also create a different short/nine-digit suffix case and confirm it is **not** treated as equivalent solely by suffix.
6. Exercise both duplicate survivor directions and cancel confirmation once.
7. Export CSV/vCard, then import into a disposable profile.
8. Create native backup and restore it into the disposable profile.
9. Exercise delete confirmation/discard behavior.
10. Exercise theme/reduced-motion/keyboard focus behavior.

## Browser/WebAssembly smoke matrix

Environment:

- Browser engine/version: `______________________________`
- OS: `______________________________`
- Deployment origin: `______________________________`
- Disposable profile confirmed: `YES / NO`

- [ ] WebAssembly application boots.
- [ ] Contact create/edit/save works.
- [ ] Reload preserves IndexedDB contact state.
- [ ] Search/filter/favorite/archive works.
- [ ] Duplicate review/merge works.
- [ ] CSV/vCard import/export works.
- [ ] Theme/preferences persist as documented.
- [ ] UI compiled-binding paths behave correctly in the actual browser.
- [ ] Native SQLite backup/restore is not presented as available.
- [ ] Native database encryption is not presented as available.
- [ ] Blocked/denied storage produces useful controlled behavior.
- [ ] Clearing site data removes browser-managed state as documented.
- [ ] Cross-tab behavior is not overclaimed beyond current support.

Result/notes:

`________________________________________________________________________`

## Android smoke matrix

Environment:

- Device/emulator: `______________________________`
- Android version: `______________________________`
- Architecture/package: `______________________________`
- Signed distribution artifact? `YES / NO` (only if real)

- [ ] Application starts.
- [ ] Rich editor fits/scrolls/accepts touch input.
- [ ] Software/hardware keyboard input works as applicable.
- [ ] SQLite persistence survives restart.
- [ ] Search/filter/duplicate workflows work.
- [ ] Import/export/file-picker behavior works where supported.
- [ ] Orientation/configuration changes preserve safe state.
- [ ] Background/resume behavior is acceptable.
- [ ] Theme/reduced-motion/accessibility behavior checked.
- [ ] No signing/store claim is made unless actually verified.

Result/notes:

`________________________________________________________________________`

## iPhone/iPad smoke matrix

Environment:

- Simulator/device: `______________________________`
- iOS/iPadOS version: `______________________________`
- Xcode: `______________________________`
- Architecture: `______________________________`
- Signed/provisioned physical-device build? `YES / NO`

- [ ] Application starts.
- [ ] Rich editor fits/scrolls/accepts touch input.
- [ ] SQLite persistence survives restart.
- [ ] Search/filter/duplicate workflows work.
- [ ] Import/export/file-picker behavior works where supported.
- [ ] Orientation/lifecycle/background/resume behavior checked.
- [ ] Theme/reduced-motion/VoiceOver behavior checked where available.
- [ ] Simulator-only CI trim policy is not presented as physical-device distribution verification.
- [ ] If a real signed device build exists, its actual production trim/link/sign/provision/install behavior is separately recorded.
- [ ] No App Store certification claim is made unless actually verified.

Result/notes:

`________________________________________________________________________`

## Data-safety scenarios

- [ ] Unsaved new-contact **Delete / discard** does not delete an unrelated persisted contact.
- [ ] Permanent delete confirmation cancellation preserves the target.
- [ ] Duplicate merge confirmation cancellation preserves both records.
- [ ] Stale duplicate review state does not resurrect a removed survivor or silently merge when a reviewed record is missing.
- [ ] Native restore rejects unrelated/corrupt/future-schema input before destructive switch.
- [ ] Native restore keeps/creates the documented recovery snapshot.
- [ ] If forced post-switch verification failure is exercised, rollback behavior matches tests/docs.
- [ ] Browser failed persistence does not leave the in-memory repository pretending an uncommitted mutation succeeded.

## Accessibility/interaction checks

Record only checks actually executed.

- [ ] Keyboard navigation/focus visibility (desktop/browser as applicable).
- [ ] `Ctrl+N` / editor-only `Ctrl+S` / `Ctrl+F` / `Esc` behavior where supported.
- [ ] High-DPI/scaling checked.
- [ ] Light/Dark/System theme checked.
- [ ] Reduced-motion preference checked.
- [ ] Screen-reader/VoiceOver/TalkBack check executed on: `______________________________`.
- [ ] Touch targets/layout checked on representative phone/tablet where applicable.

## Privacy review for evidence

- [ ] Screenshot notification areas reviewed.
- [ ] Usernames/home-directory paths reviewed.
- [ ] Contact fixture values are fictional.
- [ ] No API tokens/keys/passwords/certificates/provisioning profiles/keystores shown.
- [ ] CI logs attached to evidence do not contain private values.
- [ ] Browser origin/profile details do not disclose a private environment unnecessarily.

## Deviations / intentionally untested items

Every skipped item must be explicit; do not turn an empty checkbox into implied success.

| Item | Why not tested | Release impact / follow-up |
|---|---|---|
|  |  |  |

## Known limitations acknowledged

- [ ] Repeated-field drag/drop reordering is not claimed.
- [ ] Global group/tag taxonomy management is not claimed.
- [ ] General undo is not claimed.
- [ ] Full vCard fidelity is not claimed.
- [ ] CSV full-fidelity backup/spreadsheet neutralization is not claimed.
- [ ] Browser native SQLite backup/encryption is not claimed.
- [ ] Browser robust multi-tab conflict handling is not claimed.
- [ ] Ordinary native SQLite encrypted-at-rest support is not claimed without a verified compatible cipher provider.
- [ ] Signed/notarized/store-certified desktop/mobile packages are not claimed unless actually produced and verified.
- [ ] iOS simulator source-build success is not claimed as production signed-device trim/link verification.

## Final decision

Decision: `APPROVE / HOLD / REJECT`

Rationale:

`________________________________________________________________________`

Blocking follow-up items:

`________________________________________________________________________`

Approver/date:

`________________________________________________________________________`
