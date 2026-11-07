# LAN Rescue UI Quality Gates — v0.4.1 onward

These are project-wide acceptance rules for every new or modified Window, UserControl, dialog, popup, form, grid, and reusable control. A screen is not complete until it passes these checks in both Fresh Fluent themes.

## 1. Text and input rendering (P0)
- All text must resolve through theme-aware resources (`TextPrimaryBrush`, `TextSecondaryBrush`, semantic status brushes); never rely on WPF default foreground inheritance.
- Every TextBox template must keep `PART_ContentHost` stretched within its available height. Do not vertically center the ScrollViewer content host in a fixed-height TextBox.
- Standard single-line inputs: minimum height 38px, font size 13px, vertical content centered, readable padding, no glyph clipping at 100%, 125%, 150%, and 200% display scaling.
- ComboBox text and popup items must use theme-aware foreground and surface brushes.
- Primary action buttons must explicitly resolve `PrimaryButtonForegroundBrush`; verify actual rendered foreground rather than assuming a style was applied.

## 2. Contrast and semantic color
- Normal text: WCAG AA contrast at least 4.5:1. Large text: at least 3:1. Essential control boundaries and focus indicators: target at least 3:1 against adjacent surfaces.
- Each screen uses neutral surfaces plus no more than 2–5 semantic/function colors. Blue is action/focus/selection; green is verified healthy/success; amber is warning/unverified; red is conflict/danger; neutral is offline/disabled.
- Never color an entire DataGrid row by status unless a specific, documented workflow requires it; prefer badges, icons, or narrow accents.
- Light and Dark themes must define the same resource keys. No hardcoded color literals in view XAML except documented illustrations.

## 3. Surface hierarchy and spacing
- Preserve the three-layer hierarchy: application frame → content canvas → card/panel → interactive control. Adjacent layers must remain visually distinguishable.
- Use the spacing scale 4/8/12/16/20/24px. Keep card padding consistent and separate adjacent cards/panels.
- Forms should use aligned columns with consistent label-to-control spacing. Avoid stranded half-width fields when a full-width field is more appropriate.
- Fixed action footers must be visually separated from scrollable content and remain visible while the form scrolls. Scrollable content needs bottom padding so the final control is never hidden behind the footer.

## 4. Grid and scrolling
- DataGrid header height 44px; row height 46px; cell horizontal padding 10px; vertical alignment centered; text truncation must not clip glyphs.
- Scrollbars must use the theme palette in both orientations; no bright OS-default scrollbar track may appear inside a Dark surface.
- Scrollable popups, ComboBox dropdowns, DataGrid, and dialogs must all be tested for scrollbar visibility and thumb hover/drag states.

## 5. Interaction states and accessibility
- Every interactive control must have visible normal, hover, pressed, focused, selected, and disabled states where applicable. Disabled controls remain readable.
- Keyboard navigation and focus indication must remain visible; do not remove focus visuals without providing an equivalent.
- Motion must be short and nonessential; the UI must remain understandable if animation is disabled or unsupported.

## 6. Required review matrix before accepting a UI change
- Light and Dark theme.
- Window sizes 1120×700 and 1440×860 (plus minimum supported size).
- Windows display scaling 100%, 125%, and 150%; inspect 200% for clipping-sensitive controls.
- TextBox empty/filled/focused/disabled; ComboBox closed/open/selected; Button normal/hover/pressed/disabled; ScrollBar idle/hover/drag; DataGrid normal/alternate/hover/selected.
- Verify footer stays fixed, last form field is reachable, no content overlaps, no glyphs clip, and primary action text contrast passes.
- Run `dotnet build` on Windows with the .NET 10 SDK and Windows Desktop targeting pack. Record runtime checks separately from static XAML validation.

A UI defect in any one screen is treated as a shared design-system defect: fix the reusable resource/template first, then audit all screens that consume it. Do not patch only the screenshot's one control if the defect can recur elsewhere.

## Additional input and theme-control gates (v0.4.1 UI fix 2)

- Single-line TextBox controls must default to `ScrollViewer.VerticalScrollBarVisibility=Disabled`; `Hidden` is not equivalent because it hides the scrollbar but still permits scrolling.
- The TextBox template must TemplateBind both scroll-visibility attached properties to `PART_ContentHost`. Multi-line fields such as Notes may explicitly opt into `Auto` scrolling.
- TextBox content must remain vertically legible at the standard 38px control height. Use restrained vertical padding (4px in the current 13px typography scale), device-pixel snapping, and a full-height content host.
- Light primary actions use a soft blue surface with a deep-blue foreground; avoid saturated dark fills paired with black text.
- Dark interactive controls should be more chromatic and luminous than their Light equivalents while preserving readable text contrast: vivid sky-blue actions, blue-tinted input borders, richer popup surfaces, and brighter hover/selection states.
- Always validate TextBox keyboard input, long text, caret visibility, mouse-wheel behavior, and multiline Notes scrolling independently.
