using System.Net;
using LANRescue.Domain.Enums;

namespace LANRescue.Infrastructure.Network;

internal sealed record DnsParseResult(
    DiagnosticProbeOutcome Outcome,
    IReadOnlyList<IPAddress> Addresses,
    string Details);

internal static class DnsMessageParser
{
    public static DnsParseResult Parse(
        byte[] response,
        ushort expectedId)
    {
        if (response.Length < 12)
            throw new InvalidDataException("DNS response header is incomplete.");

        ushort id = ReadUInt16(response, 0);

        if (id != expectedId)
            throw new InvalidDataException("DNS transaction ID mismatch.");

        ushort flags = ReadUInt16(response, 2);

        bool isResponse = (flags & 0x8000) != 0;
        bool truncated = (flags & 0x0200) != 0;

        if (!isResponse)
            throw new InvalidDataException("Received packet is not a DNS response.");

        if (truncated)
        {
            return new DnsParseResult(
                DiagnosticProbeOutcome.Failed,
                Array.Empty<IPAddress>(),
                "DNS response was truncated. TCP fallback is not implemented yet.");
        }

        int responseCode = flags & 0x000F;

        if (responseCode == 3)
        {
            return new DnsParseResult(
                DiagnosticProbeOutcome.NameNotFound,
                Array.Empty<IPAddress>(),
                "DNS server returned NXDOMAIN.");
        }

        if (responseCode != 0)
        {
            return new DnsParseResult(
                DiagnosticProbeOutcome.Failed,
                Array.Empty<IPAddress>(),
                $"DNS server returned response code {responseCode}.");
        }

        ushort questionCount = ReadUInt16(response, 4);
        ushort answerCount = ReadUInt16(response, 6);

        int offset = 12;

        // Bỏ qua Question section.
        for (int i = 0; i < questionCount; i++)
        {
            offset = SkipName(response, offset);

            EnsureAvailable(response, offset, 4);

            offset += 4;
        }

        var addresses = new List<IPAddress>();

        // Đọc Answer section.
        for (int i = 0; i < answerCount; i++)
        {
            offset = SkipName(response, offset);

            EnsureAvailable(response, offset, 10);

            ushort type = ReadUInt16(response, offset);
            ushort recordClass = ReadUInt16(response, offset + 2);

            ushort dataLength = ReadUInt16(response, offset + 8);

            offset += 10;

            EnsureAvailable(response, offset, dataLength);

            // A record: type = 1, class = IN = 1.
            if (type == 1 && recordClass == 1 && dataLength == 4)
            {
                byte[] addressBytes = response
                    .Skip(offset)
                    .Take(4)
                    .ToArray();

                addresses.Add(new IPAddress(addressBytes));
            }

            offset += dataLength;
        }

        if (addresses.Count == 0)
        {
            return new DnsParseResult(
                DiagnosticProbeOutcome.NoRecords,
                addresses,
                "DNS response was received, but no IPv4 A record was returned.");
        }

        return new DnsParseResult(
            DiagnosticProbeOutcome.Succeeded,
            addresses,
            $"Resolved A records: {string.Join(", ", addresses)}");
    }

    private static int SkipName(
        byte[] message,
        int offset)
    {
        int current = offset;
        int returnOffset = -1;
        int jumpCount = 0;

        while (true)
        {
            EnsureAvailable(message, current, 1);

            byte length = message[current++];

            // DNS compression pointer.
            if ((length & 0xC0) == 0xC0)
            {
                EnsureAvailable(message, current, 1);

                int pointer =
                    ((length & 0x3F) << 8) |
                    message[current++];

                if (returnOffset < 0)
                    returnOffset = current;

                current = pointer;

                if (++jumpCount > 32)
                    throw new InvalidDataException("Too many DNS compression pointers.");

                continue;
            }

            if ((length & 0xC0) != 0)
                throw new InvalidDataException("Invalid DNS label.");

            if (length == 0)
                return returnOffset >= 0 ? returnOffset : current;

            EnsureAvailable(message, current, length);

            current += length;
        }
    }

    private static ushort ReadUInt16(
        byte[] data,
        int offset)
    {
        EnsureAvailable(data, offset, 2);

        return (ushort)(
            (data[offset] << 8) |
            data[offset + 1]);
    }

    private static void EnsureAvailable(
        byte[] data,
        int offset,
        int count)
    {
        if (offset < 0 ||
            count < 0 ||
            offset > data.Length - count)
        {
            throw new InvalidDataException("DNS packet contains an invalid offset.");
        }
    }
}