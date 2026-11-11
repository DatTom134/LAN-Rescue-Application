namespace LANRescue.Domain.Enums;

public enum IpRecoveryOperationStatus
{
    Draft,
    Validating,
    AwaitingApproval,
    Prepared,
    Applying,
    Verifying,
    Succeeded,
    RollingBack,
    RolledBack,
    Failed,
    Cancelled,
    Expired,
    ManualInterventionRequired
}