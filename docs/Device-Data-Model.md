# LAN Rescue — Device Data Model

## 1. Identity: three different identifiers

| Field | Meaning | Example | Can it change? |
|---|---|---|---|
| `Id` | Internal primary key of the LAN Rescue inventory record (GUID) | `45c...` | No; generated once |
| `DeviceCode` | Human-defined inventory code used by IT to distinguish equipment | `QA-PC-002`, `QA-PRN-001` | Only through a controlled rename |
| `DeviceName` | Native name reported by the endpoint or print system | `QA-PC-02`, `QA-PRINTER-01` | Yes; hostname/queue name can be renamed |

`DeviceCode` must be unique in the inventory (case-insensitive). `DeviceName` is required but is not a unique key. IP address and MAC address are observations, not device identity: they can change, be duplicated, or be spoofed.

For a computer, `DeviceName` is the operating-system hostname. For a printer, it is the printer queue/device name reported by the print system. The optional `Location` and `Department` fields describe where the device is used, not its identity.

## 2. Common fields

`ManagedDevice` stores the common information for computers, printers and network equipment:

- Identity: `Id`, `DeviceCode`, `DeviceName`, `Type`.
- Asset information: `Manufacturer`, `Model`, `SerialNumber`, `FirmwareVersion`.
- Organisation/location: `Department`, `Location`, `Notes`.
- Current observation: `Reachability`, `LastSeenAt`, `LastInventoryAt`.
- Network: collection of `NetworkInterfaces`.
- Management paths: collection of `ManagementChannels`.
- Optional type-specific blocks: `ComputerInfo`, `PrinterInfo`.
- Optional authenticated endpoint identity: `Agent`.

Unknown values must remain `Unknown`/`null`; do not guess that an unresponsive device is offline. A status is meaningful only with its observation timestamp.

## 3. Network model

A device can have multiple network interfaces (Ethernet, Wi-Fi, USB NIC, virtual NIC, management VLAN). Each interface can hold multiple IPv4/IPv6 addresses. The model therefore stores:

`ManagedDevice → NetworkInterfaces[] → Addresses[]`

Each interface can record its name, description, MAC address, link status, default gateway, DNS servers and whether it is the primary interface. The Devices detail panel also summarizes all recorded interfaces; the table shows the primary interface for quick scanning. Each address stores its IP, CIDR prefix length, Static/DHCP assignment mode, health (Normal/Conflict/Duplicate/Unknown) and observation time.

Do not assume that the first address or the ARP result identifies the intended endpoint. The UI may display a primary address for convenience, but the complete interface/address collection is the source of truth.

## 4. Remote-management identity and access

For computers, `Agent.AgentId` is generated when the LAN Rescue Agent is enrolled, must be unique in the inventory and is independent of IPv4/MAC. The Agent identity must be authenticated using its enrolled certificate/public-key identity before any command is accepted. `CertificateThumbprint` is an identifier/fingerprint, not a private key.

`ManagementChannels` describe other possible access paths such as SSH, SNMP, IPP, WinRM or a vendor API. `CredentialReference` is only a pointer to a secret in a protected credential store. Never put passwords, private keys, tokens or SNMP community strings into the device record, source code, logs or Git.

A channel marked `Configured` is not proof that a device is reachable or that the current user is authorised. Before a control operation, the application must verify the channel, authenticate the device, check operator permissions and record the action.

## 5. Type-specific information

- `ComputerInfo`: OS name/version, CPU architecture, domain/workgroup and last boot time.
- `PrinterInfo`: driver, port, print server/share, shared/paused flags and queue operational status.
- Switches, routers, firewalls and access points use the common inventory and `ManagementChannels`; later iterations can add strongly typed details such as firmware, VLANs, SSIDs and interface inventory without adding unrelated nullable fields to every device.

## 6. Important implementation rules

1. DeviceCode is the business identifier; `Id` is the database key; `DeviceName` is the reported system name. Keep these separate.
2. Do not use IP or MAC as a permanent primary key.
3. Keep multiple NICs and multiple IP addresses; do not flatten the model to a single IP/MAC.
4. Store address prefixes (for example `/24`) and derive the dotted subnet mask for display when needed.
5. Keep `Unknown` distinct from `Offline`, and keep timestamps beside observations.
6. Do not enable IP changes or remote commands from this inventory screen. Those actions need a separately authenticated and authorised management channel, a configuration backup and rollback strategy.
7. All rows in this source package are sample data. No live network discovery or command execution is performed.


## Inventory lifecycle fields (v0.3.0-preview.1)

- `IsManaged`: whether IT currently includes the device in its active managed inventory.
- `DeactivatedAt`: timestamp set when IT stops managing the device; cleared when the record is reactivated.
- Retiring a device does not delete its row. Its DeviceCode remains reserved while the record is retained, preventing accidental reuse.
