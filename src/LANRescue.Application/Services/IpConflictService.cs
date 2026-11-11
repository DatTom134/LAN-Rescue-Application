using System.Net;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.Domain.Models;

namespace LANRescue.Application.Services;

public sealed class IpConflictService
{
    private readonly INetworkAdapterProvider _adapterProvider;
    private readonly IIpConflictProbe _probe;
    private readonly IIpConflictRepository _repository;

    public IpConflictService(
        INetworkAdapterProvider adapterProvider,
        IIpConflictProbe probe,
        IIpConflictRepository repository)
    {
        _adapterProvider = adapterProvider
            ?? throw new ArgumentNullException(nameof(adapterProvider));

        _probe = probe
            ?? throw new ArgumentNullException(nameof(probe));

        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        return _repository.InitializeAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NetworkAdapterSnapshot>>
        GetAdaptersAsync(
            CancellationToken cancellationToken = default)
    {
        var adapters = await _adapterProvider.GetAdaptersAsync(
            cancellationToken);

        return adapters
            .Where(adapter =>
                adapter.IsOperational &&
                adapter.Ipv4Addresses.Count > 0)
            .OrderBy(adapter => adapter.Name)
            .ToArray();
    }

    public async Task<IReadOnlyList<IpConflictGroup>> ScanSubnetAsync(
        string adapterId,
        string sourceAddress,
        CancellationToken cancellationToken = default)
    {
        var adapters = await _adapterProvider.GetAdaptersAsync(
            cancellationToken);

        var adapter = adapters.FirstOrDefault(item =>
            string.Equals(
                item.Id,
                adapterId,
                StringComparison.OrdinalIgnoreCase));

        if (adapter is null)
        {
            throw new InvalidOperationException(
                "Selected network adapter was not found.");
        }

        if (!adapter.IsOperational)
        {
            throw new InvalidOperationException(
                "Selected network adapter is not operational.");
        }

        int addressIndex = -1;

        for (int i = 0; i < adapter.Ipv4Addresses.Count; i++)
        {
            if (string.Equals(
                adapter.Ipv4Addresses[i],
                sourceAddress,
                StringComparison.OrdinalIgnoreCase))
            {
                addressIndex = i;
                break;
            }
        }

        if (addressIndex < 0)
        {
            throw new InvalidOperationException(
                "The selected IPv4 address does not belong to this adapter.");
        }

        if (addressIndex >= adapter.Subnets.Count)
        {
            throw new InvalidOperationException(
                "Subnet mask information is unavailable.");
        }

        if (!IPAddress.TryParse(sourceAddress, out var sourceIp) ||
            !IPAddress.TryParse(
                adapter.Subnets[addressIndex],
                out var subnetMask))
        {
            throw new InvalidOperationException(
                "Invalid IPv4 address or subnet mask.");
        }

        var observations = await _probe.ScanSubnetAsync(
            adapter.Id,
            adapter.Name,
            sourceIp,
            subnetMask,
            cancellationToken);

        await _repository.SaveObservationsAsync(
            observations,
            cancellationToken);

        return await GetRecentGroupsAsync(
            adapter.Id,
            sourceAddress,
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }

    public async Task<IReadOnlyList<IpConflictGroup>>
        GetRecentGroupsAsync(
            string adapterId,
            string sourceAddress,
            TimeSpan lookback,
            CancellationToken cancellationToken = default)
    {
        if (lookback <= TimeSpan.Zero ||
            lookback > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(lookback));
        }

        var sinceUtc = DateTimeOffset.UtcNow.Subtract(lookback);

        var observations =
            await _repository.GetRecentObservationsAsync(
                adapterId,
                sourceAddress,
                sinceUtc,
                cancellationToken);

        return BuildGroups(observations);
    }

    private static IReadOnlyList<IpConflictGroup> BuildGroups(
        IReadOnlyList<IpAddressObservation> observations)
    {
        var groups = observations
            .GroupBy(
                item => item.IpAddress,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var items = group
                    .OrderBy(item => item.ObservedAt)
                    .ToArray();

                var macAddresses = items
                    .Select(item => NormalizeMac(item.MacAddress))
                    .Where(mac => !string.IsNullOrWhiteSpace(mac))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(mac => mac, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                // Chưa có packet capture nên chỉ được phép
                // đưa ra kết luận Observed hoặc Suspected.
                var status = macAddresses.Length > 1
                    ? IpConflictStatus.Suspected
                    : IpConflictStatus.Observed;

                return new IpConflictGroup
                {
                    AdapterId = items[0].AdapterId,
                    AdapterName = items[0].AdapterName,
                    SourceAddress = items[0].SourceAddress,
                    IpAddress = items[0].IpAddress,
                    MacAddresses = macAddresses,
                    Observations = items,
                    Status = status,
                    FirstSeenAt = items[0].ObservedAt,
                    LastSeenAt = items[^1].ObservedAt
                };
            })
            .OrderByDescending(group =>
                group.Status == IpConflictStatus.Suspected)
            .ThenBy(group => group.IpAddress)
            .ToArray();

        return groups;
    }

    private static string NormalizeMac(string value)
    {
        return value
            .Replace("-", ":")
            .Trim()
            .ToUpperInvariant();
    }
}