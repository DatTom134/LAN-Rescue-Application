using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.ConsoleApp.Models;

/// <summary>Illustrative records only. These values are not collected from the user's network.</summary>
public static class SampleDeviceData
{
    private static readonly DateTimeOffset SampleTime = DateTimeOffset.UtcNow;

    public static IReadOnlyList<DeviceRecord> Create() =>
    [
        new(CreateComputer(
            code: "IT-PC-001", name: "IT-PC-A", ip: "192.168.110.151", mac: "02-00-00-00-00-0A",
            health: IpAddressHealth.Normal, agentStatus: AgentConnectionStatus.Connected,
            department: "IT", location: "IT Office", lastSeenMinutesAgo: 2)),
        new(CreateComputer(
            code: "QA-PC-002", name: "QA-PC-02", ip: "192.168.110.93", mac: "02-00-00-00-00-0B",
            health: IpAddressHealth.Conflict, agentStatus: AgentConnectionStatus.Disconnected,
            department: "QA", location: "QA Office · Floor 3", lastSeenMinutesAgo: 45)),
        new(CreateComputer(
            code: "QA-PC-003", name: "QA-PC-03", ip: "192.168.110.93", mac: "02-00-00-00-00-0C",
            health: IpAddressHealth.Conflict, agentStatus: AgentConnectionStatus.Disconnected,
            department: "QA", location: "QA Office · Floor 3", lastSeenMinutesAgo: 52)),
        new(CreateComputer(
            code: "PMC-PC-001", name: "PMC-PC-01", ip: "192.168.110.75", mac: "02-00-00-00-00-0D",
            health: IpAddressHealth.Unknown, agentStatus: null,
            department: "PMC", location: "PMC Office", lastSeenMinutesAgo: null)),
        new(CreatePrinter()),
        new(CreateSwitch())
    ];

    private static ManagedDevice CreateComputer(
        string code,
        string name,
        string ip,
        string mac,
        IpAddressHealth health,
        AgentConnectionStatus? agentStatus,
        string department,
        string location,
        int? lastSeenMinutesAgo)
    {
        var isDhcp = code == "PMC-PC-001";
        var agentId = agentStatus.HasValue ? Guid.NewGuid() : Guid.Empty;

        return new ManagedDevice
        {
            DeviceCode = code,
            DeviceName = name,
            Type = DeviceType.Computer,
            Manufacturer = "Example OEM",
            Model = "Business Desktop",
            SerialNumber = $"SAMPLE-{code}",
            Department = department,
            Location = location,
            Notes = "Illustrative inventory entry. Not discovered from a real device.",
            Reachability = lastSeenMinutesAgo is <= 5 ? DeviceReachability.Online : DeviceReachability.Unknown,
            NetworkInterfaces =
            [
                new NetworkInterfaceInfo
                {
                    Name = "Ethernet",
                    Description = "Sample Ethernet adapter",
                    MacAddress = mac,
                    LinkStatus = lastSeenMinutesAgo is <= 5 ? NetworkLinkStatus.Up : NetworkLinkStatus.Unknown,
                    IsPrimary = true,
                    DefaultGateway = "192.168.110.9",
                    DnsServers = ["192.168.110.9"],
                    Addresses =
                    [
                        new NetworkAddress
                        {
                            Address = ip,
                            PrefixLength = 24,
                            AssignmentMode = isDhcp ? IpAssignmentMode.Dhcp : IpAssignmentMode.Unknown,
                            Health = health,
                            LastObservedAt = SampleTime.AddMinutes(-2)
                        }
                    ]
                }
            ],
            ComputerInfo = new ComputerSystemInfo
            {
                OperatingSystemName = "Windows 11 Pro",
                OperatingSystemVersion = "24H2 (sample)",
                Architecture = "64-bit",
                DomainOrWorkgroup = "WORKGROUP"
            },
            Agent = agentStatus.HasValue ? new AgentRegistrationInfo
            {
                AgentId = agentId,
                AgentVersion = "0.2.0-demo",
                ConnectionStatus = agentStatus.Value,
                CertificateThumbprint = "SAMPLE-CERTIFICATE-THUMBPRINT",
                LastHeartbeatAt = agentStatus == AgentConnectionStatus.Connected
                    ? SampleTime.AddMinutes(-2)
                    : null,
                Capabilities = ["ReadNetworkConfiguration", "RunDiagnostics"]
            } : null,
            LastSeenAt = lastSeenMinutesAgo.HasValue ? SampleTime.AddMinutes(-lastSeenMinutesAgo.Value) : null,
            LastInventoryAt = SampleTime.AddMinutes(-10),
            IsSampleData = true
        };
    }

