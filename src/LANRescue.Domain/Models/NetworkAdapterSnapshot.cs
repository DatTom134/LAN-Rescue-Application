using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Models;

public sealed record NetworkAdapterSnapshot
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? InterfaceType { get; init; }

    public NetworkLinkStatus LinkStatus { get; init; }

    public string? MacAddress { get; init; }

    public IReadOnlyList<string> Ipv4Addresses { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Subnets { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Gateways { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> DnsServers { get; init; }
        = Array.Empty<string>();

    public bool IsOperational { get; init; }

    public DateTimeOffset ObservedAt { get; init; }
        = DateTimeOffset.UtcNow;

    public NetworkInterfaceCategory Category { get; init; }
        = NetworkInterfaceCategory.Unknown;

    public bool? IsHardwareInterface { get; init; }

    public bool? IsHidden { get; init; }
}