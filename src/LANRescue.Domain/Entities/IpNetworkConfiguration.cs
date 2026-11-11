using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>
/// Represents the complete IPv4 configuration of one network interface.
/// </summary>
public sealed record IpNetworkConfiguration
{
    public IpAssignmentMode AssignmentMode { get; init; }
        = IpAssignmentMode.Unknown;

    public string? Ipv4Address { get; init; }

    public int? PrefixLength { get; init; }

    public string? DefaultGateway { get; init; }

    public IReadOnlyList<string> DnsServers { get; init; }
        = Array.Empty<string>();
}