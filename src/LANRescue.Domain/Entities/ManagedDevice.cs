using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>
/// Inventory aggregate for a managed endpoint or network device.
/// DeviceCode is the organisation-defined identifier; Id is the internal database identity.
/// DeviceName is the native host name or printer queue/device name.
/// </summary>
public sealed record ManagedDevice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string DeviceCode { get; init; }
    public required string DeviceName { get; init; }
    public DeviceType Type { get; init; } = DeviceType.Other;

    public string? Manufacturer { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public string? FirmwareVersion { get; init; }
    public string? Department { get; init; }
    public string? Location { get; init; }
    public string? Notes { get; init; }

    /// <summary>Whether IT currently includes this device in its managed inventory.</summary>
    public bool IsManaged { get; init; } = true;
    public DateTimeOffset? DeactivatedAt { get; init; }

    public DeviceReachability Reachability { get; init; } = DeviceReachability.Unknown;
    public IReadOnlyList<NetworkInterfaceInfo> NetworkInterfaces { get; init; } = Array.Empty<NetworkInterfaceInfo>();
    public ComputerSystemInfo? ComputerInfo { get; init; }
    public PrinterInfo? PrinterInfo { get; init; }
    public AgentRegistrationInfo? Agent { get; init; }
    public IReadOnlyList<ManagementChannelInfo> ManagementChannels { get; init; } = Array.Empty<ManagementChannelInfo>();

    public DateTimeOffset? LastSeenAt { get; init; }
    public DateTimeOffset? LastInventoryAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsSampleData { get; init; }

    public NetworkInterfaceInfo? PrimaryNetworkInterface =>
        NetworkInterfaces.FirstOrDefault(x => x.IsPrimary) ?? NetworkInterfaces.FirstOrDefault();

    public NetworkAddress? PrimaryIpv4Address =>
        PrimaryNetworkInterface?.Addresses.FirstOrDefault(x =>
            System.Net.IPAddress.TryParse(x.Address, out var address) &&
            address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

    public string? PrimaryMacAddress => PrimaryNetworkInterface?.MacAddress;
}
