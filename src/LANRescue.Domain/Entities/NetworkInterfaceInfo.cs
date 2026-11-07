using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>One physical or virtual NIC. A device may have several interfaces and several IP addresses.</summary>
public sealed record NetworkInterfaceInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? MacAddress { get; init; }
    public NetworkLinkStatus LinkStatus { get; init; } = NetworkLinkStatus.Unknown;
    public bool IsPrimary { get; init; }
    public string? DefaultGateway { get; init; }
    public IReadOnlyList<string> DnsServers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<NetworkAddress> Addresses { get; init; } = Array.Empty<NetworkAddress>();
}
