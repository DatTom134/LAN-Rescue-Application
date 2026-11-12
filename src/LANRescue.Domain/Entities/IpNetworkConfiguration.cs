using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

public sealed record IpNetworkConfiguration
{
    public IpAssignmentMode AssignmentMode { get; init; }
        = IpAssignmentMode.Unknown;

    public string? Ipv4Address { get; init; }

    public int? PrefixLength { get; init; }

    public string? DefaultGateway { get; init; }

    public DnsAssignmentMode DnsAssignmentMode { get; init; }
        = DnsAssignmentMode.KeepExisting;

    public IReadOnlyList<string> DnsServers { get; init; }
        = Array.Empty<string>();
}