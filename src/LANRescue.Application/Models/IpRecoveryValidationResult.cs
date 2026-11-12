namespace LANRescue.Application.Models;

public sealed record IpRecoveryValidationResult
{
    public IReadOnlyList<IpRecoveryValidationIssue> Issues { get; init; }
        = Array.Empty<IpRecoveryValidationIssue>();

    public bool IsValid =>
        Issues.All(issue =>
            issue.Severity != IpRecoveryIssueSeverity.Error);

    public IReadOnlyList<IpRecoveryValidationIssue> Errors =>
        Issues.Where(issue =>
            issue.Severity == IpRecoveryIssueSeverity.Error)
        .ToArray();

    public IReadOnlyList<IpRecoveryValidationIssue> Warnings =>
        Issues.Where(issue =>
            issue.Severity == IpRecoveryIssueSeverity.Warning)
        .ToArray();
}
