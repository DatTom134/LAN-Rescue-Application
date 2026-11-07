namespace LANRescue.Domain.Enums;

public enum DiagnosticProbeOutcome
{
    Succeeded,
    TimedOut,
    ConnectionRefused,
    NameNotFound,
    NoRecords,
    Failed,
    Error,
    Cancelled,
    Skipped
}