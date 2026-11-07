using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Net;

using LANRescue.Application.Interfaces;
using LANRescue.ConsoleApp.Models;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.ConsoleApp.Views;

public partial class DiagnosticsView : UserControl
{
    private readonly IDiagnosticService _diagnosticService;

    private bool _hasLoaded;

    public ObservableCollection<NetworkAdapterDisplayItem> Adapters { get; } = [];

    public ObservableCollection<DiagnosticEvidence> EvidenceResults { get; } = [];

    public ICollectionView GroupedAdapters { get; }

    public DiagnosticsView(IDiagnosticService diagnosticService)
    {
        InitializeComponent();

        GroupedAdapters =
            CollectionViewSource.GetDefaultView(Adapters);

        GroupedAdapters.GroupDescriptions.Add(
            new PropertyGroupDescription(
                nameof(NetworkAdapterDisplayItem.CategoryDisplayName)));

        GroupedAdapters.SortDescriptions.Add(
            new SortDescription(
                nameof(NetworkAdapterDisplayItem.CategoryOrder),
                ListSortDirection.Ascending));

        GroupedAdapters.SortDescriptions.Add(
            new SortDescription(
                nameof(NetworkAdapterDisplayItem.Name),
                ListSortDirection.Ascending));

        _diagnosticService = diagnosticService
            ?? throw new ArgumentNullException(nameof(diagnosticService));

        DataContext = this;

        Loaded += DiagnosticsView_Loaded;
    }

