using System.Net;

using LANRescue.Domain.Entities;

namespace LANRescue.Application.Interfaces;

public interface IConnectivityProbe
{
    Task<DiagnosticEvidence> PingAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress targetAddress,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    Task<DiagnosticEvidence> TestTcpAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress targetAddress,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    Task<DiagnosticEvidence> ResolveDnsAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress dnsServer,
        string host,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}