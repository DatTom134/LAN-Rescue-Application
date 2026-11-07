# LAN Rescue v0.4.0-preview.1 – Color & Readability Audit / Fix 1

## Summary

This update repairs text that could disappear on the Dark theme and improves the contrast of labels, input boundaries, selected rows, navigation states, popups, and disabled controls. The application remains WPF/.NET 10 and keeps the existing device catalog and SQLite behavior.

## Findings in the previous preview

1. **Unstyled TextBlocks inherited the WPF/system default foreground.** Many labels and values in Dashboard, Diagnostics, IP Recovery, Devices, Settings, and Device Editor did not set `Foreground`. On a dark surface, the inherited default could remain dark text, making the content difficult or impossible to read.
2. **Some Light-theme secondary text was too faint.** Examples from the previous palette: `#718096` on the sidebar `#DCE6F2` measured about **3.18:1**; `#64748B` on the DataGrid header `#EDF2F8` measured about **4.23:1**.
3. **Primary button blue was reused as an accent and a button background.** In the previous Dark theme, white text on `#4F8EF7` measured about **3.21:1**, below the 4.5:1 target for normal text.
4. **Some component outlines and state fills were too subtle.** Input/card borders blended into their surfaces, selected rows/navigation were not distinct enough, and disabled controls were faded to 42–50% opacity.
5. **Translucent surfaces could vary with the desktop backdrop.** Text on a popup or glass panel could have a different effective background depending on what was behind the window.

## Changes made

### Text inheritance

- Added `BaseTextBlockStyle`, which sets `Foreground` to the theme-aware `TextPrimaryBrush` and establishes a consistent 12px base size.
- Made the implicit `TextBlock` style and the three named page text styles inherit from that baseline.
- Set a theme-aware inherited foreground on `MainWindow`, `DeviceEditorWindow`, and all page `UserControl` roots.
- Added consistent theme-aware foreground defaults for `Label`, `CheckBox`, and `RadioButton` so new controls do not fall back to system black text.
- Explicitly pass control foregrounds into the `ContentPresenter` used by Button, navigation, ComboBox, ComboBoxItem, and MenuItem templates.

### Palette and surfaces

- Increased the contrast of light-theme secondary text and sidebar-muted text.
- Strengthened input/card borders and table grid separators.
- Deepened Light-theme hover/selected fills so the state is easier to distinguish from the surrounding surface.
- Increased the opacity of text-bearing glass surfaces and made popup/context-menu surfaces use the elevated surface brush. This reduces wallpaper-dependent contrast changes while retaining the Fluent Glass appearance.
- Kept `PrimaryBrush` as the theme accent/focus color and introduced separate `PrimaryButtonBrush` and `PrimaryButtonForegroundBrush` resources. White button text now sits on a sufficiently dark blue in both themes.
- Adjusted status colors and semantic warning/danger borders.

### Control states and typography

- Increased disabled control opacity from 0.42–0.50 to 0.70 so labels remain readable.
- Added a 2px theme-accent focus border to buttons, text boxes, and ComboBoxes.
- Set theme-aware text caret and selection background brushes for TextBox.
- Increased DataGrid header text to 11px and raised the previous 9px micro-labels to 10px.
- Styled separators and kept ComboBox/ContextMenu popup backgrounds opaque enough to protect text contrast.

## Contrast verification

The calculations below use WCAG relative luminance and composite ARGB surfaces over both black and white desktop backdrops to test a conservative range for translucent areas. They are static palette checks, not a substitute for checking the running app at the user's display scale.

| Theme / text pair | Previous | Updated |
|---|---:|---:|
| Light: sidebar-muted text on sidebar | 3.18:1 | 5.33:1 |
| Light: secondary text on DataGrid header | 4.23:1 | 5.20:1 |
| Light: white primary-button text on primary button fill | n/a | 6.47:1 |
| Dark: white primary-button text on the old accent fill | 3.21:1 | 6.70:1 on the new button fill |
| Dark: secondary text on primary surface | Not reliable for unstyled TextBlocks | 8.30:1 |

Across the audited normal-text pairs, the lowest measured contrast is **4.86:1 in Light** and **6.15:1 in Dark**. Both exceed the 4.5:1 WCAG target for normal text. The measured component outline contrast is approximately **2.92:1 in Light** and **3.03:1 or higher in Dark**, depending on the surface.

## Static validation performed

- All **12 XAML files** parsed as well-formed XML.
- Light and Dark theme dictionaries expose the same **66 color/brush resource keys**.
- **77 distinct** DynamicResource/StaticResource references were checked; no missing resource keys were found.
- **59 TextBlocks** without an explicit foreground are now covered by the global theme-aware text style.
- No hard-coded foreground color remains in the XAML views.
- No text style below 10px remains in the audited XAML files.
- ControlTemplate target names and XAML event-handler references were checked; no missing names/handlers were found.

## Build and runtime limitation

The editing environment does not have the .NET SDK or Windows Desktop/WPF runtime, so this update has **not** been verified with a real `dotnet build` or a live Windows screenshot. Run the build on Windows and inspect both themes at the same display scaling you normally use.

Recommended visual checks:

1. Dashboard cards and all summary labels in Light and Dark.
2. Devices table: normal, hovered, selected row, DataGrid headers, and the right-side inspector.
3. Device Editor: every label, TextBox caret/selection, ComboBox selected value, and its open dropdown.
4. Devices context menu and ToolTips.
5. Disabled Edit / Stop managing / Reactivate buttons and keyboard focus outlines.
6. Theme switching while each of the above controls is visible.

If any actual Windows screenshot still shows a text/surface pair that is hard to read, capture that specific screen with the current theme and display scale; the palette can then be adjusted against the real rendered result.