    private async void DiagnosticsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_hasLoaded)
            return;

        _hasLoaded = true;

        await LoadAdaptersAsync();
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadAdaptersAsync();
    }

    private void AdapterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        ClearConnectivityResults();

        if (AdapterComboBox.SelectedItem is not NetworkAdapterDisplayItem adapter)
        {
            SourceIpv4ComboBox.ItemsSource = null;

            GatewayValueText.Text = "Not configured";
            DnsServerValueText.Text = "Not configured";

            RunTestsButton.IsEnabled = false;

            return;
        }

        // Cập nhật danh sách IPv4 thuộc adapter đang chọn.
        SourceIpv4ComboBox.ItemsSource = adapter.Ipv4Addresses;

        // Mặc định chọn IPv4 đầu tiên.
        SourceIpv4ComboBox.SelectedIndex =
            adapter.Ipv4Addresses.Count > 0 ? 0 : -1;

        // Hiển thị Gateway và DNS IPv4.
        GatewayValueText.Text = adapter.FirstIpv4GatewayText;
        DnsServerValueText.Text = adapter.FirstIpv4DnsServerText;

        // Chỉ cho phép chạy nếu adapter có IPv4 và Gateway.
        RunTestsButton.IsEnabled =
            adapter.Ipv4Addresses.Count > 0 &&
            adapter.HasIpv4Gateway;

        UpdateRunButtonState();
    }

    private async Task LoadAdaptersAsync()
    {
        RefreshButton.IsEnabled = false;

        LoadingText.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;
        ErrorCard.Visibility = Visibility.Collapsed;
        AdapterItemsControl.Visibility = Visibility.Collapsed;

        StatusText.Text = "Reading network adapters from Windows...";

        try
        {
            // Đọc danh sách adapter thực tế từ Windows.
            var snapshots = await _diagnosticService.GetNetworkAdaptersAsync();

            Adapters.Clear();

            // Sắp xếp theo tên để dễ tìm kiếm và theo dõi.
            foreach (var snapshot in snapshots.OrderBy(
                adapter => adapter.Name,
                StringComparer.OrdinalIgnoreCase))
            {
                Adapters.Add(new NetworkAdapterDisplayItem(snapshot));
            }

            // Ưu tiên adapter có cả IPv4 và Gateway.
            AdapterComboBox.SelectedItem =
                Adapters.FirstOrDefault(adapter =>
                    adapter.Ipv4Addresses.Count > 0 &&
                    adapter.GatewayAddresses.Count > 0)
                ?? Adapters.FirstOrDefault(adapter =>
                    adapter.Ipv4Addresses.Count > 0);

            // Cập nhật trạng thái giao diện.
            StatusText.Text =
                $"{Adapters.Count} network adapter(s) detected by Windows.";

            LastUpdatedText.Text =
                $"Last updated: {DateTime.Now:HH:mm:ss}";

            if (Adapters.Count > 0)
            {
                AdapterItemsControl.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyState.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Unable to read network adapters.";

            ErrorText.Text =
                $"{ex.GetType().Name}: {ex.Message}";

            ErrorCard.Visibility = Visibility.Visible;
        }
        finally
        {
            LoadingText.Visibility = Visibility.Collapsed;
            RefreshButton.IsEnabled = true;
        }
    }

    private async void RunConnectivityTests_Click(
    object sender,
    RoutedEventArgs e)
    {
        ConnectivityErrorCard.Visibility = Visibility.Collapsed;
        ResultsCard.Visibility = Visibility.Collapsed;

        EvidenceResults.Clear();

        // 1. Kiểm tra adapter.
        if (AdapterComboBox.SelectedItem
            is not NetworkAdapterDisplayItem adapter)
        {
            ShowConnectivityError("Please select a network adapter.");
            return;
        }

        // 2. Kiểm tra Source IPv4.
        if (SourceIpv4ComboBox.SelectedItem is not string sourceAddress)
        {
            ShowConnectivityError(
                "The selected adapter does not have a valid Source IPv4 address.");

            return;
        }

        // 3. Đọc cấu hình TCP.
        string rawTcpHost = TcpTargetTextBox.Text.Trim();
        string rawTcpPort = TcpPortTextBox.Text.Trim();

        bool hasTcpHost = !string.IsNullOrWhiteSpace(rawTcpHost);
        bool hasTcpPort = !string.IsNullOrWhiteSpace(rawTcpPort);

        // Không cho nhập thiếu một trong hai trường TCP.
        if (hasTcpHost != hasTcpPort)
        {
            ShowConnectivityError(
                "Please provide both TCP Target IPv4 and Port, or leave both empty.");

            return;
        }

        string? tcpHost = null;
        int? tcpPort = null;

        if (hasTcpHost)
        {
            // Phiên bản hiện tại chỉ hỗ trợ địa chỉ IPv4.
            if (!IPAddress.TryParse(rawTcpHost, out var targetIp) ||
                targetIp.AddressFamily !=
                    System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ShowConnectivityError(
                    "TCP Target must be a valid IPv4 address.");

                return;
            }

            if (!int.TryParse(rawTcpPort, out int parsedPort) ||
                parsedPort is < 1 or > 65535)
            {
                ShowConnectivityError(
                    "TCP Port must be a number between 1 and 65535.");

                return;
            }

            tcpHost = targetIp.ToString();
            tcpPort = parsedPort;
        }

        // 4. Đọc cấu hình DNS.
        string? dnsHost =
            string.IsNullOrWhiteSpace(DnsHostTextBox.Text)
                ? null
                : DnsHostTextBox.Text.Trim();

        // Nếu người dùng yêu cầu DNS test,
        // adapter phải có DNS Server IPv4.
        if (dnsHost is not null &&
            adapter.FirstIpv4DnsServerText == "Not configured")
        {
            ShowConnectivityError(
                "The selected adapter does not have an IPv4 DNS server.");

            return;
        }

        // 5. Chuẩn bị trạng thái UI.
        RunTestsButton.IsEnabled = false;
        RefreshButton.IsEnabled = false;

        RunTestsButton.Content = "Running...";

        ConnectivityStatusText.Text =
            "Running connectivity tests...";

        ConnectivityInfoText.Text = adapter.Name;

        try
        {
            // 6. Gọi Application Service.
            var session = await _diagnosticService.RunAsync(
                adapter.Id,
                sourceAddress,
                tcpHost,
                tcpPort,
                dnsHost);

            // 7. Đưa các kết quả vào collection.
            foreach (var evidence in session.Evidence)
            {
                EvidenceResults.Add(evidence);
            }

            // 8. Hiển thị trạng thái tổng hợp.
            int successfulTests =
                session.Evidence.Count(item => item.IsSuccessful);

            ConnectivityStatusText.Text =
                session.OverallStatus == DiagnosticStatus.Online
                    ? $"Online · {successfulTests}/{session.Evidence.Count} tests succeeded"
                    : "Unknown · No successful connectivity evidence";

            ConnectivityInfoText.Text =
                $"{session.AdapterName} · " +
                $"{session.StartedAt.ToLocalTime():HH:mm:ss}";

            ResultsCard.Visibility = Visibility.Visible;

            // Không có phản hồi thành công không đồng nghĩa với Offline.
            if (session.OverallStatus == DiagnosticStatus.Unknown)
            {
                ConnectivityStatusText.ToolTip =
                    "No successful response was received. " +
                    "This does not prove that the target is offline.";
            }
        }
        catch (Exception ex)
        {
            ShowConnectivityError(
                $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            UpdateRunButtonState();
            RefreshButton.IsEnabled = true;

            RunTestsButton.Content = "▶  Run Tests";
        }
    }

    private void ShowConnectivityError(string message)
    {
        ConnectivityErrorText.Text = message;

        ConnectivityErrorCard.Visibility = Visibility.Visible;
    }

    private void ClearConnectivityResults()
    {
        EvidenceResults.Clear();

        ResultsCard.Visibility = Visibility.Collapsed;
        ConnectivityErrorCard.Visibility = Visibility.Collapsed;

        ConnectivityStatusText.ToolTip = null;
        ConnectivityInfoText.Text = string.Empty;
    }

    private void UpdateRunButtonState()
    {
        bool canRun =
            AdapterComboBox.SelectedItem
                is NetworkAdapterDisplayItem adapter &&
            SourceIpv4ComboBox.SelectedItem
                is string sourceAddress &&
            adapter.Ipv4Addresses.Contains(
                sourceAddress,
                StringComparer.OrdinalIgnoreCase) &&
            adapter.GatewayAddresses.Count > 0;

        RunTestsButton.IsEnabled = canRun;
    }
}