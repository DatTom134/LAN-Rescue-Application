using LANRescue.Domain.Enums;

namespace LANRescue.Domain.Entities;

/// <summary>A non-secret reference to a device-management path, for example SSH, SNMP or IPP.</summary>
public sealed record ManagementChannelInfo
{
    public ManagementMethod Method { get; init; } = ManagementMethod.None;
    public string? Endpoint { get; init; }
    public int? Port { get; init; }
    public ManagementChannelStatus Status { get; init; } = ManagementChannelStatus.NotConfigured;
    public string? CredentialReference { get; init; }
    public DateTimeOffset? LastCheckedAt { get; init; }
}
