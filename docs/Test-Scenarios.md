# LAN Rescue test scenarios

## Device catalog persistence

1. Launch LAN Rescue and open Devices for the first time. Confirm six sample records are inserted.
2. Close and reopen the app. Confirm records are loaded from SQLite and are not duplicated.
3. Add a computer with a new device code, hostname, department and location. Confirm it appears in the list.
4. Close and reopen the app. Confirm the added record remains.
5. Add another record using the same DeviceCode with different letter casing. Confirm validation rejects it.
6. Edit the first record's name and location. Confirm values persist after restart.
7. Stop managing a record. Confirm it moves to Not managed, remains in All devices, and disappears from Managed only.
8. Reactivate it. Confirm it appears in Managed only again.
9. Add a printer and enter a driver, port, print server and share name. Confirm printer-specific details appear in the detail panel.
10. Enter an invalid IPv4, invalid gateway, invalid DNS address or prefix outside 0–32. Confirm the editor blocks saving.

## Identity and duplicate IP demonstration

- Keep QA-PC-002 and QA-PC-003 as distinct sample rows with the same IPv4 address.
- Confirm each has its own DeviceCode and internal GUID.
- Do not merge or select a device based on IPv4 or ARP output alone.

## Not yet testable in this preview

- Actual device discovery, SNMP/SSH/IPP polling, Agent communication, remote diagnostics and remote IP changes are not implemented.
