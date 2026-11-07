using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>An IP address assigned to a specific network interface.</summary>
public sealed record NetworkAddress
{
    public required string Address { get; init; }
    public int? PrefixLength { get; init; }
    public IpAssignmentMode AssignmentMode { get; init; } = IpAssignmentMode.Unknown;
    public IpAddressHealth Health { get; init; } = IpAddressHealth.Unknown;
    public DateTimeOffset? LastObservedAt { get; init; }
}
