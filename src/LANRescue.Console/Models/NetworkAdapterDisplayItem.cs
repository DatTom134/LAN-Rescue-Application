using LANRescue.Domain.Enums;
using LANRescue.Domain.Models;

namespace LANRescue.ConsoleApp.Models;

public sealed class NetworkAdapterDisplayItem
{
    private readonly NetworkAdapterSnapshot _source;

    public NetworkAdapterDisplayItem(
        NetworkAdapterSnapshot source)
    {
        _source = source;
    }

    public string Id => _source.Id;

    public string Name => _source.Name;

    public string Description =>
        _source.Description ?? "Description unavailable";

    public string CategoryDisplayName =>
        _source.Category switch
        {
            NetworkInterfaceCategory.Physical =>
                "Physical adapters",

            NetworkInterfaceCategory.Software =>
                "Software / virtual interfaces",

            NetworkInterfaceCategory.Tunnel =>
                "Tunnel interfaces",

            _ =>
                "Other / unclassified interfaces"
        };

    public int CategoryOrder =>
        _source.Category switch
        {
            NetworkInterfaceCategory.Physical => 0,
            NetworkInterfaceCategory.Software => 1,
            NetworkInterfaceCategory.Tunnel => 2,
            _ => 3
        };

    public string StatusText =>
        _source.LinkStatus.ToString();

    public string Ipv4Text =>
        Format(_source.Ipv4Addresses);

    public string SubnetText =>
        Format(_source.Subnets);

    public string GatewayText =>
        Format(_source.Gateways);

    public string DnsText =>
        Format(_source.DnsServers);

    public string MacAddressText =>
        _source.MacAddress ?? "Not available";

    public string VisibilityText =>
        _source.IsHidden == true ? "Hidden" : "Visible";

    private static string Format(
        IReadOnlyList<string> values)
    {
        return values.Count == 0
            ? "Not configured"
            : string.Join(Environment.NewLine, values);
    }

    // Danh sách IPv4 thuộc adapter để người dùng lựa chọn.
    public IReadOnlyList<string> Ipv4Addresses =>
        _source.Ipv4Addresses;

    // Danh sách IPv4 Gateway của adapter.
    public IReadOnlyList<string> GatewayAddresses =>
        _source.Gateways;

    // Gateway đầu tiên được hiển thị trên UI.
    public string FirstIpv4GatewayText =>
    _source.Gateways.FirstOrDefault(address =>
        System.Net.IPAddress.TryParse(address, out var ip) &&
        ip.AddressFamily ==
            System.Net.Sockets.AddressFamily.InterNetwork)
    ?? "Not configured";

    public bool HasIpv4Gateway =>
        FirstIpv4GatewayText != "Not configured";

    // DNS IPv4 đầu tiên được DiagnosticService sử dụng.
    public string FirstIpv4DnsServerText =>
        _source.DnsServers.FirstOrDefault(address =>
            System.Net.IPAddress.TryParse(address, out var ip) &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        ?? "Not configured";
}