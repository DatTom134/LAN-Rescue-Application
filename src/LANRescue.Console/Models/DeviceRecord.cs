using System.ComponentModel;
using System.Runtime.CompilerServices;
using LANRescue.ConsoleApp.Services;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using DeviceTypeEnum = LANRescue.Domain.Enums.DeviceType;

namespace LANRescue.ConsoleApp.Models;

/// <summary>Read-only presentation adapter used by the Devices grid.</summary>
public sealed class DeviceRecord(ManagedDevice source) : INotifyPropertyChanged
{
    public ManagedDevice Source { get; } = source ?? throw new ArgumentNullException(nameof(source));
    public event PropertyChangedEventHandler? PropertyChanged;

    public string DeviceCode => Source.DeviceCode;
    public string DeviceName => Source.DeviceName;
    public string DeviceType => GetDeviceTypeLabel(Source.Type);
    public string IPv4 => Source.PrimaryIpv4Address is { } address
        ? address.PrefixLength is { } prefix ? $"{address.Address}/{prefix}" : address.Address
        : "—";
    public string MacAddress => Source.PrimaryMacAddress ?? "—";
    public string Location => Source.Location ?? "—";
    public string Department => Source.Department ?? "—";
    public string Reachability => TranslateReachability(Source.Reachability);
    public string IpStatus => GetIpStatus(Source);
    public string ManagementStatus => GetManagementStatus(Source);
    public string InventoryState => Source.IsManaged
        ? LocalizationManager.GetString(Source.IsSampleData ? "DeviceStatus_ManagedSample" : "DeviceStatus_Managed")
        : LocalizationManager.GetString(Source.IsSampleData ? "DeviceStatus_NotManagedSample" : "DeviceStatus_NotManaged");

    public string NetworkInterfaceName => Source.PrimaryNetworkInterface?.Name ?? "—";
    public string LinkStatus => TranslateLinkStatus(Source.PrimaryNetworkInterface?.LinkStatus ?? NetworkLinkStatus.Unknown);
    public string SubnetPrefix => Source.PrimaryIpv4Address?.PrefixLength is { } prefix ? $"/{prefix}" : "—";
    public string IpAssignment => Source.PrimaryIpv4Address is { } address
        ? TranslateAssignmentMode(address.AssignmentMode)
        : "—";
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
            var mac = nic.MacAddress ?? LocalizationManager.GetString("DeviceStatus_MacUnavailable");
            var addresses = nic.Addresses.Count == 0
                ? LocalizationManager.GetString("DeviceStatus_NoIpAddress")
                : string.Join(", ", nic.Addresses.Select(address => address.Address));
            return $"{nic.Name} [{TranslateLinkStatus(nic.LinkStatus)}] {mac}: {addresses}";
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
    public string AgentStatus => Source.Agent is not null
        ? TranslateAgentStatus(Source.Agent.ConnectionStatus)
        : (Source.Type is DeviceTypeEnum.Computer or DeviceTypeEnum.Server
            ? LocalizationManager.GetString("DeviceStatus_NotInstalled")
            : LocalizationManager.GetString("DeviceStatus_NotApplicable"));
    public string LastHeartbeat => Source.Agent?.LastHeartbeatAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string LastSeen => Source.LastSeenAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string LastInventory => Source.LastInventoryAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

    public string ManagementChannels
    {
        get
        {
            var channels = Source.ManagementChannels.Select(channel =>
            {
                var endpoint = channel.Endpoint ?? LocalizationManager.GetString("DeviceStatus_EndpointUnavailable");
                var port = channel.Port is { } value ? $":{value}" : string.Empty;
                return $"{TranslateManagementMethod(channel.Method)} · {endpoint}{port} ({TranslateManagementStatus(channel.Status)})";
            }).ToList();
            if (Source.Agent is not null)
                channels.Insert(0, $"LAN Rescue Agent ({TranslateAgentStatus(Source.Agent.ConnectionStatus)})");
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
            var shared = printer.IsShared.HasValue
                ? LocalizationManager.GetString(printer.IsShared.Value ? "DeviceStatus_Yes" : "DeviceStatus_No")
                : LocalizationManager.GetString("Common_Unknown");
            var paused = printer.IsPaused.HasValue
                ? LocalizationManager.GetString(printer.IsPaused.Value ? "DeviceStatus_Yes" : "DeviceStatus_No")
                : LocalizationManager.GetString("Common_Unknown");
            return LocalizationManager.Format(
                "DeviceStatus_PrinterDetailsFormat",
                driver,
                port,
                TranslatePrinterStatus(printer.OperationalStatus),
                server,
                share,
                shared,
                paused);
        }
    }

    public void RefreshLocalization()
    {
        foreach (var property in new[]
        {
            nameof(DeviceType), nameof(Reachability), nameof(IpStatus), nameof(ManagementStatus),
            nameof(InventoryState), nameof(LinkStatus), nameof(AllNetworkInterfaces),
            nameof(AgentStatus), nameof(DeviceSpecificDetails), nameof(IpAssignment),
            nameof(ManagementChannels)
        })
            OnPropertyChanged(property);
    }

