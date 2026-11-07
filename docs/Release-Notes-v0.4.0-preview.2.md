# LAN Rescue v0.4.0-preview.2 – Layout & Color System Review

This preview refines the Fluent Glass interface after the contrast-fix iteration.

## Changes
- Added a distinct content canvas between the outer app frame and card surfaces in both Light and Dark themes.
- Rebalanced the palette around neutral slate surfaces and one primary blue accent.
- Reserved green, amber and red for semantic status only.
- Standardized shell dimensions, page inset, card spacing, form field heights and button heights.
- Made the Devices toolbar wrap at smaller window widths and balanced its grid/inspector columns.
- Standardized DataGrid rows to 46px and headers to 44px, with subtle alternate-row fill and ellipsis for long cell values.
- Allowed the IP Conflict page/table to scroll and size from content rather than relying on a fixed table height.

## Compatibility
- WPF / .NET 10; no new UI dependency.
- No database schema or application-layer changes.
- Existing SQLite data and CRUD code are unchanged.

## Validation note
Static XML/resource checks are included in the release workflow. A real Windows WPF build and visual review are still required; this environment cannot execute the target desktop runtime.
