using System.Net;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.Domain.Models;

namespace LANRescue.Application.Services;

public sealed class DiagnosticService : IDiagnosticService
{
    private readonly INetworkAdapterProvider _adapterProvider;
    private readonly IConnectivityProbe _connectivityProbe;

    private static readonly TimeSpan ProbeTimeout =
        TimeSpan.FromSeconds(3);

    public DiagnosticService(
        INetworkAdapterProvider adapterProvider,
        IConnectivityProbe connectivityProbe)
    {
        _adapterProvider = adapterProvider
            ?? throw new ArgumentNullException(nameof(adapterProvider));

        _connectivityProbe = connectivityProbe
            ?? throw new ArgumentNullException(nameof(connectivityProbe));
    }

    public Task<IReadOnlyList<NetworkAdapterSnapshot>>
        GetNetworkAdaptersAsync(
            CancellationToken cancellationToken = default)
    {
        return _adapterProvider.GetAdaptersAsync(cancellationToken);
    }

    public async Task<DiagnosticSession> RunAsync(
        string adapterId,
        string sourceAddress,
        string? tcpHost = null,
        int? tcpPort = null,
        string? dnsHost = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adapterId))
            throw new ArgumentException(
                "Adapter ID is required.",
                nameof(adapterId));

        if (!IPAddress.TryParse(sourceAddress, out var sourceIp) ||
            sourceIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new ArgumentException(
                "A valid Source IPv4 address is required.",
                nameof(sourceAddress));
        }

        if ((tcpHost is null) != (tcpPort is null))
        {
            throw new ArgumentException(
                "TCP Host and TCP Port must be supplied together.");
        }

        if (tcpPort is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(tcpPort));

        cancellationToken.ThrowIfCancellationRequested();

        var adapters = await _adapterProvider.GetAdaptersAsync(
            cancellationToken);

        var adapter = adapters.FirstOrDefault(
            item => string.Equals(
                item.Id,
                adapterId,
                StringComparison.OrdinalIgnoreCase));

        if (adapter is null)
            throw new InvalidOperationException(
                "The selected network adapter no longer exists.");

        // Không cho phép kiểm tra bằng một Source IP
        // không thuộc adapter đang được chọn.
        if (!adapter.Ipv4Addresses.Contains(
                sourceIp.ToString(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Source IPv4 {sourceIp} does not belong to adapter {adapter.Name}.");
        }

        var gateway = adapter.Gateways
            .Select(value => IPAddress.TryParse(value, out var ip) ? ip : null)
            .FirstOrDefault(ip =>
                ip is not null &&
                ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        if (gateway is null)
        {
            throw new InvalidOperationException(
                "The selected adapter does not have an IPv4 gateway.");
        }

        var sessionId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;

        var evidence = new List<DiagnosticEvidence>();

        // 1. Gateway ICMP test.
        evidence.Add(await _connectivityProbe.PingAsync(
            sessionId,
            adapter.Id,
            sourceIp,
            gateway,
            ProbeTimeout,
            cancellationToken));

        // 2. Optional TCP test.
        if (!string.IsNullOrWhiteSpace(tcpHost) && tcpPort.HasValue)
        {
            if (!IPAddress.TryParse(tcpHost, out var tcpTarget) ||
                tcpTarget.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new ArgumentException(
                    "TCP Host must be a valid IPv4 address in this version.",
                    nameof(tcpHost));
            }

            evidence.Add(await _connectivityProbe.TestTcpAsync(
                sessionId,
                adapter.Id,
                sourceIp,
                tcpTarget,
                tcpPort.Value,
                ProbeTimeout,
                cancellationToken));
        }

        // 3. Optional DNS test.
        if (!string.IsNullOrWhiteSpace(dnsHost))
        {
            var dnsServer = adapter.DnsServers
                .Select(value => IPAddress.TryParse(value, out var ip) ? ip : null)
                .FirstOrDefault(ip =>
                    ip is not null &&
                    ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (dnsServer is null)
            {
                throw new InvalidOperationException(
                    "The selected adapter has no configured IPv4 DNS server.");
            }

            evidence.Add(await _connectivityProbe.ResolveDnsAsync(
                sessionId,
                adapter.Id,
                sourceIp,
                dnsServer,
                dnsHost.Trim(),
                ProbeTimeout,
                cancellationToken));
        }

        var overallStatus = evidence.Any(item => item.IsSuccessful)
            ? DiagnosticStatus.Online
            : DiagnosticStatus.Unknown;

        return new DiagnosticSession
        {
            Id = sessionId,
            AdapterId = adapter.Id,
            AdapterName = adapter.Name,
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            OverallStatus = overallStatus,
            Evidence = evidence.AsReadOnly()
        };
    }

    public Task<IReadOnlyList<DiagnosticSession>> GetRecentSessionsAsync(
            int limit = 20,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit));

        // Chưa có DiagnosticRepository/SQLite.
        // Lịch sử sẽ được triển khai ở giai đoạn Persistence.
        IReadOnlyList<DiagnosticSession> sessions =
            Array.Empty<DiagnosticSession>();

        return Task.FromResult(sessions);
    }
}