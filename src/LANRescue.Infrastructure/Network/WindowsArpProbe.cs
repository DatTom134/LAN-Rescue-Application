using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;

namespace LANRescue.Infrastructure.Network;

public sealed class WindowsArpProbe : IIpConflictProbe
{
    private const int MaxParallelRequests = 8;
    private const ulong MaxSubnetHosts = 1024;

    public async Task<IReadOnlyList<IpAddressObservation>> ScanSubnetAsync(
        string adapterId,
        string adapterName,
        IPAddress sourceAddress,
        IPAddress subnetMask,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Windows ARP scanning requires Windows.");
        }

        ValidateIpv4(sourceAddress, nameof(sourceAddress));
        ValidateIpv4(subnetMask, nameof(subnetMask));

        var targets = GetHostAddresses(sourceAddress, subnetMask);

        var results = new ConcurrentBag<IpAddressObservation>();

        using var semaphore = new SemaphoreSlim(MaxParallelRequests);

        var tasks = targets.Select(async target =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // SendARP là lời gọi native đồng bộ.
                // Đưa nó lên ThreadPool để tránh chặn giao diện WPF.
                var observation = await Task.Run(
                    () => ProbeOne(
                        adapterId,
                        adapterName,
                        sourceAddress,
                        target),
                    CancellationToken.None);

                if (observation is not null)
                {
                    results.Add(observation);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return results
            .OrderBy(item => item.IpAddress, StringComparer.Ordinal)
            .ToArray();
    }

    private static IpAddressObservation? ProbeOne(
        string adapterId,
        string adapterName,
        IPAddress sourceAddress,
        IPAddress targetAddress)
    {
        byte[] macBuffer = new byte[8];
        uint macLength = (uint)macBuffer.Length;

        uint result = SendARP(
            ToNativeAddress(targetAddress),
            ToNativeAddress(sourceAddress),
            macBuffer,
            ref macLength);

        // Không có MAC hợp lệ: không tạo observation.
        if (result != 0 || macLength < 6 || macLength > macBuffer.Length)
        {
            return null;
        }

        var macBytes = macBuffer
            .Take((int)macLength)
            .ToArray();

        // Loại MAC rỗng, broadcast và multicast.
        if (macBytes.All(value => value == 0) ||
            macBytes.All(value => value == 0xFF) ||
            (macBytes[0] & 0x01) != 0)
        {
            return null;
        }

        string macAddress = string.Join(
            ":",
            macBytes.Select(value => value.ToString("X2")));

        return new IpAddressObservation
        {
            AdapterId = adapterId,
            AdapterName = adapterName,
            SourceAddress = sourceAddress.ToString(),
            IpAddress = targetAddress.ToString(),
            MacAddress = macAddress,
            ProbeMethod = "Windows SendARP",
            ObservedAt = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyList<IPAddress> GetHostAddresses(
        IPAddress sourceAddress,
        IPAddress subnetMask)
    {
        uint addressValue = ToBigEndianValue(sourceAddress);
        uint maskValue = ToBigEndianValue(subnetMask);

        if (!IsContiguousMask(maskValue))
        {
            throw new ArgumentException(
                "Subnet mask is not valid.");
        }

        uint networkAddress = addressValue & maskValue;
        uint broadcastAddress = networkAddress | ~maskValue;

        if ((ulong)broadcastAddress <= (ulong)networkAddress + 1)
        {
            throw new ArgumentException(
                "The selected subnet has no usable host addresses.");
        }

        ulong hostCount =
            (ulong)broadcastAddress - networkAddress - 1;

        // Không để người dùng vô tình quét những subnet quá lớn.
        if (hostCount > MaxSubnetHosts)
        {
            throw new InvalidOperationException(
                $"Subnet contains {hostCount} hosts. " +
                $"The current scan limit is {MaxSubnetHosts} hosts.");
        }

        var targets = new List<IPAddress>((int)hostCount);

        for (ulong value = (ulong)networkAddress + 1;
             value < broadcastAddress;
             value++)
        {
            uint current = (uint)value;

            // Không truy vấn chính máy đang chạy LAN Rescue.
            if (current == addressValue)
                continue;

            targets.Add(FromBigEndianValue(current));
        }

        return targets;
    }

    private static bool IsContiguousMask(uint mask)
    {
        bool zeroFound = false;

        for (int bit = 31; bit >= 0; bit--)
        {
            bool isOne = ((mask >> bit) & 1) != 0;

            if (!isOne)
            {
                zeroFound = true;
            }
            else if (zeroFound)
            {
                return false;
            }
        }

        return true;
    }

    private static uint ToBigEndianValue(IPAddress address)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(
            address.GetAddressBytes());
    }

    private static IPAddress FromBigEndianValue(uint value)
    {
        byte[] bytes = new byte[4];

        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);

        return new IPAddress(bytes);
    }

    private static uint ToNativeAddress(IPAddress address)
    {
        // Cùng cách chuyển đổi IPAddr native đang dùng
        // trong WindowsConnectivityProbe.cs của dự án.
        return BitConverter.ToUInt32(
            address.GetAddressBytes(),
            0);
    }

    private static void ValidateIpv4(
        IPAddress address,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException(
                "IPv4 address required.",
                parameterName);
        }
    }

    [DllImport(
        "iphlpapi.dll",
        ExactSpelling = true)]
    private static extern uint SendARP(
        uint DestIP,
        uint SrcIP,
        [Out] byte[] pMacAddr,
        ref uint PhyAddrLen);
}