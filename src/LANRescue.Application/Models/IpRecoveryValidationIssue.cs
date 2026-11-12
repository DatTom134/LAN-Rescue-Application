namespace LANRescue.Application.Models;

public sealed record IpRecoveryValidationIssue
{
    public required string Code { get; init; }

    public required string Field { get; init; }

    public IpRecoveryIssueSeverity Severity { get; init; }
}