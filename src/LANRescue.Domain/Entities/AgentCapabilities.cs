namespace LANRescue.Domain.Entities;

public static class AgentCapabilities
{
    public const string ReadNetworkConfiguration =
        "ReadNetworkConfiguration";

    public const string RunDiagnostics =
        "RunDiagnostics";

    public const string ChangeNetworkConfiguration =
        "ChangeNetworkConfiguration";

    public const string RollbackNetworkConfiguration =
        "RollbackNetworkConfiguration";
}