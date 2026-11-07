# LAN Rescue v0.4.0-preview.1 — Fluent Glass UI

## Added

- Shared Fluent Glass resource system with Light and Dark themes.
- Runtime theme switching from the top bar and Settings page.
- Theme persistence under `%LOCALAPPDATA%\LANRescue\settings\theme.txt`.
- Native Windows 11 DWM backdrop requests: Mica for the main window, transient backdrop for the device editor dialog, and rounded window corners where supported.
- Shared rounded styles for buttons, text boxes, combo boxes, DataGrid, context menus and cards.
- Button hover/pressed feedback and page/inspector transitions.
- Devices page redesigned as a data grid with a right-side inspector panel.
- Devices context menu: copy device code, copy IPv4 and edit device.
- Right-clicking a device row selects it before opening the context menu.

## Retained

- SQLite persistence and the existing device add/edit/stop-managing/reactivate flows.
- Existing Domain, Application and Infrastructure project boundaries.
- Safety restrictions: no actual remote network configuration changes are enabled.

## Verification

XAML well-formedness and static resource/event-handler checks passed. A real `dotnet build` and visual rendering check on Windows are still required because the build environment used to prepare this archive does not have the .NET SDK or a WPF desktop runtime.

## Build fix – preview.4

- Fixed WPF markup compile error MC4111 in `Resources/Controls.xaml`: pressed-state scaling now targets the named template `Grid` (`Root`) and sets its `RenderTransform`, instead of targeting a nested `ScaleTransform` by name.
- Details: see `docs/Build-Fix-preview.4.md`.

## Contrast and readability fix 1

- Added a theme-aware default TextBlock style so text without an explicit Foreground remains readable in both Light and Dark themes.
- Increased contrast for secondary/sidebar text, input/card borders, selected/hover states, popup surfaces, and primary action buttons.
- Separated the accent/focus color from the primary button fill to preserve white button text contrast in Dark mode.
- Improved disabled control readability and keyboard focus indication.
- Added a color/contrast audit report: `docs/Color-Contrast-Audit-v0.4.0-preview.1-fix.1.md`.
- Static XAML/resource/handler checks passed. A Windows `dotnet build` and live UI review are still required.
