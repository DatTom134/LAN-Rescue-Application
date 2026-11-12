using System.Windows;
using System.Windows.Controls;

using LANRescue.Application.Services;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.Application.Models;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Views;

public partial class IpRecoveryView : UserControl
{
    private readonly IpRecoveryService? _service;

    private bool _isCompact;
    private bool _hasLoaded;
    private IpNetworkConfiguration? _currentConfiguration;
    private IpRecoveryValidationResult? _lastValidationResult;

    // Giữ constructor không tham số để hỗ trợ XAML Designer.
    public IpRecoveryView()
    {
        InitializeComponent();
    }

    // Constructor dùng khi MainWindow truyền service.
    public IpRecoveryView(IpRecoveryService service)
        : this()
    {
        _service = service
            ?? throw new ArgumentNullException(nameof(service));

        Loaded += IpRecoveryView_Loaded;
    }

    private async void IpRecoveryView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_hasLoaded || _service is null)
            return;

        _hasLoaded = true;

        try
        {
            await _service.InitializeAsync();

            // Giai đoạn hiện tại mới chuẩn bị nguồn dữ liệu.
            // Việc hiển thị danh sách Target Device sẽ được
            // triển khai trong phần tiếp theo của Giai đoạn 1.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[IP Recovery] Initialization failed: {ex}");
        }
    }

    private void RecoveryCardsGrid_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 920;

        if (compact == _isCompact)
            return;

        _isCompact = compact;

        if (compact)
        {
            RecoveryCardsGrid.ColumnDefinitions[0].Width =
                new GridLength(1, GridUnitType.Star);

            RecoveryCardsGrid.ColumnDefinitions[1].Width =
                new GridLength(0, GridUnitType.Pixel);

            RecoveryCardsGrid.ColumnDefinitions[2].Width =
                new GridLength(0, GridUnitType.Pixel);

            RecoveryCardsGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            RecoveryCardsGrid.RowDefinitions[1].Height =
                GridLength.Auto;

            Grid.SetRow(RecoveryConfigCard, 1);
            Grid.SetColumn(RecoveryConfigCard, 0);

            RecoveryTargetCard.Margin =
                new Thickness(0, 0, 0, 12);

            RecoveryConfigCard.Margin =
                new Thickness(0);
        }
        else
        {
            RecoveryCardsGrid.ColumnDefinitions[0].Width =
                new GridLength(1, GridUnitType.Star);

            RecoveryCardsGrid.ColumnDefinitions[1].Width =
                new GridLength(16, GridUnitType.Pixel);

            RecoveryCardsGrid.ColumnDefinitions[2].Width =
                new GridLength(1, GridUnitType.Star);

            RecoveryCardsGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            RecoveryCardsGrid.RowDefinitions[1].Height =
                new GridLength(0, GridUnitType.Pixel);

            Grid.SetRow(RecoveryConfigCard, 0);
            Grid.SetColumn(RecoveryConfigCard, 2);

            RecoveryTargetCard.Margin =
                new Thickness(0, 0, 0, 16);

            RecoveryConfigCard.Margin =
                new Thickness(0, 0, 0, 16);
        }
    }

    private IpNetworkConfiguration BuildDesiredConfiguration()
    {
        var assignmentMode =
            (AddressModeBox.SelectedItem as ComboBoxItem)?
                .Tag?.ToString() switch
            {
                "Static" => IpAssignmentMode.Static,
                "Dhcp" => IpAssignmentMode.Dhcp,
                _ => IpAssignmentMode.Unknown
            };

        var dnsMode =
            (DnsModeBox.SelectedItem as ComboBoxItem)?
                .Tag?.ToString() switch
            {
                "Automatic" => DnsAssignmentMode.Automatic,
                "Static" => DnsAssignmentMode.Static,
                "KeepExisting" => DnsAssignmentMode.KeepExisting,
                _ => DnsAssignmentMode.Unknown
            };

        var dnsServers = DnsServersTextBox.Text
            .Split(
                [';', ',', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        int? prefix = null;

        if (int.TryParse(PrefixLengthTextBox.Text, out int parsedPrefix))
            prefix = parsedPrefix;

        return new IpNetworkConfiguration
        {
            AssignmentMode = assignmentMode,

            Ipv4Address = string.IsNullOrWhiteSpace(NewIpv4TextBox.Text)
                ? null
                : NewIpv4TextBox.Text.Trim(),

            PrefixLength = prefix,

            DefaultGateway = string.IsNullOrWhiteSpace(GatewayTextBox.Text)
                ? null
                : GatewayTextBox.Text.Trim(),

            DnsAssignmentMode = dnsMode,

            DnsServers = dnsServers
        };
    }

    private void ValidatePlan_Click(
    object sender,
    RoutedEventArgs e)
    {
        try
        {
            var desired = BuildDesiredConfiguration();

            _lastValidationResult =
                _service.ValidatePlan(
                    desired,
                    _currentConfiguration);

            ValidationIssuesList.ItemsSource =
                _lastValidationResult.Issues
                    .Select(issue =>
                        LocalizationManager.GetString(issue.Code))
                    .ToArray();

            if (_lastValidationResult.IsValid)
            {
                ValidationSummaryText.Text =
                    LocalizationManager.Format(
                        "IpRecovery_ValidationPassed",
                        _lastValidationResult.Warnings.Count);
            }
            else
            {
                ValidationSummaryText.Text =
                    LocalizationManager.Format(
                        "IpRecovery_ValidationFailed",
                        _lastValidationResult.Errors.Count,
                        _lastValidationResult.Warnings.Count);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[IP Recovery] Validation failed: {ex}");

            ValidationSummaryText.Text =
                LocalizationManager.GetString(
                    "IpRecovery_ValidationInternalError");
        }
    }

    private void AddressModeBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        bool isStatic =
            (AddressModeBox.SelectedItem as ComboBoxItem)?
                .Tag?.ToString() == "Static";

        NewIpv4TextBox.IsEnabled = isStatic;
        PrefixLengthTextBox.IsEnabled = isStatic;
        GatewayTextBox.IsEnabled = isStatic;

        if (!isStatic)
        {
            NewIpv4TextBox.Clear();
            PrefixLengthTextBox.Clear();
            GatewayTextBox.Clear();

            DnsModeBox.SelectedValue = "Automatic";
        }
        else
        {
            if (DnsModeBox.SelectedIndex == 0)
                DnsModeBox.SelectedValue = "KeepExisting";
        }

        ValidationIssuesList.ItemsSource = null;

        ValidationSummaryText.Text =
            LocalizationManager.GetString(
                "IpRecovery_NotValidated");

        _lastValidationResult = null;
    }
}
