# Accessibility

ContactCore aims for an accessible desktop experience, but the repository does **not** claim formal WCAG, Section 508, EN 301 549, or platform accessibility certification. Automated code review cannot substitute for manual testing with representative assistive technologies on every supported operating system.

This document separates implemented design choices from validation that still must be performed.

## Current keyboard behavior

The main window uses standard Avalonia text boxes, buttons, toggle buttons, check boxes, combo boxes, lists, and scrolling surfaces.

Explicit shortcuts are:

- `Ctrl+N` — create a new contact draft;
- `Ctrl+S` — save only while the contact editor is visible;
- `Ctrl+F` — focus search;
- `Esc` — close/cancel the active contact editor, Settings, Data Tools, or Duplicate Review surface.

The editor-only `Ctrl+S` restriction is intentional: a global shortcut must not save stale contact state while another surface is active.

## Visible focus

`Styles/DesignSystem.axaml` supplies an explicit accent focus border for the major interactive Avalonia controls, including buttons, text boxes, combo boxes, check boxes, toggle buttons, and list boxes.

Manual testing is still required because a style definition does not prove that focus is visible in every platform/theme/state combination.

## Full editor labels and repeated controls

The editor visually labels:

- given name;
- family name;
- nickname;
- birthday;
- Favorite/Archived state;
- phone rows;
- email rows;
- address rows;
- organization rows;
- group rows;
- tag rows;
- notes.

Repeated collections have explicit **Add** and **Remove** buttons. Phone/email rows also expose a field-kind combo box.

Visible labels do not automatically prove correct platform accessibility-name/relationship metadata. Screen-reader testing must confirm that a user can identify each repeated row and distinguish its Remove button from other rows' Remove buttons.

## Duplicate-review accessibility

Duplicate Review adds a high-impact workflow that must be understandable without visual inference. The current UI presents:

- candidate list;
- confidence score;
- matching evidence;
- first-record summary;
- second-record summary;
- merge-behavior explanation;
- **Keep first record…** action;
- **Keep second record…** action;
- confirmation dialog before persistence.

Manual accessibility testing must verify that both survivor choices are announced distinctly and that the confirmation message clearly identifies which contact remains versus which contact is removed. Do not rely on column position/color alone.

## Text status instead of color-only status

Workflow success/failure uses `StatusMessage`, `DuplicateMessage`, and `FooterText`. Favorite/archive also have textual/interactive context in addition to compact list markers.

Dynamic status text may still require an explicit automation/live-region strategy for reliable screen-reader announcement. This remains a manual-validation/future-enhancement area.

## Theme support

Theme choices are System, Light, and Dark. The design system uses dynamic Avalonia theme resources for major surfaces/borders/accents rather than hard-coding a light-only palette.

Dynamic resources do not prove a particular contrast ratio. Audit all supported themes, focus states, disabled states, and high-contrast/OS contrast configurations where Avalonia exposes them.

## Reduced motion

Settings persist a `ReducedMotion` Boolean. The current custom UI has little bespoke animation, so this setting mainly establishes a contract for future motion. New custom animation should consult it.

Do not claim that all framework or operating-system animation is disabled merely because the preference exists.

## Text wrapping and scrolling

Long status/privacy/help text uses wrapping. Contact detail/settings/data/duplicate content is hosted in scrollable surfaces. Contact-list names/subtitles are truncated with ellipsis where needed.

The full rich editor can become substantially taller than the window. Keyboard and assistive-technology testing must verify that newly added rows remain reachable and that focus movement causes the relevant content to scroll into view.

## Minimum window size

The current `MainWindow.axaml` sets **900×600** as the minimum. Test this minimum plus high text/display scaling on each supported platform. The three-column layout may be dense; do not respond to clipping by simply shrinking fonts.

## Specific risks

### Repeated Remove buttons

Many controls share the visible label `Remove`. Screen-reader output may be ambiguous without row context. Manual testing should determine whether additional automation names/help text are needed, such as identifying the associated phone/email/address/group/tag.

### Contact list semantics

Verify that selected contact, name, subtitle, favorite/archive state, and list count are understandable and that decorative avatar initials/star markers are not noisy or misleading.

