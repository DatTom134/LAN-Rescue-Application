using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>
/// LAN Rescue Agent identity and health. AgentId is independent of IP/MAC and must be authenticated.
/// Never store private keys, passwords, or reusable secrets in this model.
/// </summary>
public sealed record AgentRegistrationInfo
{
    public Guid AgentId { get; init; }
    public string? AgentVersion { get; init; }
    public AgentConnectionStatus ConnectionStatus { get; init; } = AgentConnectionStatus.Installed;
    public string? CertificateThumbprint { get; init; }
    public DateTimeOffset? LastHeartbeatAt { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
}
