using System.Globalization;
using System.Net;
using System.Net.Sockets;

using LANRescue.Application.Models;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.Application.Services;

/// <summary>
/// Validates an IPv4 recovery plan without performing network changes.
/// This class has no network or database side effects.
/// </summary>
public sealed class IpRecoveryPlanValidator
{
    public IpRecoveryValidationResult Validate(
        IpNetworkConfiguration desired,
        IpNetworkConfiguration? current = null)
    {
        ArgumentNullException.ThrowIfNull(desired);

        var issues = new List<IpRecoveryValidationIssue>();

        switch (desired.AssignmentMode)
        {
            case IpAssignmentMode.Static:
                ValidateStaticIpv4(desired, issues);
                break;

            case IpAssignmentMode.Dhcp:
                ValidateDhcp(desired, issues);
                break;

            default:
                AddError(
                    issues,
                    "ADDRESS_MODE_UNKNOWN",
                    nameof(desired.AssignmentMode));
                break;
        }

        ValidateDns(desired, issues);

        if (current is not null &&
            IsSameConfiguration(current, desired))
        {
            AddWarning(
                issues,
                "CONFIGURATION_UNCHANGED",
                "Configuration");
        }

        return new IpRecoveryValidationResult
        {
            Issues = issues.AsReadOnly()
        };
    }

    private static void ValidateStaticIpv4(
        IpNetworkConfiguration config,
        List<IpRecoveryValidationIssue> issues)
    {
        if (!TryParseUsableIpv4(
                config.Ipv4Address,
                out uint addressValue))
        {
            AddError(
                issues,
                "IPV4_INVALID",
                nameof(config.Ipv4Address));

            return;
        }

        if (config.PrefixLength is null ||
            config.PrefixLength < 1 ||
            config.PrefixLength > 30)
        {
            AddError(
                issues,
                "PREFIX_INVALID",
                nameof(config.PrefixLength));

            return;
        }

        uint mask = uint.MaxValue << (32 - config.PrefixLength.Value);
        uint network = addressValue & mask;
        uint broadcast = network | ~mask;

        if (addressValue == network)
        {
            AddError(
                issues,
                "IPV4_NETWORK_ADDRESS",
                nameof(config.Ipv4Address));
        }
        else if (addressValue == broadcast)
        {
            AddError(
                issues,
                "IPV4_BROADCAST_ADDRESS",
                nameof(config.Ipv4Address));
        }

        ValidateGateway(
            config.DefaultGateway,
            addressValue,
            network,
            broadcast,
            mask,
            issues);

        // Syntax validation does not prove that an IP is free.
        AddWarning(
            issues,
            "IPV4_AVAILABILITY_NOT_VERIFIED",
            nameof(config.Ipv4Address));
    }

    private static void ValidateGateway(
        string? gateway,
        uint targetAddress,
        uint network,
        uint broadcast,
        uint mask,
        List<IpRecoveryValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(gateway))
        {
            AddWarning(
                issues,
                "GATEWAY_NOT_SPECIFIED",
                "DefaultGateway");

            return;
        }

        if (!TryParseUsableIpv4(gateway, out uint gatewayValue))
        {
            AddError(
                issues,
                "GATEWAY_INVALID",
                "DefaultGateway");

            return;
        }

        if (gatewayValue == targetAddress)
        {
            AddError(
                issues,
                "GATEWAY_EQUALS_TARGET",
                "DefaultGateway");

            return;
        }

        if ((gatewayValue & mask) != network)
        {
            AddError(
                issues,
                "GATEWAY_OUTSIDE_SUBNET",
                "DefaultGateway");

            return;
        }

