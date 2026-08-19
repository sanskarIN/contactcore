# Accessibility

ContactCore aims for an accessible desktop experience, but the repository does **not** claim formal WCAG, Section 508, EN 301 549, or platform accessibility certification. Automated code review cannot substitute for manual testing with real assistive technologies on each supported operating system.

This document separates implemented design choices from verification still required.

## Current implementation

### Keyboard access

The main window uses standard Avalonia controls for text input, buttons, toggle buttons, check boxes, combo boxes, and lists. Current explicit shortcuts are:

- `Ctrl+N` — new contact;
- `Ctrl+S` — save the active draft when executable;
- `Ctrl+F` — focus the search box;
- `Esc` — close/cancel the active editor, Settings, or Data tools surface.

The event handler marks these key events handled after executing the action.

### Visible focus

`Styles/DesignSystem.axaml` applies an explicit two-pixel accent border for focused:

- `Button`;
- `TextBox`;
- `ComboBox`;
- `CheckBox`;
- `ToggleButton`;
- `ListBox`.

This supplements framework focus behavior so keyboard location is not intended to rely solely on subtle default styling.

### Field labels

The contact editor visually labels:

- Given name;
- Family name;
- Nickname;
- Birthday;
- Phone;
- Email;
- Notes.

Settings/Data tools also use visible section titles and field labels. Visual labels are useful but do not by themselves prove that every assistive-technology accessibility name/relationship is exposed correctly through each Avalonia platform backend; this must be manually verified.

### Text status, not color-only status

Success/failure information is displayed as text in `StatusMessage`/`FooterText`. Favorite state uses both a dedicated filtering context/checkbox in editing and a star marker in the list. Destructive buttons include explicit textual labels such as `Delete permanently`.

### Theme support

Theme selection supports:

- System;
- Light;
- Dark.

The design system uses Avalonia dynamic theme resources for key borders, controls, accents, and cards rather than assuming a single fixed light palette.

### Reduced motion preference

Settings persist a `ReducedMotion` Boolean. The current custom UI contains little bespoke animation, so the setting mainly establishes a contract for future motion. Any new custom animation should consult this preference.

Do not state that all operating-system/framework animation is disabled merely because the preference exists.

### Text wrapping/trimming

Longer status/settings/privacy text uses wrapping. List display names/subtitles use character ellipsis to constrain the middle pane. The local data/backup paths are shown with wrapping.

### Minimum window size

The current main window sets a minimum size of 850×560. Accessibility testing should therefore include smaller displays/scaling configurations to ensure the minimum remains practical.

## Accessibility risks and limitations

### Three-column density

The fixed three-column desktop layout can become dense under high text scaling. It should be tested at common OS scaling levels and with larger text. If content becomes clipped or horizontal navigation becomes impractical, the layout should adapt rather than lowering font size.

### Contact-list semantics

Contact list items contain avatar initials, name, subtitle, and optional star. Verify with screen readers that selection, name, and favorite context are understandable and that decorative initials do not create noisy/redundant announcements.

### Confirmation dialog

The modal confirmation window must be manually checked for:

- initial focus placement;
- clear accessible name/message;
- keyboard ordering of Cancel/Confirm;
- Escape/window-close semantics;
- screen-reader announcement of modal context.

### Editor validation feedback

Validation failures currently appear as status text. Verify that screen-reader users are notified of changes rather than requiring them to navigate manually to discover the error. A future improvement may use an appropriate live-region/automation notification pattern supported by Avalonia.

### Search result changes

Search is debounced and results update dynamically. Verify whether assistive-technology users receive enough result-count/status feedback without disruptive repeated announcements.

### Star icon

The list uses a text star `★` to show favorite state. Ensure it is not the only meaningful signal for users who cannot see the icon and that screen readers do not announce it ambiguously.

### Color contrast

Dynamic resources improve theme integration but do not prove all foreground/background pairs meet a particular contrast ratio. Audit Light, Dark, System/high-contrast variants and focus borders on target platforms.

## Manual test matrix

Before accessibility claims in a release, test representative combinations such as:

### Windows

- keyboard-only navigation;
- Narrator and/or another commonly used screen reader when available;
- 125%, 150%, 200% display scaling;
- Windows high-contrast/contrast themes if Avalonia supports the relevant mapping;
- Light and Dark app themes.

### macOS

- keyboard navigation/full keyboard access settings;
- VoiceOver;
- Retina/high-DPI scaling;
- Light/Dark/System theme changes.

### Linux

- keyboard navigation;
- a representative accessible desktop/session and screen reader such as Orca where supported by the Avalonia backend;
- high-DPI/font scaling;
- multiple desktop environments if the release claims broad Linux support.

Record the exact OS version, app commit/release, assistive technology, and result instead of writing “accessibility tested” generically.

## Keyboard-only scenario

A minimum smoke test should be possible without a mouse:

1. focus search with `Ctrl+F`;
2. type a query and navigate result list;
3. create a contact with `Ctrl+N`;
4. traverse fields with Tab/Shift+Tab;
5. toggle Favorite/Archived;
6. save with `Ctrl+S`;
7. open/close Settings and Data tools;
8. cancel with `Esc`;
9. invoke destructive/restore confirmation and cancel safely;
10. navigate sidebar filters/alphabet buttons.

Focus must remain visible throughout.

## Screen-reader scenario

Verify that a user can determine:

- application/window name;
- purpose of search;
- current browse/filter state;
- selected contact and list count;
- editor field labels/values;
- Favorite/Archived checkbox state;
- save/delete actions;
- validation/status error text;
- Settings controls and selected theme;
- Data tools actions and safety descriptions;
- confirmation-dialog purpose and actions.

## Text/scaling scenario

At increased text/display scaling verify:

- no essential control disappears offscreen permanently;
- detail content remains reachable through scrolling;
- button labels are not clipped beyond recognition;
- status text wraps;
- minimum window constraints do not prevent use on the target display;
- focus indication remains visible.

## Contributor requirements

For new UI controls:

- prefer native/standard Avalonia controls when they meet the interaction need;
- provide visible text labels for form fields;
- provide meaningful accessible names for icon-only controls if introduced;
- do not communicate state by color alone;
- preserve keyboard execution paths;
- make focus obvious;
- ensure content is reachable when text grows;
- respect reduced-motion preference for custom animation;
- avoid auto-focusing destructive actions;
- add non-visual tests for view-model state and manual verification notes for visual/assistive behavior.

## Automated testing boundaries

Unit tests can verify command/state behavior, but they do not prove screen-reader output or visual contrast. If Avalonia accessibility automation testing becomes stable for the target environments, add it as a supplement—not a substitute for manual release checks.

## Known current follow-up work

High-value accessibility improvements include:

- automated/non-visual tests around shortcut-command behavior where practical;
- richer accessible announcements for validation and dynamic result changes;
- manual screen-reader audit of the list/editor/settings/data-tools surfaces;
- high-contrast audit;
- responsive layout improvements for large text/small screens;
- documenting platform-specific assistive-technology quirks found during release testing.
