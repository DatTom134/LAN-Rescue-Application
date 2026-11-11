using System.ComponentModel;
using System.Runtime.CompilerServices;
using LANRescue.ConsoleApp.Services;
using LANRescue.Domain.Enums;
using LANRescue.Domain.Models;

namespace LANRescue.ConsoleApp.Models;

public sealed class NetworkAdapterDisplayItem : INotifyPropertyChanged
{
    private readonly NetworkAdapterSnapshot _source;

    public NetworkAdapterDisplayItem(NetworkAdapterSnapshot source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id => _source.Id;
    public string Name => _source.Name;
    public NetworkInterfaceCategory Category => _source.Category;
    public bool IsUp => _source.LinkStatus == NetworkLinkStatus.Up;

    public string Description => _source.Description ??
        LocalizationManager.GetString("Diagnostics_DescriptionUnavailable");

    public string CategoryDisplayName => _source.Category switch
    {
        NetworkInterfaceCategory.Physical => LocalizationManager.GetString("Diagnostics_CategoryPhysical"),
        NetworkInterfaceCategory.Software => LocalizationManager.GetString("Diagnostics_CategorySoftware"),
        NetworkInterfaceCategory.Tunnel => LocalizationManager.GetString("Diagnostics_CategoryTunnel"),
        _ => LocalizationManager.GetString("Diagnostics_CategoryOther")
    };

    public int CategoryOrder => _source.Category switch
    {
        NetworkInterfaceCategory.Physical => 0,
        NetworkInterfaceCategory.Software => 1,
        NetworkInterfaceCategory.Tunnel => 2,
        _ => 3
    };

    public string StatusText => _source.LinkStatus switch
    {
        NetworkLinkStatus.Up => LocalizationManager.GetString("Diagnostics_StatusUp"),
        NetworkLinkStatus.Down => LocalizationManager.GetString("Diagnostics_StatusDown"),
        NetworkLinkStatus.Disabled => LocalizationManager.GetString("Diagnostics_StatusDisabled"),
        _ => LocalizationManager.GetString("Diagnostics_StatusUnknown")
    };

    public string Ipv4Text => Format(_source.Ipv4Addresses);
    public string SubnetText => Format(_source.Subnets);
    public string GatewayText => Format(_source.Gateways);
    public string DnsText => Format(_source.DnsServers);
    public string MacAddressText => _source.MacAddress ?? LocalizationManager.GetString("Diagnostics_NotAvailable");
    public string VisibilityText => _source.IsHidden == true
        ? LocalizationManager.GetString("Diagnostics_Hidden")
        : LocalizationManager.GetString("Diagnostics_Visible");

    public IReadOnlyList<string> Ipv4Addresses => _source.Ipv4Addresses;
    public IReadOnlyList<string> GatewayAddresses => _source.Gateways;

    public string FirstIpv4GatewayText => FirstIpv4(_source.Gateways) ??
        LocalizationManager.GetString("Diagnostics_NotConfigured");

    public bool HasIpv4Gateway => FirstIpv4(_source.Gateways) is not null;

    public string FirstIpv4DnsServerText => FirstIpv4(_source.DnsServers) ??
        LocalizationManager.GetString("Diagnostics_NotConfigured");

    public bool HasIpv4DnsServer => FirstIpv4(_source.DnsServers) is not null;

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(CategoryDisplayName));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(Ipv4Text));
        OnPropertyChanged(nameof(SubnetText));
        OnPropertyChanged(nameof(GatewayText));
        OnPropertyChanged(nameof(DnsText));
        OnPropertyChanged(nameof(MacAddressText));
        OnPropertyChanged(nameof(VisibilityText));
        OnPropertyChanged(nameof(FirstIpv4GatewayText));
        OnPropertyChanged(nameof(FirstIpv4DnsServerText));
    }

    private string Format(IReadOnlyList<string> values) =>
        values.Count == 0
            ? LocalizationManager.GetString("Diagnostics_NotConfigured")
            : string.Join(Environment.NewLine, values);

    private static string? FirstIpv4(IReadOnlyList<string> values) =>
        values.FirstOrDefault(address =>
            System.Net.IPAddress.TryParse(address, out var ip) &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
