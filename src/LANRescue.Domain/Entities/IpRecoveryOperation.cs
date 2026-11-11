using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

public sealed record IpRecoveryOperation
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid DeviceId { get; init; }

    public Guid AgentId { get; init; }

    public string InterfaceId { get; init; } = "";

    public IpNetworkConfiguration PreviousConfiguration { get; init; }
        = new();

    public IpNetworkConfiguration DesiredConfiguration { get; init; }
        = new();

    public IpRecoveryOperationStatus Status { get; init; }
        = IpRecoveryOperationStatus.Draft;

    public string? OperatorReason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? PreparedAt { get; init; }

    public DateTimeOffset? AppliedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public string? FailureCode { get; init; }
}