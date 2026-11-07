# Device Profile UI Review & Fix — v0.4.1

## Changes
- TextBox `PART_ContentHost` now stretches vertically/horizontally; this addresses the clipped glyphs seen in the supplied Device profile screenshot.
- Primary Save button explicitly uses `PrimaryButtonForegroundBrush` at the call site in addition to the shared `PrimaryButtonStyle`, preventing a local/inherited foreground from making the label white on the bright-blue button.
- Added theme-aware ScrollBar track/thumb/hover tokens and a shared ScrollBar template so Dark theme no longer shows the bright default OS track.
- Increased standard Button/TextBox/ComboBox typography to 13px while preserving the 38px control height. Section headings in Device profile are 12px.
- Firmware version now spans the complete form width.
- Footer now has a subtle top divider and stays outside the scroll viewer; scroll content has bottom breathing room.

## Project-wide prevention
`UI-QUALITY-GATES-v0.4.1.md` defines mandatory rules for every future view/dialog/control. Treat a shared control defect as a design-system defect and fix the shared template/resource, then review all consumers.

## Verification status
- XML well-formedness and theme resource parity are checked in the packaged source.
- Runtime build and visual verification must be performed on Windows with .NET 10 SDK / WPF Desktop runtime. This packaging environment does not include `dotnet`, so no runtime-build claim is made.
