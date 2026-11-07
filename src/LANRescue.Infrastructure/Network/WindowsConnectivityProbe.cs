using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;

using Microsoft.Win32.SafeHandles;

using LANRescue.Application.Interfaces;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.Infrastructure.Network;

public sealed class WindowsConnectivityProbe : IConnectivityProbe
{
    public Task<DiagnosticEvidence> PingAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress targetAddress,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ValidateIpv4Addresses(sourceAddress, targetAddress, timeout);

        // ICMP native API là lời gọi đồng bộ.
        // Đưa nó lên ThreadPool để không chặn giao diện WPF.
        return Task.Run(
            () => PingCore(
                sessionId,
                adapterId,
                sourceAddress,
                targetAddress,
                timeout,
                cancellationToken),
            CancellationToken.None);
    }

    private static DiagnosticEvidence PingCore(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress targetAddress,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "ICMP source-bound diagnostics requires Windows.");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var icmpHandle = IcmpCreateFile();

            if (icmpHandle.IsInvalid)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Cannot create ICMP handle.");
            }

            byte[] requestData =
                Encoding.ASCII.GetBytes("LANRescue");

            byte[] replyBuffer = new byte[512];

            uint sourceIp = ToIpAddressValue(sourceAddress);
            uint targetIp = ToIpAddressValue(targetAddress);

            uint timeoutMs = (uint)Math.Ceiling(
                timeout.TotalMilliseconds);

            uint replyCount = IcmpSendEcho2Ex(
                icmpHandle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                sourceIp,
                targetIp,
                requestData,
                (ushort)requestData.Length,
                IntPtr.Zero,
                replyBuffer,
                (uint)replyBuffer.Length,
                timeoutMs);

            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                return CreateEvidence(
                    sessionId,
                    adapterId,
                    sourceAddress,
                    DiagnosticProbeType.GatewayPing,
                    DiagnosticEvidenceSource.Icmp,
                    targetAddress.ToString(),
                    DiagnosticProbeOutcome.Cancelled,
                    "Cancellation requested during ICMP operation.",
                    stopwatch.ElapsedMilliseconds);
            }

            if (replyCount == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();

                var outcome = errorCode is 0 or 1460 or 11010
                    ? DiagnosticProbeOutcome.TimedOut
                    : DiagnosticProbeOutcome.Error;

                return CreateEvidence(
                    sessionId,
                    adapterId,
                    sourceAddress,
                    DiagnosticProbeType.GatewayPing,
                    DiagnosticEvidenceSource.Icmp,
                    targetAddress.ToString(),
                    outcome,
                    $"No ICMP reply. Windows error: {errorCode}.",
                    stopwatch.ElapsedMilliseconds);
            }

            var handle = GCHandle.Alloc(
                replyBuffer,
                GCHandleType.Pinned);

            try
            {
                var reply = Marshal.PtrToStructure<IcmpEchoReply>(
                    handle.AddrOfPinnedObject());

                var outcome = reply.Status switch
                {
                    0 => DiagnosticProbeOutcome.Succeeded,
                    11010 => DiagnosticProbeOutcome.TimedOut,
                    _ => DiagnosticProbeOutcome.Failed
                };

                return CreateEvidence(
                    sessionId,
                    adapterId,
                    sourceAddress,
                    DiagnosticProbeType.GatewayPing,
                    DiagnosticEvidenceSource.Icmp,
                    targetAddress.ToString(),
                    outcome,
                    outcome == DiagnosticProbeOutcome.Succeeded
                        ? $"ICMP reply received. RTT: {reply.RoundTripTime} ms."
                        : $"ICMP returned status {reply.Status}.",
                    reply.RoundTripTime);
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Win32Exception ex)
        {
            stopwatch.Stop();

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.GatewayPing,
                DiagnosticEvidenceSource.Icmp,
                targetAddress.ToString(),
                DiagnosticProbeOutcome.Error,
                ex.Message,
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static uint ToIpAddressValue(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("IPv4 address required.");

        // IPAddr của Windows cần dữ liệu 4 byte của IPv4.
        return BitConverter.ToUInt32(address.GetAddressBytes(), 0);
    }

    private sealed class SafeIcmpHandle
        : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeIcmpHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return IcmpCloseHandle(handle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IpOptionInformation
    {
        public byte Ttl;
        public byte Tos;
        public byte Flags;
        public byte OptionsSize;
        public IntPtr OptionsData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IcmpEchoReply
    {
        public uint Address;
        public uint Status;
        public uint RoundTripTime;
        public ushort DataSize;
        public ushort Reserved;
        public IntPtr Data;
        public IpOptionInformation Options;
    }

    [DllImport(
        "iphlpapi.dll",
        SetLastError = true,
        EntryPoint = "IcmpCreateFile")]
    private static extern SafeIcmpHandle IcmpCreateFile();

    [DllImport(
        "iphlpapi.dll",
        SetLastError = true,
        EntryPoint = "IcmpCloseHandle")]
    private static extern bool IcmpCloseHandle(
        IntPtr icmpHandle);

    [DllImport(
        "iphlpapi.dll",
        SetLastError = true,
        EntryPoint = "IcmpSendEcho2Ex")]
    private static extern uint IcmpSendEcho2Ex(
        SafeIcmpHandle icmpHandle,
        IntPtr Event,
        IntPtr ApcRoutine,
        IntPtr ApcContext,
        uint SourceAddress,
        uint DestinationAddress,
        byte[] RequestData,
        ushort RequestSize,
        IntPtr RequestOptions,
        byte[] ReplyBuffer,
        uint ReplySize,
        uint Timeout);

    // Các phương thức dùng chung sẽ được bổ sung ở phần dưới.

    public async Task<DiagnosticEvidence> TestTcpAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress targetAddress,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ValidateIpv4Addresses(sourceAddress, targetAddress, timeout);

        if (port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(port));

        var stopwatch = Stopwatch.StartNew();

        using var timeoutCts = new CancellationTokenSource(timeout);

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

        try
        {
            using var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            // Gắn kết nối với IPv4 nguồn của adapter đang chọn.
            socket.Bind(
                new IPEndPoint(sourceAddress, 0));

            await socket.ConnectAsync(
                new IPEndPoint(targetAddress, port),
                linkedCts.Token);

            stopwatch.Stop();

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.TcpConnection,
                DiagnosticEvidenceSource.Tcp,
                $"{targetAddress}:{port}",
                DiagnosticProbeOutcome.Succeeded,
                "TCP connection established successfully.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            var outcome = cancellationToken.IsCancellationRequested
                ? DiagnosticProbeOutcome.Cancelled
                : DiagnosticProbeOutcome.TimedOut;

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.TcpConnection,
                DiagnosticEvidenceSource.Tcp,
                $"{targetAddress}:{port}",
                outcome,
                outcome == DiagnosticProbeOutcome.TimedOut
                    ? "TCP connection timed out."
                    : "TCP operation was cancelled.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();

            var outcome = ex.SocketErrorCode switch
            {
                SocketError.ConnectionRefused =>
                    DiagnosticProbeOutcome.ConnectionRefused,

                SocketError.TimedOut =>
                    DiagnosticProbeOutcome.TimedOut,

                _ =>
                    DiagnosticProbeOutcome.Failed
            };

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.TcpConnection,
                DiagnosticEvidenceSource.Tcp,
                $"{targetAddress}:{port}",
                outcome,
                $"{ex.SocketErrorCode}: {ex.Message}",
                stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<DiagnosticEvidence> ResolveDnsAsync(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        IPAddress dnsServer,
        string host,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ValidateIpv4Addresses(sourceAddress, dnsServer, timeout);

        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("DNS hostname is required.", nameof(host));

        ushort transactionId = (ushort)RandomNumberGenerator.GetInt32(0, 65536);

        byte[] query;

        try
        {
            query = BuildDnsQuery(host, transactionId);
        }
        catch (ArgumentException ex)
        {
            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.DnsResolution,
                DiagnosticEvidenceSource.Dns,
                host,
                DiagnosticProbeOutcome.Failed,
                ex.Message,
                null,
                dnsServer.ToString());
        }

        var stopwatch = Stopwatch.StartNew();

        using var timeoutCts = new CancellationTokenSource(timeout);

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutCts.Token);

        try
        {
            using var socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Dgram,
                ProtocolType.Udp);

            // Gắn DNS query với adapter đang được chọn.
            socket.Bind(new IPEndPoint(sourceAddress, 0));

            var dnsEndpoint = new IPEndPoint(dnsServer, 53);

            await socket.SendToAsync(
                query.AsMemory(),
                SocketFlags.None,
                dnsEndpoint,
                linkedCts.Token);

            byte[] responseBuffer = new byte[4096];

            var received = await socket.ReceiveFromAsync(
                responseBuffer.AsMemory(),
                SocketFlags.None,
                new IPEndPoint(IPAddress.Any, 0),
                linkedCts.Token);

            stopwatch.Stop();

            byte[] response = responseBuffer
                .Take(received.ReceivedBytes)
                .ToArray();

            var parsed = DnsMessageParser.Parse(
                response,
                transactionId);

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.DnsResolution,
                DiagnosticEvidenceSource.Dns,
                host,
                parsed.Outcome,
                parsed.Details,
                stopwatch.ElapsedMilliseconds,
                dnsServer.ToString());
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();

            var outcome = cancellationToken.IsCancellationRequested
                ? DiagnosticProbeOutcome.Cancelled
                : DiagnosticProbeOutcome.TimedOut;

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.DnsResolution,
                DiagnosticEvidenceSource.Dns,
                host,
                outcome,
                outcome == DiagnosticProbeOutcome.TimedOut
                    ? "DNS query timed out."
                    : "DNS query was cancelled.",
                stopwatch.ElapsedMilliseconds,
                dnsServer.ToString());
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.DnsResolution,
                DiagnosticEvidenceSource.Dns,
                host,
                DiagnosticProbeOutcome.Failed,
                $"{ex.SocketErrorCode}: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                dnsServer.ToString());
        }
        catch (InvalidDataException ex)
        {
            stopwatch.Stop();

            return CreateEvidence(
                sessionId,
                adapterId,
                sourceAddress,
                DiagnosticProbeType.DnsResolution,
                DiagnosticEvidenceSource.Dns,
                host,
                DiagnosticProbeOutcome.Error,
                ex.Message,
                stopwatch.ElapsedMilliseconds,
                dnsServer.ToString());
        }
    }

    private static byte[] BuildDnsQuery(
    string host,
    ushort transactionId)
    {
        string asciiHost = new IdnMapping()
            .GetAscii(host.Trim().TrimEnd('.'));

        string[] labels = asciiHost.Split('.');

        using var stream = new MemoryStream();

        WriteUInt16(stream, transactionId);

        // Flags: Standard query, recursion desired.
        WriteUInt16(stream, 0x0100);

        WriteUInt16(stream, 1); // QDCOUNT
        WriteUInt16(stream, 0); // ANCOUNT
        WriteUInt16(stream, 0); // NSCOUNT
        WriteUInt16(stream, 0); // ARCOUNT

        foreach (string label in labels)
        {
            byte[] labelBytes = Encoding.ASCII.GetBytes(label);

            if (labelBytes.Length is < 1 or > 63)
                throw new ArgumentException("Invalid DNS label length.");

            stream.WriteByte((byte)labelBytes.Length);
            stream.Write(labelBytes);
        }

        stream.WriteByte(0);

        WriteUInt16(stream, 1); // QTYPE A
        WriteUInt16(stream, 1); // QCLASS IN

        return stream.ToArray();
    }

    private static void WriteUInt16(
        Stream stream,
        ushort value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value & 0xFF));
    }

    private static void ValidateIpv4Addresses(
    IPAddress sourceAddress,
    IPAddress targetAddress,
    TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(sourceAddress);
        ArgumentNullException.ThrowIfNull(targetAddress);

        if (sourceAddress.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Source IPv4 address is required.");

        if (targetAddress.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Target IPv4 address is required.");

        if (timeout <= TimeSpan.Zero ||
            timeout > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                "Timeout must be between 1 ms and 30 seconds.");
        }
    }

    private static DiagnosticEvidence CreateEvidence(
        Guid sessionId,
        string adapterId,
        IPAddress sourceAddress,
        DiagnosticProbeType probeType,
        DiagnosticEvidenceSource source,
        string target,
        DiagnosticProbeOutcome outcome,
        string details,
        long? durationMs,
        string? resolverAddress = null)
    {
        return new DiagnosticEvidence
        {
            SessionId = sessionId,
            AdapterId = adapterId,
            SourceAddress = sourceAddress.ToString(),
            ProbeType = probeType,
            Source = source,
            Target = target,
            ResolverAddress = resolverAddress,
            Outcome = outcome,
            Details = details,
            DurationMs = durationMs,
            ObservedAt = DateTimeOffset.UtcNow
        };
    }
}