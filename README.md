# LAN Rescue v0.4.1 – Fresh Fluent

LAN Rescue is a Windows desktop project for inventorying IT-managed endpoints and preparing a safe remote network-recovery workflow.

## What's included

- WPF Console with Dashboard, Devices, IP Conflicts, IP Recovery, Diagnostics and Settings screens.
- Fresh Fluent – Bright Blue / Sky Glass resource system with Light/Dark themes and a saved per-user theme preference.
- Native Windows 11 Mica / transient backdrop and rounded-corner requests with a normal WPF fallback.
- Shared rounded Fluent styles, hover/pressed/focus/disabled states, fade transitions, combo-box popups and context menus.
- Fresh Fluent Light/Dark surface hierarchy: application frame, content canvas, card, elevated popup and input surfaces; bright blue accent and restrained semantic status colors.
- Standardized spacing and control dimensions; Devices toolbar wraps at narrow widths.
- Standardized DataGrid rows to 46px, headers to 44px, and truncates long cell values with ellipses.
- Devices workspace redesigned with a searchable/filterable table and a right-side device inspector.
- Device context menu to copy device code, copy IPv4 and edit the selected device.
- Separate Domain, Application and Infrastructure projects.
- Persistent local device catalog using SQLite (`Microsoft.Data.Sqlite` 10.0.14).
- Create and edit device records, including IT-defined code, native device name, device type, department, location, hardware, primary network interface, IPv4/prefix, MAC, gateway, DNS and basic printer queue details.
- Soft retirement and reactivation of device records; no permanent delete action.
- Case-insensitive unique `DeviceCode` and unique registered `AgentId` constraints.
- Sample data initialization only when the database is empty.

**Still not implemented:** real network discovery, SNMP/SSH/IPP polling, Agent enrollment/communication, endpoint control, IP changes or remote command execution. Network reachability and Agent values shown in sample records are not live measurements.

## Requirements

- Windows 10/11
- .NET 10 SDK
- Visual Studio Code with C# Dev Kit
- Network access to NuGet during first restore

## Run

From the repository root:

```powershell
dotnet --info
dotnet restore .\LANRescue.sln
dotnet build .\LANRescue.sln
dotnet run --project .\src\LANRescue.Console\LANRescue.Console.csproj
```

## UI themes and backdrop

Use the theme toggle in the top bar or open **Settings → Appearance** to switch between Light and Dark. The choice is persisted at:

```text
%LOCALAPPDATA%\LANRescue\settings\theme.txt
```

On supported Windows 11 builds, the application requests Mica for the main window, a transient backdrop for the device editor dialog, rounded corners and native dark title-bar treatment. When DWM attributes are unavailable, regular WPF surface brushes are used. ComboBox and ContextMenu popups use translucent XAML surfaces; they do not use a separate native Acrylic DWM backdrop in this preview.

See [docs/Fluent-Glass-UI.md](docs/Fluent-Glass-UI.md) for material behavior and [docs/Fresh-Fluent-Color-System-v0.4.1.md](docs/Fresh-Fluent-Color-System-v0.4.1.md) for the updated layout, palette and contrast audit.

## Local data

SQLite database:

```text
%LOCALAPPDATA%\LANRescue\data\lanrescue.db
```

Theme preference:

```text
%LOCALAPPDATA%\LANRescue\settings\theme.txt
```

## Device identity and safety rules

1. `Id` is the stable internal database key (GUID).
2. `DeviceCode` is the IT-assigned inventory identifier, unique without case sensitivity.
3. `DeviceName` is the hostname or printer name and is not a permanent identity.
4. IP and MAC addresses are mutable observations, never primary keys.
5. A LAN Rescue Agent must have its own authenticated `AgentId`, independent of IPv4/MAC.
6. Never store passwords, private keys, tokens or SNMP community secrets in inventory records.
7. Device retirement uses `IsManaged = false`; the record is retained.
8. Do not attempt IP recovery until device identity is verified through an authenticated management channel and the change has allocation checks, backup and rollback.

## Project structure

- `src/LANRescue.Console`: WPF UI, view models/presentation adapters, Fresh Fluent theme resources and Fluent window integration.
- `src/LANRescue.Domain`: device entities, enums and validation rules.
- `src/LANRescue.Application`: device-catalog use cases and repository contract.
- `src/LANRescue.Infrastructure`: SQLite persistence implementation.
- `src/LANRescue.Contracts`: placeholder for versioned Console-Agent messages.
- `src/LANRescue.Agent`: placeholder for a future Windows Service.
- `docs/Device-Data-Model.md`: device data model.
- `docs/SQLite-Device-Catalog.md`: SQLite schema and operation notes.
- `docs/Fluent-Glass-UI.md`: UI theme, controls, backdrops and transition notes.
