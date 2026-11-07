using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Enums;
using LANRescue.Domain.Models;

namespace LANRescue.Infrastructure.Network;

public sealed class WindowsNetworkAdapterProvider
    : INetworkAdapterProvider
{
    public Task<IReadOnlyList<NetworkAdapterSnapshot>> GetAdaptersAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<NetworkAdapterSnapshot>>(
            () => ReadAdapters(cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<NetworkAdapterSnapshot> ReadAdapters(
        CancellationToken cancellationToken
    )
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Windows Network Adapter Provider only supports Windows.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        var metadataByGuid = WindowsAdapterMetadataReader.Read();

        var adapters = new List<NetworkAdapterSnapshot>();

        foreach (var networkInterface in networkInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (networkInterface.NetworkInterfaceType
                == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            var snapshot = CreateSnapshot(
                networkInterface,
                metadataByGuid);

            adapters.Add(snapshot);
        }

        return adapters.AsReadOnly();
    }

    private static NetworkAdapterSnapshot CreateSnapshot(
        NetworkInterface networkInterface,
        IReadOnlyDictionary<string, WindowsAdapterMetadata> metadataByGuid
    )
    {
        var ipProperties = networkInterface.GetIPProperties();

        // Lấy tất cả địa chỉ IPv4 được gán cho adapter
        var ipv4Addresses = ipProperties.UnicastAddresses
            .Where(address =>
                address.Address.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();

        // Lấy Subnet Mask tương ứng với từng địa chỉ IPv4.
        var subnets = ipv4Addresses
            .Select(address => address.IPv4Mask.ToString())
            .ToArray();

        // Lấy Gateway IPv4
        var gateways = ipProperties.GatewayAddresses
            .Select(gateway => gateway.Address)
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.Any.Equals(address))
            .Select(address => address.ToString())
            .Distinct()
            .ToArray();

        // Lấy DNS Server được cấu hình trên adapter.
        // Có thể bao gồm cả địa chỉ IPv4 và IPv6.
        var dnsServers = ipProperties.DnsAddresses
            .Where(address =>
                address.AddressFamily == AddressFamily.InterNetwork
                || address.AddressFamily == AddressFamily.InterNetworkV6)
            .Select(address => address.ToString())
            .Distinct()
            .ToArray();

        // Lấy MAC Address.
        var macBytes = networkInterface
            .GetPhysicalAddress()
            .GetAddressBytes();

        var macAddress = macBytes.Length == 0
            ? null
            : string.Join(
                ":",
                macBytes.Select(value => value.ToString("X2"))
            );

        var operationalStatus = networkInterface.OperationalStatus;

        var normalizedId = WindowsAdapterMetadataReader.NormalizeGuid(networkInterface.Id);

        metadataByGuid.TryGetValue(normalizedId, out var metadata);

        return new NetworkAdapterSnapshot
        {
            Id = networkInterface.Id,
            Name = networkInterface.Name,
            Description = networkInterface.Description,
            InterfaceType = networkInterface.NetworkInterfaceType.ToString(),
            LinkStatus = MapLinkStatus(operationalStatus),
            MacAddress = macAddress,
            Ipv4Addresses = ipv4Addresses
                .Select(address => address.Address.ToString())
                .ToArray(),
            Subnets = subnets,
            Gateways = gateways,
            DnsServers = dnsServers,
            IsOperational = operationalStatus == OperationalStatus.Up,
            ObservedAt = DateTimeOffset.UtcNow,
            Category = Classify(
                networkInterface.NetworkInterfaceType,
                metadata?.HardwareInterface),
            IsHardwareInterface = metadata?.HardwareInterface,
            IsHidden = metadata?.IsHidden,
        };
    }

    private static NetworkLinkStatus MapLinkStatus(
        OperationalStatus status
    )
    {
        return status switch
        {
            OperationalStatus.Up => NetworkLinkStatus.Up,

            OperationalStatus.Down
                or OperationalStatus.NotPresent
                or OperationalStatus.LowerLayerDown =>
                NetworkLinkStatus.Down,

            _ => NetworkLinkStatus.Unknown
        };
    }

    private static NetworkInterfaceCategory Classify(
        NetworkInterfaceType interfaceType,
        bool? hardwareInterface)
    {
        if (interfaceType == NetworkInterfaceType.Tunnel)
            return NetworkInterfaceCategory.Tunnel;

        if (hardwareInterface == true)
            return NetworkInterfaceCategory.Physical;

        if (hardwareInterface == false)
            return NetworkInterfaceCategory.Software;

        return NetworkInterfaceCategory.Other;
    }
}