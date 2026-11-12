namespace LANRescue.Contracts.Responses;

public sealed record NetworkSnapshotResponse
{
    public Guid AgentId { get; init; }

    public DateTimeOffset CollectedAt { get; init; }

    public IReadOnlyList<NetworkInterfaceSnapshotDto> Interfaces { get; init; }
        = Array.Empty<NetworkInterfaceSnapshotDto>();
}

public sealed record NetworkInterfaceSnapshotDto
{
    public string InterfaceId { get; init; } = "";

    public string Name { get; init; } = "";

    public string MacAddress { get; init; } = "";

    public bool IsOperational { get; init; }

    public IReadOnlyList<string> Ipv4Addresses { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Subnets { get; init; }
        = Array.Empty<string>();

    public string? DefaultGateway { get; init; }

    public IReadOnlyList<string> DnsServers { get; init; }
        = Array.Empty<string>();
}