        if (gatewayValue == network ||
            gatewayValue == broadcast)
        {
            AddError(
                issues,
                "GATEWAY_NOT_HOST_ADDRESS",
                "DefaultGateway");
        }
    }

    private static void ValidateDhcp(
        IpNetworkConfiguration config,
        List<IpRecoveryValidationIssue> issues)
    {
        if (!string.IsNullOrWhiteSpace(config.Ipv4Address) ||
            config.PrefixLength.HasValue ||
            !string.IsNullOrWhiteSpace(config.DefaultGateway))
        {
            AddError(
                issues,
                "DHCP_STATIC_FIELDS_MUST_BE_EMPTY",
                "NetworkConfiguration");
        }
    }

    private static void ValidateDns(
        IpNetworkConfiguration config,
        List<IpRecoveryValidationIssue> issues)
    {
        switch (config.DnsAssignmentMode)
        {
            case DnsAssignmentMode.Automatic:
                if (config.AssignmentMode != IpAssignmentMode.Dhcp)
                {
                    AddError(
                        issues,
                        "DNS_AUTOMATIC_REQUIRES_DHCP",
                        nameof(config.DnsAssignmentMode));
                }

                if (config.DnsServers.Count > 0)
                {
                    AddError(
                        issues,
                        "DNS_STATIC_LIST_NOT_ALLOWED",
                        nameof(config.DnsServers));
                }

                break;

            case DnsAssignmentMode.KeepExisting:
                if (config.DnsServers.Count > 0)
                {
                    AddError(
                        issues,
                        "DNS_LIST_MUST_BE_EMPTY_WHEN_KEEPING_EXISTING",
                        nameof(config.DnsServers));
                }

                break;

            case DnsAssignmentMode.Static:
                if (config.DnsServers.Count == 0)
                {
                    AddError(
                        issues,
                        "DNS_SERVER_REQUIRED",
                        nameof(config.DnsServers));

                    break;
                }

                var normalized = new HashSet<uint>();

                foreach (var dns in config.DnsServers)
                {
                    if (!TryParseUsableIpv4(dns, out uint dnsValue))
                    {
                        AddError(
                            issues,
                            "DNS_SERVER_INVALID",
                            nameof(config.DnsServers));

                        continue;
                    }

                    if (!normalized.Add(dnsValue))
                    {
                        AddWarning(
                            issues,
                            "DNS_SERVER_DUPLICATE",
                            nameof(config.DnsServers));
                    }
                }

                break;

            default:
                AddError(
                    issues,
                    "DNS_MODE_UNKNOWN",
                    nameof(config.DnsAssignmentMode));
                break;
        }
    }

    /// <summary>
    /// Strict dotted-decimal IPv4 parser.
    /// Rejects shortened or non-canonical IPv4 forms.
    /// </summary>
    private static bool TryParseUsableIpv4(
        string? value,
        out uint numericValue)
    {
        numericValue = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Trim().Split('.');

        if (parts.Length != 4)
            return false;

        var bytes = new byte[4];

        for (int i = 0; i < 4; i++)
        {
            if (!byte.TryParse(
                    parts[i],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out bytes[i]))
            {
                return false;
            }
        }

        // Reserved, loopback, APIPA and multicast/reserved ranges.
        if (bytes[0] == 0 ||
            bytes[0] == 127 ||
            bytes[0] == 169 && bytes[1] == 254 ||
            bytes[0] >= 224)
        {
            return false;
        }

        numericValue =
            ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];

        return true;
    }

    private static bool IsSameConfiguration(
        IpNetworkConfiguration current,
        IpNetworkConfiguration desired)
    {
        return current.AssignmentMode == desired.AssignmentMode &&
               string.Equals(
                   current.Ipv4Address,
                   desired.Ipv4Address,
                   StringComparison.OrdinalIgnoreCase) &&
               current.PrefixLength == desired.PrefixLength &&
               string.Equals(
                   current.DefaultGateway,
                   desired.DefaultGateway,
                   StringComparison.OrdinalIgnoreCase) &&
               current.DnsAssignmentMode == desired.DnsAssignmentMode &&
               current.DnsServers.SequenceEqual(
                   desired.DnsServers,
                   StringComparer.OrdinalIgnoreCase);
    }

    private static void AddError(
        List<IpRecoveryValidationIssue> issues,
        string code,
        string field)
    {
        issues.Add(new IpRecoveryValidationIssue
        {
            Code = code,
            Field = field,
            Severity = IpRecoveryIssueSeverity.Error
        });
    }

    private static void AddWarning(
        List<IpRecoveryValidationIssue> issues,
        string code,
        string field)
    {
        issues.Add(new IpRecoveryValidationIssue
        {
            Code = code,
            Field = field,
            Severity = IpRecoveryIssueSeverity.Warning
        });
    }
}