namespace LANRescue.Domain.Entities;

/// <summary>Optional inventory details specific to Windows/Linux computers and servers.</summary>
public sealed record ComputerSystemInfo
{
    public string? OperatingSystemName { get; init; }
    public string? OperatingSystemVersion { get; init; }
    public string? Architecture { get; init; }
    public string? DomainOrWorkgroup { get; init; }
    public DateTimeOffset? LastBootAt { get; init; }
}