### Confirmation dialog

Verify initial focus, modal announcement, message reading order, Cancel/Confirm order, Escape/close behavior, and that destructive confirmation is not auto-focused in a risky manner.

### Dynamic search results

Search is debounced and can update repeatedly. Verify result-count/status announcement is useful without becoming disruptive.

### Validation feedback

Validation currently appears as status text. Verify screen-reader discoverability after failed save/import; consider an appropriate Avalonia automation notification/live-region mechanism if needed.

### Duplicate comparison

Side-by-side layout visually communicates first/second records. Screen readers must receive a sensible reading order and clear group labels. High scaling should not make one record inaccessible.

### Color contrast and icons

Favorite `★`, borders, accents, muted text, and status styling require contrast testing. State must not depend on color/icon alone.

## Keyboard-only smoke scenario

At minimum, using no mouse:

1. focus search with `Ctrl+F`;
2. search and navigate results;
3. create a draft with `Ctrl+N`;
4. traverse scalar fields;
5. add/edit/remove multiple phone/email/address/organization/group/tag rows;
6. toggle Favorite/Archived;
7. save with `Ctrl+S`;
8. open/close Settings and Data Tools;
9. open Duplicate Review, select a pair, reach both survivor choices, and cancel confirmation;
10. invoke/cancel permanent delete and restore confirmation;
11. navigate sidebar filters and alphabet buttons;
12. close the active surface with `Esc`.

Focus must remain visible and reachable throughout.

## Screen-reader scenario

Verify a user can determine:

- app/window name;
- search purpose and result count;
- current browse/filter state;
- selected contact;
- every editor field/row and its add/remove action;
- Favorite/Archived state;
- save/delete/discard actions;
- validation/status changes;
- Settings controls/theme;
- Data Tools import/export/backup/restore descriptions;
- duplicate pair score/reasons;
- first versus second duplicate record details;
- which survivor button keeps which record;
- confirmation-dialog purpose and actions.

## Scaling scenario

At representative 125%, 150%, 200% or platform-equivalent scaling verify:

- essential controls remain reachable;
- editor/duplicate panes scroll correctly;
- add/remove/survivor labels remain understandable;
- status text wraps;
- minimum-window constraints remain practical;
- focus indication remains visible;
- group/tag/address rows do not become unusably compressed.

## Platform matrix

### Windows

Test keyboard-only navigation, Narrator and/or another representative screen reader, 125/150/200% scaling, contrast themes where relevant, and Light/Dark/System app themes.

### macOS

Test Full Keyboard Access, VoiceOver, Retina/high-DPI behavior, and theme changes.

### Linux

Test keyboard navigation, a representative supported desktop accessibility stack such as Orca where Avalonia/backend support permits, high-DPI/font scaling, and more than one desktop environment if broad Linux claims are made.

Record exact OS/app commit/assistive technology/result rather than saying only “accessibility tested.”

## Contributor requirements

For new UI controls:

- prefer standard Avalonia controls when suitable;
- provide visible text labels;
- give icon-only controls meaningful accessible names if introduced;
- distinguish repeated-row actions contextually;
- do not communicate state by color alone;
- preserve keyboard execution paths;
- make focus obvious;
- keep content reachable when text grows;
- respect reduced-motion for custom animation;
- avoid auto-focusing destructive actions;
- add non-visual state tests and manual assistive-technology notes.

## Automated testing boundary

View-model/unit tests can verify state, draft semantics, commands, and data preservation, but they do not prove screen-reader output, focus visuals, contrast, or platform accessibility bridges.

If stable Avalonia accessibility automation becomes available, add it as a supplement—not a replacement—for manual release checks.

## Known follow-up work

High-value accessibility improvements remain:

- automated shortcut/command-state tests where reliable;
- context-specific automation names for repeated `Remove` controls if manual testing shows ambiguity;
- richer announcements for validation and dynamic result changes;
- screen-reader audit of the full editor and duplicate-review pane;
- high-contrast audit;
- responsive layout improvements for large text/small displays;
- documented platform-specific accessibility quirks from release testing.
