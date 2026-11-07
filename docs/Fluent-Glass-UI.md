# Fluent Glass UI — v0.4.0-preview.1

## Design goals

- Keep the WPF + .NET 10 stack and the existing Domain/Application/Infrastructure boundaries.
- Use a compact desktop shell: fixed navigation, command/header area, page host and status footer.
- Keep device identity, IP, MAC and warning states more prominent than decorative effects.
- Support Light and Dark themes with a per-Windows-user preference.
- Use shared control templates so buttons, text boxes, combo boxes, DataGrid rows and menus remain visually consistent.

## Theme resources

- `Resources/Colors.Light.xaml`: light color tokens and brushes.
- `Resources/Colors.Dark.xaml`: dark color tokens and brushes.
- `Resources/Controls.xaml`: shared styles and templates.
- `Services/ThemeManager.cs`: loads and persists the active theme.

The active theme is persisted at:

```text
%LOCALAPPDATA%\LANRescue\settings\theme.txt
```

All theme-dependent page/control colors use `DynamicResource`, allowing the dictionaries to be swapped at runtime.

## Window materials

`Services/WindowBackdrop.cs` uses `DwmSetWindowAttribute` and `DwmExtendFrameIntoClientArea` on supported Windows 11 builds. It also makes the WPF composition target transparent so the native system material can be visible behind the WPF client area:

- Main application: `DWMSBT_MAINWINDOW` (Mica system backdrop).
- Device editor dialog: `DWMSBT_TRANSIENTWINDOW` (Desktop Acrylic system backdrop).
- Rounded window corners and immersive dark mode are also requested where supported.

The APIs are guarded by Windows build checks and optional native-call fallbacks. On Windows builds without these attributes, the normal WPF surface brushes remain in use. This preview does not apply native DWM blur to every ComboBox or ContextMenu popup; those use translucent XAML surfaces and shadows instead.

## Motion and interaction

- Navigation page host: fade + short vertical slide.
- Device inspector: short fade + horizontal slide on row selection.
- Buttons and navigation items: hover overlay fade and pressed-state feedback.
- ComboBox popup: WPF fade animation, rounded surface and hover/selected item states.
- Context menu: rounded translucent surface and drop shadow.

## Device page

The Devices page uses a data table on the left and a device inspector on the right. The table has a context menu for copying the device code or IPv4 and opening the editor. Right-click selects the row under the pointer before displaying the menu.

## Safety and functional scope

This release changes presentation only. It does not add network polling, discovery, SSH/SNMP/IPP communication, Agent enrollment, remote commands or IP modification. Dashboard network status values remain illustrative sample values. The SQLite device catalog and existing create/edit/retire/reactivate workflows are retained.

## Verification notes

The source was statically checked for XML well-formedness, theme resource references, XAML event-handler names and ZIP integrity. The current authoring environment does not contain the .NET SDK, so a real WPF `dotnet build` and Windows rendering test could not be run here. Run the build and visual checks on Windows using the instructions in the root README.
