using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>
/// Tổng hợp các lần quan sát cùng một IPv4 trên cùng một adapter.
/// </summary>
public sealed record IpConflictGroup
{
    public required string AdapterId { get; init; }

    public required string AdapterName { get; init; }

    public required string SourceAddress { get; init; }

    public required string IpAddress { get; init; }

    public IReadOnlyList<string> MacAddresses { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<IpAddressObservation> Observations { get; init; }
        = Array.Empty<IpAddressObservation>();

    public IpConflictStatus Status { get; init; }
        = IpConflictStatus.InsufficientEvidence;

    public DateTimeOffset FirstSeenAt { get; init; }

    public DateTimeOffset LastSeenAt { get; init; }

    public int ObservationCount => Observations.Count;

    public string MacAddressesDisplay =>
        string.Join(", ", MacAddresses);
}
