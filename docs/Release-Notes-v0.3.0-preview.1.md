# LAN Rescue v0.3.0-preview.1

## New

- Added `LANRescue.Application` with `IDeviceRepository` and `DeviceCatalogService`.
- Added `LANRescue.Infrastructure` with SQLite persistence through `Microsoft.Data.Sqlite` 10.0.14.
- Added local database creation at `%LOCALAPPDATA%\LANRescue\data\lanrescue.db`.
- Added create/edit dialog for core device identity, inventory, primary IPv4 network settings and printer queue details.
- Added managed / not-managed filtering, search, soft retirement and reactivation.
- Added case-insensitive unique `DeviceCode` and unique non-null `AgentId` indexes.
- Added data validation for required identity fields, IP/prefix syntax and management ports.

## Behavior

- Sample records are inserted once, only when the database is empty.
- Subsequent launches load saved records; they do not overwrite the database with sample data.
- Stop managing retains the record; no physical delete or network change is performed.
- Existing non-primary network interfaces, IPv6 addresses and additional IPv4 addresses are preserved when editing the first IPv4 entry on the primary interface.

## Known limitations

- The editor currently exposes one primary IPv4 address. The domain model still supports multiple interfaces and multiple IPv4/IPv6 addresses; advanced multi-address editing is a future UI task.
- Existing reachability, Agent, OS inventory and diagnostic values remain observations/sample values. They are not refreshed from the network.
- No remote command, IP change, discovery or Agent enrollment is enabled.
- Database storage is per Windows user and is not designed for concurrent access by multiple PCs over a network share.
