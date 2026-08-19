# Accessibility

ContactCore targets practical WCAG-oriented desktop accessibility while respecting platform conventions.

## Baseline requirements

Every user-facing change should preserve:

- keyboard reachability for all primary actions;
- visible focus indication;
- descriptive text labels for input fields;
- meaningful button text/tooltips rather than icon-only ambiguity;
- touch/pointing-device targets with comfortable spacing;
- readable light/dark theme contrast;
- status information that is not communicated by color alone;
- text wrapping/scaling without clipping important content;
- reduced-motion behavior if nonessential animation is introduced.

## Current workspace

The initial workspace uses text-labeled controls for search, filters, editing, save, favorite/archive, delete, export, and backup. Validation and operation status are displayed as text. The window has a documented minimum size and uses Avalonia theme resources rather than hard-coded foreground/background pairs for most surfaces.

## Keyboard review

Before release, verify without a mouse:

1. Tab through search, filters, list, editor fields, and action buttons.
2. Confirm the selected control has a visible focus indicator.
3. Change list selection and edit fields.
4. Activate Save, Favorite/Archive, Export, and Backup.
5. Ensure focus does not disappear after list refresh.
6. Check that destructive actions cannot be triggered accidentally by routine navigation.

A full shortcut map is planned; shortcuts must supplement, not replace, ordinary keyboard navigation.

## Screen readers

When adding custom controls or icon-only visuals, provide accessible names/descriptions through Avalonia accessibility properties. Do not encode state only in decorative glyphs. Important dynamic statuses should be exposed in a way assistive technology can discover; this requires platform-specific manual verification before claiming screen-reader conformance.

## Themes and contrast

Use dynamic theme resources so system/high-contrast behavior can be improved centrally. Do not rely on opacity alone for critical information. Check validation text, selected rows, disabled controls, links, focus rings, and placeholder text in both light and dark modes.

## Text and localization readiness

Avoid fixed-width containers around long user-facing labels where possible. Keep UI strings centralized/externalizable as the localization phase progresses. User contact values must preserve Unicode exactly even when normalized search keys are used for matching.

## Testing checklist for pull requests

For UI-affecting PRs, record:

- OS and theme tested;
- keyboard-only result;
- scaling/text-size result;
- narrow-window result;
- whether new icons have text/accessibility labels;
- any known assistive-technology limitation.

Accessibility regressions are product defects, not cosmetic issues.
