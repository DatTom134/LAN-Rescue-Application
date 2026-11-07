# SQLite Device Catalog — v0.3.0-preview.1

## Goal

Persist the device inventory between application sessions and provide basic asset-record maintenance without enabling any network-changing operation.

## Storage location

```text
%LOCALAPPDATA%\LANRescue\data\lanrescue.db
```

The database is local to the current Windows profile. The application creates the folder and schema during the first Devices-page load.

## Tables and constraints

The preview uses one `Devices` table:

| Column | Purpose |
|---|---|
| `Id` | Stable GUID primary key |
| `DeviceCode` | IT-defined identifier, unique with case-insensitive comparison |
| `DeviceName` | Hostname or printer device/queue name |
| `AgentId` | Optional Agent identity; unique when present |
| `IsManaged` | Whether the asset is in active IT management |
| `UpdatedAt` | UTC update timestamp |
| `PayloadJson` | Serialized complete `ManagedDevice` aggregate |

Indexes:

- Unique index on `DeviceCode COLLATE NOCASE`.
- Partial unique index on `AgentId` when it is not `NULL`.

SQLite `user_version` is set to `1`. This preview creates the schema with `CREATE TABLE IF NOT EXISTS`; future schema changes must add explicit migrations rather than changing existing columns silently.

## Why the aggregate is stored as JSON

The domain object already contains nested network interfaces, multiple IP addresses, optional computer/printer details, Agent registration and management channels. Serializing the complete aggregate into `PayloadJson` keeps the preview small and ensures these nested fields round-trip together.

The identity, uniqueness and inventory-state fields are also kept as regular SQLite columns so SQLite can enforce key constraints and order/filter the catalogue. For high-volume network observations, heartbeat history, diagnostics and audit events, separate normalized tables should be introduced in a later release.

## Operations

- **Initialize:** create folder, enable WAL and create schema/indexes.
- **Read all:** load JSON payloads ordered by managed state and device code.
- **Read by ID:** retrieve one aggregate.
- **Save:** insert new aggregate or update the existing aggregate by `Id` in a transaction.
- **Stop managing:** set `IsManaged = false` and record `DeactivatedAt`; preserve the row.
- **Reactivate:** set `IsManaged = true` and clear `DeactivatedAt`.

The application seeds the six sample records only when there are no rows in `Devices`. It does not overwrite the catalogue on subsequent launches.

## Validation

- `DeviceCode` and `DeviceName` are required.
- `DeviceCode` must be unique, case-insensitively, including among inactive records.
- A registered `AgentId` must be non-empty and unique.
- A device may have no more than one primary network interface.
- IP address syntax and prefix lengths are checked by the domain validation rules.
- Management ports must be in the TCP/UDP port range.

## Backup and handling

Close LAN Rescue before making a manual copy of the database. SQLite may use a write-ahead log while the application is running. Do not place the local database on a shared network drive for multi-user concurrent access; this preview is a per-user local catalogue, not a multi-user server database.

The database stores inventory data only. It must not contain passwords, private keys, bearer tokens or other reusable secrets.

## Current limitations

- No automatic discovery or synchronization from real devices.
- No authentication/enrollment workflow for Agent yet.
- No audit table for every field change.
- No multi-user central database or conflict-resolution service.
- No physical delete workflow in the UI.