    private static ManagedDevice CreatePrinter() => new()
    {
        DeviceCode = "QA-PRN-001",
        DeviceName = "QA-PRINTER-01",
        Type = DeviceType.Printer,
        Manufacturer = "Example Printer Co.",
        Model = "Network Laser Printer",
        SerialNumber = "SAMPLE-PRINTER-001",
        FirmwareVersion = "Printer firmware (demo)",
        Department = "QA",
        Location = "QA Office · Floor 3",
        Reachability = DeviceReachability.Online,
        NetworkInterfaces =
        [
            new NetworkInterfaceInfo
            {
                Name = "LAN",
                MacAddress = "02-00-00-00-00-51",
                LinkStatus = NetworkLinkStatus.Up,
                IsPrimary = true,
                DefaultGateway = "192.0.2.1",
                DnsServers = ["192.0.2.1"],
                Addresses =
                [new NetworkAddress
                {
                    Address = "192.0.2.50", PrefixLength = 24,
                    AssignmentMode = IpAssignmentMode.Static,
                    Health = IpAddressHealth.Normal,
                    LastObservedAt = SampleTime.AddMinutes(-5)
                }]
            }
        ],
        PrinterInfo = new PrinterInfo
        {
            DriverName = "Generic Network Printer Driver",
            PortName = "IP_192.0.2.50",
            PrintServer = "—",
            IsShared = false,
            IsPaused = false,
            OperationalStatus = PrinterOperationalStatus.Ready
        },
        ManagementChannels =
        [new ManagementChannelInfo
        {
            Method = ManagementMethod.Ipp,
            Endpoint = "192.0.2.50",
            Port = 631,
            Status = ManagementChannelStatus.Configured,
            LastCheckedAt = SampleTime.AddMinutes(-5)
        }],
        LastSeenAt = SampleTime.AddMinutes(-5),
        LastInventoryAt = SampleTime.AddMinutes(-5),
        IsSampleData = true
    };

    private static ManagedDevice CreateSwitch() => new()
    {
        DeviceCode = "NET-SW-001",
        DeviceName = "CORE-SW-01",
        Type = DeviceType.Switch,
        Manufacturer = "H3C",
        Model = "Managed Ethernet Switch (sample model)",
        SerialNumber = "SAMPLE-SWITCH-001",
        FirmwareVersion = "Switch firmware (demo)",
        Department = "IT",
        Location = "Server Room · Main Rack",
        Reachability = DeviceReachability.Online,
        NetworkInterfaces =
        [
            new NetworkInterfaceInfo
            {
                Name = "Management VLAN",
                MacAddress = "02-00-00-00-00-61",
                LinkStatus = NetworkLinkStatus.Up,
                IsPrimary = true,
                DefaultGateway = "192.0.2.1",
                DnsServers = ["192.0.2.1"],
                Addresses =
                [new NetworkAddress
                {
                    Address = "192.0.2.2", PrefixLength = 24,
                    AssignmentMode = IpAssignmentMode.Static,
                    Health = IpAddressHealth.Normal,
                    LastObservedAt = SampleTime.AddMinutes(-1)
                }]
            }
        ],
        ManagementChannels =
        [new ManagementChannelInfo
        {
            Method = ManagementMethod.Ssh,
            Endpoint = "192.0.2.2",
            Port = 22,
            Status = ManagementChannelStatus.Configured,
            LastCheckedAt = SampleTime.AddMinutes(-1)
        }, new ManagementChannelInfo
        {
            Method = ManagementMethod.Snmp,
            Endpoint = "192.0.2.2",
            Port = 161,
            Status = ManagementChannelStatus.NotConfigured
        }],
        LastSeenAt = SampleTime.AddMinutes(-1),
        LastInventoryAt = SampleTime.AddMinutes(-1),
        IsSampleData = true
    };
}
