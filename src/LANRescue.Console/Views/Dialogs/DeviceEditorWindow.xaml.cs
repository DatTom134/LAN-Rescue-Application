using System.Net;
using System.Net.Sockets;
using System.Windows;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.ConsoleApp.Services;
using System.Windows.Input;

namespace LANRescue.ConsoleApp.Views.Dialogs;

public partial class DeviceEditorWindow : Window
{
    private readonly ManagedDevice? _existing;
    private string? _validationMessageResourceKey;

    public DeviceEditorWindow(ManagedDevice? existing = null)
    {
        InitializeComponent();
        _existing = existing;
        DeviceTypeCombo.ItemsSource = Enum.GetValues<DeviceType>();
        AssignmentModeCombo.ItemsSource = Enum.GetValues<IpAssignmentMode>();
        DeviceTypeCombo.SelectionChanged += (_, _) => UpdateTypeSections();
        PopulateFields();
        SourceInitialized += (_, _) => WindowBackdrop.Apply(this, ThemeManager.CurrentTheme, transient: true);
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
        Closed += (_, _) =>
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        };
    }


    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e)
    {
        UpdateDialogTitle();

        // Re-evaluate enum item templates after the resource dictionary changes.
        DeviceTypeCombo.Items.Refresh();
        AssignmentModeCombo.Items.Refresh();

        if (_validationMessageResourceKey is not null)
            ValidationMessageText.Text =
                LocalizationManager.GetString(_validationMessageResourceKey);
    }

    private void UpdateDialogTitle()
    {
        DialogTitleText.Text = _existing is null
            ? LocalizationManager.GetString("DeviceEditor_AddDevice")
            : LocalizationManager.Format("DeviceEditor_EditDevice", _existing.DeviceCode);
    }

    private void DialogHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Trả về kết quả hủy và đóng cửa sổ
        DialogResult = false;
    }

    private void ThemeManager_ThemeChanged(object? sender, EventArgs e) =>
        WindowBackdrop.Apply(this, ThemeManager.CurrentTheme, transient: true);

    public ManagedDevice? ResultDevice { get; private set; }

    private void PopulateFields()
    {
        UpdateDialogTitle();
        DeviceCodeBox.Text = _existing?.DeviceCode ?? string.Empty;
        DeviceNameBox.Text = _existing?.DeviceName ?? string.Empty;
        DeviceTypeCombo.SelectedItem = _existing?.Type ?? DeviceType.Computer;
        DepartmentBox.Text = _existing?.Department ?? string.Empty;
        ManufacturerBox.Text = _existing?.Manufacturer ?? string.Empty;
        ModelBox.Text = _existing?.Model ?? string.Empty;
        SerialNumberBox.Text = _existing?.SerialNumber ?? string.Empty;
        LocationBox.Text = _existing?.Location ?? string.Empty;
        FirmwareVersionBox.Text = _existing?.FirmwareVersion ?? string.Empty;
        NotesBox.Text = _existing?.Notes ?? string.Empty;

        var nic = _existing?.PrimaryNetworkInterface;
        InterfaceNameBox.Text = nic?.Name ?? "Ethernet";
        MacAddressBox.Text = nic?.MacAddress ?? string.Empty;
        var ipv4 = nic?.Addresses.FirstOrDefault(address =>
            IPAddress.TryParse(address.Address, out var parsed) && parsed.AddressFamily == AddressFamily.InterNetwork);
        Ipv4Box.Text = ipv4?.Address ?? string.Empty;
        PrefixLengthBox.Text = ipv4?.PrefixLength?.ToString() ?? "24";
        AssignmentModeCombo.SelectedItem = ipv4?.AssignmentMode ?? IpAssignmentMode.Unknown;
        GatewayBox.Text = nic?.DefaultGateway ?? string.Empty;
        DnsServersBox.Text = nic is null ? string.Empty : string.Join(", ", nic.DnsServers);

        PrinterDriverBox.Text = _existing?.PrinterInfo?.DriverName ?? string.Empty;
        PrinterPortBox.Text = _existing?.PrinterInfo?.PortName ?? string.Empty;
        PrintServerBox.Text = _existing?.PrinterInfo?.PrintServer ?? string.Empty;
        ShareNameBox.Text = _existing?.PrinterInfo?.ShareName ?? string.Empty;
        UpdateTypeSections();
    }

    private void UpdateTypeSections()
    {
        PrinterDetailsPanel.Visibility = DeviceTypeCombo.SelectedItem is DeviceType.Printer
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var code = DeviceCodeBox.Text.Trim();
        var name = DeviceNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            ShowValidation("DeviceEditor_RequiredError");
            return;
        }

        if (DeviceTypeCombo.SelectedItem is not DeviceType deviceType)
        {
            MessageBox.Show(this, LocalizationManager.GetString("DeviceEditor_SelectType"), LocalizationManager.GetString("DeviceEditor_CheckDeviceInformation"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var ipv4Text = Ipv4Box.Text.Trim();
        IPAddress? parsedIpv4 = null;
        if (!string.IsNullOrWhiteSpace(ipv4Text) &&
            (!IPAddress.TryParse(ipv4Text, out parsedIpv4) || parsedIpv4.AddressFamily != AddressFamily.InterNetwork))
        {
            MessageBox.Show(this, LocalizationManager.GetString("DeviceEditor_InvalidIPv4"),
                LocalizationManager.GetString("DeviceEditor_CheckNetworkConfiguration"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int? prefixLength = null;
        if (!string.IsNullOrWhiteSpace(ipv4Text))
        {
            if (!int.TryParse(PrefixLengthBox.Text.Trim(), out var prefix) || prefix is < 0 or > 32)
            {
                MessageBox.Show(this, LocalizationManager.GetString("DeviceEditor_InvalidPrefix"),
                    LocalizationManager.GetString("DeviceEditor_CheckNetworkConfiguration"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            prefixLength = prefix;
        }

        var gateway = GatewayBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(gateway) && !IPAddress.TryParse(gateway, out _))
        {
            MessageBox.Show(this, LocalizationManager.GetString("DeviceEditor_InvalidGateway"), LocalizationManager.GetString("DeviceEditor_CheckNetworkConfiguration"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dnsServers = DnsServersBox.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var invalidDns = dnsServers.FirstOrDefault(value => !IPAddress.TryParse(value, out _));
        if (invalidDns is not null)
        {
            MessageBox.Show(this, LocalizationManager.Format("DeviceEditor_InvalidDns", invalidDns),
                LocalizationManager.GetString("DeviceEditor_CheckNetworkConfiguration"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var oldPrimary = _existing?.PrimaryNetworkInterface;
        var oldIpv4 = oldPrimary?.Addresses.FirstOrDefault(address =>
            IPAddress.TryParse(address.Address, out var parsed) && parsed.AddressFamily == AddressFamily.InterNetwork);
        // Replace only the first IPv4 entry shown by this editor; preserve any other
        // IPv4/IPv6 addresses already recorded on the same interface.
        var addresses = (oldPrimary?.Addresses ?? Array.Empty<NetworkAddress>())
            .Where(address => !ReferenceEquals(address, oldIpv4))
            .ToList();

        if (parsedIpv4 is not null)
        {
            var matchingOldIpv4 = oldIpv4 is not null &&
                string.Equals(oldIpv4.Address, parsedIpv4.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? oldIpv4
                    : null;

            addresses.Add(new NetworkAddress
            {
                Address = parsedIpv4.ToString(),
                PrefixLength = prefixLength,
                AssignmentMode = AssignmentModeCombo.SelectedItem is IpAssignmentMode mode ? mode : IpAssignmentMode.Unknown,
                Health = matchingOldIpv4?.Health ?? IpAddressHealth.Unknown,
                LastObservedAt = matchingOldIpv4?.LastObservedAt
            });
        }

        var hasNetworkData = parsedIpv4 is not null || !string.IsNullOrWhiteSpace(MacAddressBox.Text) ||
                             !string.IsNullOrWhiteSpace(gateway) || dnsServers.Length > 0 || oldPrimary is not null;
        var networkInterfaces = (_existing?.NetworkInterfaces ?? Array.Empty<NetworkInterfaceInfo>())
            .Where(nic => !ReferenceEquals(nic, oldPrimary))
            .ToList();

        if (hasNetworkData)
        {
            networkInterfaces.Insert(0, new NetworkInterfaceInfo
            {
                Name = string.IsNullOrWhiteSpace(InterfaceNameBox.Text) ? "Ethernet" : InterfaceNameBox.Text.Trim(),
                Description = oldPrimary?.Description,
                MacAddress = Normalize(MacAddressBox.Text),
                LinkStatus = oldPrimary?.LinkStatus ?? NetworkLinkStatus.Unknown,
                IsPrimary = true,
                DefaultGateway = Normalize(gateway),
                DnsServers = dnsServers,
                Addresses = addresses
            });
        }

        var printerInfo = deviceType == DeviceType.Printer
            ? new PrinterInfo
            {
                DriverName = Normalize(PrinterDriverBox.Text),
                PortName = Normalize(PrinterPortBox.Text),
                PrintServer = Normalize(PrintServerBox.Text),
                ShareName = Normalize(ShareNameBox.Text),
                IsShared = _existing?.PrinterInfo?.IsShared,
                IsPaused = _existing?.PrinterInfo?.IsPaused,
                OperationalStatus = _existing?.PrinterInfo?.OperationalStatus ?? PrinterOperationalStatus.Unknown
            }
            : null;

        ResultDevice = new ManagedDevice
        {
            Id = _existing?.Id ?? Guid.NewGuid(),
            DeviceCode = code,
            DeviceName = name,
            Type = deviceType,
            Manufacturer = Normalize(ManufacturerBox.Text),
            Model = Normalize(ModelBox.Text),
            SerialNumber = Normalize(SerialNumberBox.Text),
            FirmwareVersion = Normalize(FirmwareVersionBox.Text),
            Department = Normalize(DepartmentBox.Text),
            Location = Normalize(LocationBox.Text),
            Notes = Normalize(NotesBox.Text),
            IsManaged = _existing?.IsManaged ?? true,
            DeactivatedAt = _existing?.DeactivatedAt,
            Reachability = _existing?.Reachability ?? DeviceReachability.Unknown,
            NetworkInterfaces = networkInterfaces,
            ComputerInfo = deviceType is DeviceType.Computer or DeviceType.Server ? _existing?.ComputerInfo : null,
            PrinterInfo = printerInfo,
            Agent = _existing?.Agent,
            ManagementChannels = _existing?.ManagementChannels ?? Array.Empty<ManagementChannelInfo>(),
            LastSeenAt = _existing?.LastSeenAt,
            LastInventoryAt = _existing?.LastInventoryAt,
            CreatedAt = _existing?.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsSampleData = false
        };

        DialogResult = true;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ShowValidation(string resourceKey)
    {
        _validationMessageResourceKey = resourceKey;
        ValidationMessageText.Text = LocalizationManager.GetString(resourceKey);
        ValidationMessageText.Visibility = Visibility.Visible;
    }
}
