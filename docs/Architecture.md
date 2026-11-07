# LAN Rescue architecture

## Components

- **Console:** WPF desktop UI used by IT staff. The Devices page provides inventory CRUD and status filtering.
- **Domain:** device identity, inventory entities, network interfaces/addresses, enums and catalog validation rules.
- **Application:** device-catalog use cases and the `IDeviceRepository` contract.
- **Infrastructure:** SQLite implementation of `IDeviceRepository` using `Microsoft.Data.Sqlite`.
- **Contracts:** placeholder for versioned Console-Agent request/response messages.
- **Agent:** future Windows Service installed in advance on managed Windows endpoints.

## Persistence boundary

The Console calls `DeviceCatalogService`; the application service validates the aggregate and calls `IDeviceRepository`. The Infrastructure project owns SQL and serialization details. The Domain project does not depend on SQLite or WPF.

The current repository stores the full `ManagedDevice` aggregate as JSON in SQLite while indexing its core identity and management fields. This is a preview storage format; future event history and frequent observations should move to dedicated tables with explicit migrations.

## Device identity

- Domain `Id`: stable internal GUID for a row in the inventory.
- `DeviceCode`: human-defined, case-insensitive unique inventory identifier.
- `DeviceName`: native hostname or printer queue/device name, which can change.
- `AgentId`: separate identity generated at Agent enrollment and authenticated independently of IPv4/MAC.
- IP/MAC: mutable observations attached to one network interface; never primary keys.

## Non-negotiable safety rules

1. IP address alone is not a device identity.
2. A MAC address observed in ARP is evidence, not proof that the intended target is reachable.
3. Do not accept commands until the Agent or management endpoint identity has been authenticated.
4. Do not store credentials, private keys, tokens or SNMP secrets in inventory fields.
5. Do not start an unauthorized DHCP server on the company LAN.
6. Prefer the organization's DHCP/IPAM process or an approved static-IP allocation.
7. Preserve a management channel and provide configuration backup and rollback for IP changes.
8. Unknown reachability is not the same as offline; observations must include timestamps.
9. Stopping management is a soft retirement. Do not delete asset data from the UI by default.
