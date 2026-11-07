# LAN Rescue v0.4.1 – TextBox and Control Color Fix

## What changed

1. Single-line TextBox defaults now set `ScrollViewer.VerticalScrollBarVisibility=Disabled`. WPF `Hidden` only hides the scrollbar; it does not disable scrolling.
2. `PART_ContentHost` binds horizontal/vertical scroll behavior from the TextBox, so multi-line Notes can still use `VerticalScrollBarVisibility=Auto`.
3. `CanContentScroll=False`, full-height content hosting, device-pixel snapping, and reduced vertical padding improve stable text rendering in 38px fields.
4. Light primary buttons use a pale blue fill (`#DCEBFF`) with deep-blue text (`#174A91`) and a soft border.
5. Dark controls use a brighter, more saturated palette: vivid blue action buttons (`#348FFF`), blue-tinted fields (`#1A3655`), brighter control borders (`#6FAFE8`), and richer hover/selected surfaces.

## Why the TextBox could scroll

The previous template used `VerticalScrollBarVisibility=Hidden`. In WPF, `Hidden` suppresses the scrollbar UI while preserving the ability to scroll the content. For single-line fields this creates an unexpected vertical scroll experience. `Disabled` is the correct default. The template now uses `TemplateBinding` so a multi-line field can explicitly opt into `Auto`.

## Validation

XML/XAML parsing, theme key parity, resource references, and color contrast were checked statically. A Windows WPF build and interactive text-entry test are still required to verify runtime rendering and input behavior.