    private static string GetIpStatus(ManagedDevice device)
    {
        var addresses = device.NetworkInterfaces.SelectMany(nic => nic.Addresses).ToArray();
        if (addresses.Any(address => address.Health == IpAddressHealth.Conflict))
            return LocalizationManager.GetString("DeviceStatus_Conflict");
        if (addresses.Any(address => address.Health == IpAddressHealth.Duplicate))
            return LocalizationManager.GetString("DeviceStatus_Duplicate");
        if (addresses.Length == 0 || addresses.All(address => address.Health == IpAddressHealth.Unknown))
            return LocalizationManager.GetString("Common_Unknown");
        return LocalizationManager.GetString("DeviceStatus_Normal");
    }

    private static string GetManagementStatus(ManagedDevice device)
    {
        if (device.Agent is not null)
            return LocalizationManager.Format(
                "DeviceStatus_Agent",
                TranslateAgentStatus(device.Agent.ConnectionStatus));

        if (device.Type is DeviceTypeEnum.Computer or DeviceTypeEnum.Server)
            return LocalizationManager.GetString("DeviceStatus_NotInstalled");

        var channel = device.ManagementChannels.FirstOrDefault();
        return channel is null
            ? LocalizationManager.GetString("DeviceStatus_NotConfigured")
            : $"{TranslateManagementMethod(channel.Method)} · {TranslateManagementStatus(channel.Status)}";
    }

    private static string TranslateAssignmentMode(IpAssignmentMode value) => value switch
    {
        IpAssignmentMode.Static => LocalizationManager.GetString("Enum_IpAssignmentMode_Static"),
        IpAssignmentMode.Dhcp => LocalizationManager.GetString("Enum_IpAssignmentMode_Dhcp"),
        _ => LocalizationManager.GetString("Enum_IpAssignmentMode_Unknown")
    };

    private static string TranslateAgentStatus(AgentConnectionStatus value) => value switch
    {
        AgentConnectionStatus.Installed => LocalizationManager.GetString("Enum_AgentConnectionStatus_Installed"),
        AgentConnectionStatus.Connected => LocalizationManager.GetString("Enum_AgentConnectionStatus_Connected"),
        AgentConnectionStatus.Disconnected => LocalizationManager.GetString("Enum_AgentConnectionStatus_Disconnected"),
        AgentConnectionStatus.Untrusted => LocalizationManager.GetString("Enum_AgentConnectionStatus_Untrusted"),
        _ => value.ToString()
    };

    private static string TranslateManagementMethod(ManagementMethod value) =>
        LocalizationManager.GetString($"Enum_ManagementMethod_{value}", value.ToString());

    private static string TranslateManagementStatus(ManagementChannelStatus value) =>
        LocalizationManager.GetString($"Enum_ManagementChannelStatus_{value}", value.ToString());

    private static string TranslatePrinterStatus(PrinterOperationalStatus value) =>
        LocalizationManager.GetString($"Enum_PrinterOperationalStatus_{value}", value.ToString());

    private static string TranslateReachability(DeviceReachability value) => value switch
    {
        DeviceReachability.Online => LocalizationManager.GetString("DeviceStatus_Online"),
        DeviceReachability.Offline => LocalizationManager.GetString("DeviceStatus_Offline"),
        _ => LocalizationManager.GetString("DeviceStatus_ReachabilityUnknown")
    };

    private static string TranslateLinkStatus(NetworkLinkStatus value) => value switch
    {
        NetworkLinkStatus.Up => LocalizationManager.GetString("DeviceStatus_LinkUp"),
        NetworkLinkStatus.Down => LocalizationManager.GetString("DeviceStatus_LinkDown"),
        NetworkLinkStatus.Disabled => LocalizationManager.GetString("DeviceStatus_LinkDisabled"),
        _ => LocalizationManager.GetString("DeviceStatus_LinkUnknown")
    };

    private static string GetDeviceTypeLabel(DeviceTypeEnum type) => type switch
    {
        DeviceTypeEnum.Computer => LocalizationManager.GetString("Devices_DeviceType_Computer"),
        DeviceTypeEnum.Server => LocalizationManager.GetString("Devices_DeviceType_Server"),
        DeviceTypeEnum.Printer => LocalizationManager.GetString("Devices_DeviceType_Printer"),
        DeviceTypeEnum.Switch => LocalizationManager.GetString("Devices_DeviceType_Switch"),
        DeviceTypeEnum.Router => LocalizationManager.GetString("Devices_DeviceType_Router"),
        DeviceTypeEnum.Firewall => LocalizationManager.GetString("Devices_DeviceType_Firewall"),
        DeviceTypeEnum.AccessPoint => LocalizationManager.GetString("Devices_DeviceType_AccessPoint"),
        DeviceTypeEnum.Other => LocalizationManager.GetString("Devices_DeviceType_Other"),
        _ => type.ToString()
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
