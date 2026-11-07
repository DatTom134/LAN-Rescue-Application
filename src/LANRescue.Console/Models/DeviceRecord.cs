using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using DeviceTypeEnum = LANRescue.Domain.Enums.DeviceType;

namespace LANRescue.ConsoleApp.Models;

/// <summary>Read-only presentation adapter used by the Devices grid.</summary>
public sealed class DeviceRecord(ManagedDevice source)
{
    public ManagedDevice Source { get; } = source;

    public string DeviceCode => Source.DeviceCode;
    public string DeviceName => Source.DeviceName;
    public string DeviceType => GetDeviceTypeLabel(Source.Type);
    public string IPv4 => Source.PrimaryIpv4Address is { } address
        ? address.PrefixLength is { } prefix ? $"{address.Address}/{prefix}" : address.Address
        : "—";
    public string MacAddress => Source.PrimaryMacAddress ?? "—";
    public string Location => Source.Location ?? "—";
    public string Department => Source.Department ?? "—";
    public string Reachability => Source.Reachability.ToString();
    public string IpStatus => GetIpStatus(Source);
    public string ManagementStatus => GetManagementStatus(Source);
    public string InventoryState => Source.IsManaged
        ? Source.IsSampleData ? "Managed · sample" : "Managed"
        : Source.IsSampleData ? "Not managed · sample" : "Not managed";

    public string NetworkInterfaceName => Source.PrimaryNetworkInterface?.Name ?? "—";
    public string LinkStatus => Source.PrimaryNetworkInterface?.LinkStatus.ToString() ?? "Unknown";
    public string SubnetPrefix => Source.PrimaryIpv4Address?.PrefixLength is { } prefix ? $"/{prefix}" : "—";
    public string IpAssignment => Source.PrimaryIpv4Address?.AssignmentMode.ToString() ?? "—";
    public string Gateway => Source.PrimaryNetworkInterface?.DefaultGateway ?? "—";
    public string DnsServers => Source.PrimaryNetworkInterface is { DnsServers.Count: > 0 } nic
        ? string.Join(", ", nic.DnsServers)
        : "—";
    public string Hardware
    {
        get
        {
            var value = string.Join(" ", new[] { Source.Manufacturer, Source.Model }
                .Where(item => !string.IsNullOrWhiteSpace(item)));
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }
    }
    public string FirmwareVersion => Source.FirmwareVersion ?? "—";
    public string AllNetworkInterfaces => Source.NetworkInterfaces.Count == 0
        ? "—"
        : string.Join(" | ", Source.NetworkInterfaces.Select(nic =>
        {
            var mac = nic.MacAddress ?? "MAC n/a";
            var addresses = nic.Addresses.Count == 0
                ? "no IP"
                : string.Join(", ", nic.Addresses.Select(address => address.Address));
            return $"{nic.Name} [{nic.LinkStatus}] {mac}: {addresses}";
        }));
    public string SerialNumber => Source.SerialNumber ?? "—";
    public string OperatingSystem => Source.ComputerInfo is { } computer
        ? string.Join(" · ", new[]
            {
                string.Join(" ", new[] { computer.OperatingSystemName, computer.OperatingSystemVersion }
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
                computer.Architecture,
                computer.DomainOrWorkgroup
            }.Where(value => !string.IsNullOrWhiteSpace(value)))
        : "—";
    public string Architecture => Source.ComputerInfo?.Architecture ?? "—";
    public string DomainOrWorkgroup => Source.ComputerInfo?.DomainOrWorkgroup ?? "—";
    public string AgentId => Source.Agent?.AgentId.ToString() ?? "—";
    public string AgentVersion => Source.Agent?.AgentVersion ?? "—";
    public string AgentStatus => Source.Agent?.ConnectionStatus.ToString() ??
        (Source.Type is DeviceTypeEnum.Computer or DeviceTypeEnum.Server ? "Not installed" : "Not applicable");
    public string LastHeartbeat => Source.Agent?.LastHeartbeatAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string LastSeen => Source.LastSeenAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string LastInventory => Source.LastInventoryAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string ManagementChannels
    {
        get
        {
            var channels = Source.ManagementChannels.Select(channel =>
            {
                var endpoint = channel.Endpoint ?? "endpoint n/a";
                var port = channel.Port is { } value ? $":{value}" : string.Empty;
                return $"{channel.Method} · {endpoint}{port} ({channel.Status})";
            }).ToList();

            if (Source.Agent is not null)
                channels.Insert(0, $"LAN Rescue Agent ({Source.Agent.ConnectionStatus})");

            return channels.Count == 0 ? "—" : string.Join("; ", channels);
        }
    }
    public string DeviceSpecificDetails
    {
        get
        {
            if (Source.PrinterInfo is not { } printer)
                return Source.Notes ?? "—";

            var driver = printer.DriverName ?? "—";
            var port = printer.PortName ?? "—";
            var server = printer.PrintServer ?? "—";
            var share = printer.ShareName ?? "—";
            var shared = printer.IsShared?.ToString() ?? "Unknown";
            var paused = printer.IsPaused?.ToString() ?? "Unknown";
            return $"Driver: {driver} | Port: {port} | Status: {printer.OperationalStatus} | " +
                   $"Server: {server} | Share: {share} | Shared: {shared} | Paused: {paused}";
        }
    }

    private static string GetIpStatus(ManagedDevice device)
    {
        var addresses = device.NetworkInterfaces.SelectMany(nic => nic.Addresses).ToArray();
        if (addresses.Any(address => address.Health == IpAddressHealth.Conflict)) return "Conflict";
        if (addresses.Any(address => address.Health == IpAddressHealth.Duplicate)) return "Duplicate";
        if (addresses.Length == 0 || addresses.All(address => address.Health == IpAddressHealth.Unknown)) return "Unknown";
        return "Normal";
    }

    private static string GetManagementStatus(ManagedDevice device)
    {
        if (device.Agent is not null) return $"Agent {device.Agent.ConnectionStatus}";
        if (device.Type is DeviceTypeEnum.Computer or DeviceTypeEnum.Server) return "Agent Not installed";
        var channel = device.ManagementChannels.FirstOrDefault();
        return channel is null ? "Not configured" : $"{channel.Method} {channel.Status}";
    }

    private static string GetDeviceTypeLabel(DeviceTypeEnum type) => type switch
    {
        DeviceTypeEnum.AccessPoint => "Access point",
        DeviceTypeEnum.Firewall => "Firewall",
        _ => type.ToString()
    };
}
