namespace LANRescue.Contracts.Messages;

public sealed record AgentHeartbeatMessage
{
    public Guid AgentId { get; init; }

    public string AgentVersion { get; init; } = "";

    public DateTimeOffset SentAt { get; init; }

    public string CertificateThumbprint { get; init; } = "";

    public IReadOnlyList<string> Capabilities { get; init; }
        = Array.Empty<string>();
}