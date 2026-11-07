using LANRescue.Domain.Entities;
using LANRescue.Domain.Models;

namespace LANRescue.Application.Interfaces;

public interface IDiagnosticService
{
    Task<IReadOnlyList<NetworkAdapterSnapshot>>
        GetNetworkAdaptersAsync(
            CancellationToken cancellationToken = default);

    Task<DiagnosticSession> RunAsync(
        string adapterId,
        string sourceAddress,
        string? tcpHost = null,
        int? tcpPort = null,
        string? dnsHost = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiagnosticSession>>
        GetRecentSessionsAsync(
            int limit = 20,
            CancellationToken cancellationToken = default);
}