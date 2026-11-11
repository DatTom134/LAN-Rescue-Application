using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.ComponentModel;
using System.Windows.Data;

using LANRescue.Application.Interfaces;
using LANRescue.ConsoleApp.Models;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Views;

public partial class DiagnosticsView : UserControl
{
    private readonly IDiagnosticService _diagnosticService;

    private bool _hasLoaded;

    private bool _isCompactConnectivityLayout;

    private bool _isBusy;

    // Ngăn các SelectionChanged xử lý dữ liệu trung gian
    // trong lúc thay toàn bộ danh sách adapter.
    private bool _isUpdatingAdapters;

    private bool _isCompactNetworkInterfacesLayout;

    private bool _hasSuccessfulAdapterLoad;

    private string _statusResourceKey = "Diagnostics_WaitingAdapters";
    private object[] _statusArguments = Array.Empty<object>();
    private string _lastUpdatedResourceKey = "Diagnostics_NotRefreshed";
    private DateTime? _lastUpdatedAt;
    private string? _connectivityStatusResourceKey;
    private object[] _connectivityStatusArguments = Array.Empty<object>();
    private string? _connectivityTooltipResourceKey;
    private string? _connectivityErrorResourceKey;

    public ObservableCollection<NetworkAdapterDisplayItem> Adapters { get; } = [];

    public ObservableCollection<DiagnosticEvidence> EvidenceResults { get; } = [];

    public ICollectionView AdapterListView { get; }

    public DiagnosticsView(IDiagnosticService diagnosticService)
    {
        InitializeComponent();

        _diagnosticService = diagnosticService
            ?? throw new ArgumentNullException(nameof(diagnosticService));

        // Tạo một CollectionView riêng cho Adapter List.
        // Không sử dụng DefaultView vì Adapters cũng được binding vào ComboBox.
        var adapterListView = new ListCollectionView(Adapters);

        // Nhóm adapter theo category.
        adapterListView.GroupDescriptions.Add(
            new PropertyGroupDescription(
                nameof(NetworkAdapterDisplayItem.CategoryDisplayName)));

        // Sắp xếp category trước, sau đó sắp xếp tên adapter.
        adapterListView.SortDescriptions.Add(
            new SortDescription(
                nameof(NetworkAdapterDisplayItem.CategoryOrder),
                ListSortDirection.Ascending));

        adapterListView.SortDescriptions.Add(
            new SortDescription(
                nameof(NetworkAdapterDisplayItem.Name),
                ListSortDirection.Ascending));

        AdapterListView = adapterListView;

        DataContext = this;

        Loaded += DiagnosticsView_Loaded;
        Unloaded += DiagnosticsView_Unloaded;
    }

    private void DiagnosticsView_Unloaded(object sender, RoutedEventArgs e)
    {
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
    }

    private async void DiagnosticsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;

        LocalizationManager_LanguageChanged(this, EventArgs.Empty);

        if (_hasLoaded)
            return;

        _hasLoaded = true;

        await LoadAdaptersAsync();
    }

    private void LocalizationManager_LanguageChanged(object? sender, EventArgs e)
    {
        foreach (var adapter in Adapters)
            adapter.RefreshLocalization();

        AdapterListView.Refresh();

        // Enum and result converters are binding-driven. Re-publish the
        // existing evidence so their display values are evaluated again
        // against the newly loaded language dictionary.
        if (EvidenceResults.Count > 0)
        {
            var evidenceSnapshot = EvidenceResults.ToArray();
            EvidenceResults.Clear();
            foreach (var evidence in evidenceSnapshot)
                EvidenceResults.Add(evidence);
        }

        if (AdapterComboBox.SelectedItem is NetworkAdapterDisplayItem selectedAdapter)
        {
            GatewayValueText.Text = selectedAdapter.FirstIpv4GatewayText;
            DnsServerValueText.Text = selectedAdapter.FirstIpv4DnsServerText;
        }
        else
        {
            GatewayValueText.Text = LocalizationManager.GetString("Common_NotConfigured");
            DnsServerValueText.Text = LocalizationManager.GetString("Common_NotConfigured");
        }

        UpdateAdapterSummary();
        UpdateDiagnosticSummary();
        SetStatus(_statusResourceKey, _statusArguments);
        UpdateLastUpdatedText();

        if (_connectivityStatusResourceKey is not null)
            SetConnectivityStatus(_connectivityStatusResourceKey, _connectivityStatusArguments,
                _connectivityTooltipResourceKey);

        if (_connectivityErrorResourceKey is not null)
            ConnectivityErrorText.Text = LocalizationManager.GetString(_connectivityErrorResourceKey);

        RunTestsButtonText.SetResourceReference(
            TextBlock.TextProperty,
            _isBusy ? "Diagnostics_Running" : "Diagnostics_RunTests");
    }

    private void SetStatus(string resourceKey, params object[] arguments)
    {
        _statusResourceKey = resourceKey;
        _statusArguments = arguments;
        StatusText.Text = LocalizationManager.Format(resourceKey, arguments);
    }

    private void SetLastUpdatedKey(string resourceKey)
    {
        _lastUpdatedResourceKey = resourceKey;
        _lastUpdatedAt = null;
        UpdateLastUpdatedText();
    }

    private void SetLastUpdatedTime(DateTime value)
    {
        _lastUpdatedAt = value;
        UpdateLastUpdatedText();
    }

    private void UpdateLastUpdatedText()
    {
        LastUpdatedText.Text = _lastUpdatedAt.HasValue
            ? LocalizationManager.Format("Diagnostics_LastUpdated", _lastUpdatedAt.Value)
            : LocalizationManager.GetString(_lastUpdatedResourceKey);
    }

    private void SetConnectivityStatus(
        string resourceKey,
        object[]? arguments = null,
        string? tooltipResourceKey = null)
    {
        _connectivityStatusResourceKey = resourceKey;
        _connectivityStatusArguments = arguments ?? Array.Empty<object>();
        _connectivityTooltipResourceKey = tooltipResourceKey;
        ConnectivityStatusText.Text = LocalizationManager.Format(resourceKey, _connectivityStatusArguments);
        ConnectivityStatusText.ToolTip = tooltipResourceKey is null
            ? null
            : LocalizationManager.GetString(tooltipResourceKey);
    }

    private void ShowConnectivityError(string resourceKey)
    {
        _connectivityErrorResourceKey = resourceKey;
        ConnectivityErrorText.Text = LocalizationManager.GetString(resourceKey);
        ConnectivityErrorCard.Visibility = Visibility.Visible;
    }

    private async void Refresh_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadAdaptersAsync();
    }

    private void ConnectivityLayoutGrid_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        // Khi chiều rộng vùng Connectivity nhỏ hơn 980 px,
        // chuyển từ hai cột sang một cột.
        const double CompactBreakpoint = 980;

        bool useCompactLayout =
            e.NewSize.Width < CompactBreakpoint;

        if (useCompactLayout == _isCompactConnectivityLayout)
            return;

        _isCompactConnectivityLayout = useCompactLayout;

        if (useCompactLayout)
        {
            // =====================================
            // COMPACT: Xếp Connectivity thành 1 cột
            // =====================================

            ConnectivityLayoutGrid.ColumnDefinitions[0].Width =
                new GridLength(1, GridUnitType.Star);

            ConnectivityLayoutGrid.ColumnDefinitions[1].Width =
                new GridLength(0, GridUnitType.Pixel);

            ConnectivityLayoutGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            ConnectivityLayoutGrid.RowDefinitions[1].Height =
                GridLength.Auto;

            Grid.SetRow(ConnectivityOutputPanel, 1);
            Grid.SetColumn(ConnectivityOutputPanel, 0);

            ConnectivityTestsCard.Margin =
                new Thickness(0, 0, 0, 12);

            ConnectivityOutputPanel.Margin =
                new Thickness(0);
        }
        else
        {
            // =====================================
            // WIDE: Xếp Connectivity thành 2 cột
            // =====================================

            ConnectivityLayoutGrid.ColumnDefinitions[0].Width =
                new GridLength(0.9, GridUnitType.Star);

            ConnectivityLayoutGrid.ColumnDefinitions[1].Width =
                new GridLength(1.1, GridUnitType.Star);

            ConnectivityLayoutGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            ConnectivityLayoutGrid.RowDefinitions[1].Height =
                new GridLength(0, GridUnitType.Pixel);

            Grid.SetRow(ConnectivityOutputPanel, 0);
            Grid.SetColumn(ConnectivityOutputPanel, 1);

            ConnectivityTestsCard.Margin =
                new Thickness(0, 0, 8, 0);

            ConnectivityOutputPanel.Margin =
                new Thickness(8, 0, 0, 0);
        }
    }

    private void NetworkInterfacesLayoutGrid_SizeChanged(
    object sender,
    SizeChangedEventArgs e)
    {
        // Vùng nội dung bên trong trang, không phải độ phân giải màn hình.
        const double CompactBreakpoint = 1050;

        bool useCompactLayout =
            e.NewSize.Width < CompactBreakpoint;

        if (useCompactLayout == _isCompactNetworkInterfacesLayout)
            return;

        _isCompactNetworkInterfacesLayout = useCompactLayout;

        if (useCompactLayout)
        {
            // =====================================
            // COMPACT: List ở trên, Inspector ở dưới
            // =====================================

            NetworkInterfacesLayoutGrid.ColumnDefinitions[0].Width =
                new GridLength(1, GridUnitType.Star);

            NetworkInterfacesLayoutGrid.ColumnDefinitions[1].Width =
                new GridLength(0, GridUnitType.Pixel);

            NetworkInterfacesLayoutGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            NetworkInterfacesLayoutGrid.RowDefinitions[1].Height =
                GridLength.Auto;

            Grid.SetRow(AdapterInspectorCard, 1);
            Grid.SetColumn(AdapterInspectorCard, 0);

            AdapterListCard.Margin =
                new Thickness(0, 0, 0, 12);

            AdapterInspectorCard.Margin =
                new Thickness(0);

            AdapterListBox.MaxHeight = 420;
        }
        else
        {
            // =====================================
            // WIDE: List trái, Inspector phải
            // =====================================

            NetworkInterfacesLayoutGrid.ColumnDefinitions[0].Width =
                new GridLength(0.42, GridUnitType.Star);

            NetworkInterfacesLayoutGrid.ColumnDefinitions[1].Width =
                new GridLength(0.58, GridUnitType.Star);

            NetworkInterfacesLayoutGrid.RowDefinitions[0].Height =
                GridLength.Auto;

            NetworkInterfacesLayoutGrid.RowDefinitions[1].Height =
                new GridLength(0, GridUnitType.Pixel);

            Grid.SetRow(AdapterInspectorCard, 0);
            Grid.SetColumn(AdapterInspectorCard, 1);

            AdapterListCard.Margin =
                new Thickness(0, 0, 8, 0);

            AdapterInspectorCard.Margin =
                new Thickness(8, 0, 0, 0);

            AdapterListBox.MaxHeight = 620;
        }
    }

    private void AdapterListBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingAdapters || _isBusy)
            return;

        if (AdapterListBox.SelectedItem
            is not NetworkAdapterDisplayItem adapter)
        {
            AdapterInspectorContent.Content = null;

            AdapterInspectorPlaceholder.Visibility =
                Visibility.Visible;

            return;
        }

        // Cập nhật Inspector.
        AdapterInspectorPlaceholder.Visibility =
            Visibility.Collapsed;

        AdapterInspectorContent.Content = adapter;

        // Đồng bộ với Connectivity Adapter ComboBox.
        if (!ReferenceEquals(
                AdapterComboBox.SelectedItem,
                adapter))
        {
            AdapterComboBox.SelectedItem = adapter;
        }
    }

    private void AdapterSummaryStats_SizeChanged(
    object sender,
    SizeChangedEventArgs e)
    {
        const double CompactBreakpoint = 760;

        AdapterSummaryStats.Columns =
            e.NewSize.Width < CompactBreakpoint
                ? 3
                : 5;
    }

    private void AdapterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isUpdatingAdapters)
            return;

        ClearConnectivityResults();

        if (AdapterComboBox.SelectedItem is not NetworkAdapterDisplayItem adapter)
        {
            SourceIpv4ComboBox.ItemsSource = null;

            AdapterListBox.SelectedItem = null;

            AdapterInspectorContent.Content = null;

            AdapterInspectorPlaceholder.Visibility =
                Visibility.Visible;

            GatewayValueText.Text = LocalizationManager.GetString("Common_NotConfigured");
            DnsServerValueText.Text = LocalizationManager.GetString("Common_NotConfigured");

            RunTestsButton.IsEnabled = false;

            UpdateDiagnosticSummary();

            return;
        }

        // Đồng bộ lựa chọn trong Adapter List.
        if (!ReferenceEquals(
                AdapterListBox.SelectedItem,
                adapter))
        {
            AdapterListBox.SelectedItem = adapter;
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

        UpdateDiagnosticSummary();
    }

    private void SourceIpv4ComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        HandleConfigurationChanged();
    }

    private void ConnectivityInputs_TextChanged(
    object sender,
    TextChangedEventArgs e)
    {
        HandleConfigurationChanged();
    }

    private void HandleConfigurationChanged()
    {
        if (_isBusy)
            return;

        ClearConnectivityResults();

        UpdateRunButtonState();
        UpdateDiagnosticSummary();
    }

    private string GetTcpConfigurationStatus()
    {
        string host = TcpTargetTextBox.Text.Trim();
        string portText = TcpPortTextBox.Text.Trim();

        bool hasHost = !string.IsNullOrWhiteSpace(host);
        bool hasPort = !string.IsNullOrWhiteSpace(portText);

        if (!hasHost && !hasPort)
            return string.Empty;

        if (hasHost != hasPort)
            return LocalizationManager.GetString("Diagnostics_TcpIncomplete");

        if (!IPAddress.TryParse(host, out var ip) ||
            ip.AddressFamily !=
                System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return LocalizationManager.GetString("Diagnostics_TcpInvalidIpv4");
        }

        if (!int.TryParse(portText, out int port) ||
            port is < 1 or > 65535)
        {
            return LocalizationManager.GetString("Diagnostics_TcpInvalidPort");
        }

        return LocalizationManager.Format("Diagnostics_TcpReady", ip, port);
    }

    private string GetDnsConfigurationStatus(
    NetworkAdapterDisplayItem adapter)
    {
        string hostname = DnsHostTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(hostname))
            return string.Empty;

        if (!adapter.HasIpv4DnsServer)
            return LocalizationManager.GetString("Diagnostics_DnsServerNone");

        try
        {
            string asciiHost =
                new System.Globalization.IdnMapping()
                    .GetAscii(hostname);

            if (asciiHost.Length > 253)
                return LocalizationManager.GetString("Diagnostics_DnsInvalidHostname");

            string[] labels = asciiHost.Split('.');

            bool invalid = labels.Any(label =>
                label.Length is < 1 or > 63 ||
                label.StartsWith('-') ||
                label.EndsWith('-') ||
                label.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character != '-'));

            if (invalid)
                return LocalizationManager.GetString("Diagnostics_DnsInvalidHostname");

            return LocalizationManager.Format("Diagnostics_DnsReady", hostname);
        }
        catch (ArgumentException)
        {
            return LocalizationManager.GetString("Diagnostics_DnsInvalidHostname");
        }
    }

    private void UpdateDiagnosticSummary()
    {
        var adapter =
            AdapterComboBox.SelectedItem as NetworkAdapterDisplayItem;

        // Adapter hiện được chọn.
        SummaryAdapterText.Text =
            adapter?.Name ?? LocalizationManager.GetString("Diagnostics_NotSelected");

        // Source IPv4 hiện được chọn.
        SummarySourceText.Text =
            SourceIpv4ComboBox.SelectedItem as string ?? LocalizationManager.GetString("Diagnostics_NotSelected");

        // Chưa chọn adapter thì chưa có kế hoạch kiểm tra.
        if (adapter is null)
        {
            SummaryChecksText.Text =
                LocalizationManager.GetString("Diagnostics_SelectAdapterForChecks");

            return;
        }

        var plannedChecks = new List<string>();

        bool canPingGateway =
            adapter.HasIpv4Gateway &&
            SourceIpv4ComboBox.SelectedItem is string;

        plannedChecks.Add(
            canPingGateway
                ? LocalizationManager.Format("Diagnostics_GatewayPing", adapter.FirstIpv4GatewayText)
                : LocalizationManager.GetString("Diagnostics_GatewayUnavailable"));

        string tcpStatus =
            GetTcpConfigurationStatus();

        if (!string.IsNullOrWhiteSpace(tcpStatus))
        {
            plannedChecks.Add(tcpStatus);
        }

        string dnsStatus =
            GetDnsConfigurationStatus(adapter);

        if (!string.IsNullOrWhiteSpace(dnsStatus))
        {
            plannedChecks.Add(dnsStatus);
        }

        SummaryChecksText.Text =
            string.Join(Environment.NewLine, plannedChecks);
    }

    private async Task LoadAdaptersAsync()
    {
        if (_isBusy)
            return;

        string? previouslySelectedAdapterId =
            (AdapterComboBox.SelectedItem as NetworkAdapterDisplayItem)?.Id;

        SetBusyState(true);

        LoadingText.Visibility =
            Visibility.Visible;
        AdapterLoadingProgressBar.Visibility = MotionSettings.Current.ReducedMotion
            ? Visibility.Collapsed : Visibility.Visible;

        EmptyState.Visibility =
            Visibility.Collapsed;

        ErrorCard.Visibility =
            Visibility.Collapsed;

        SetStatus(_hasSuccessfulAdapterLoad
            ? "Diagnostics_LoadingAdapters"
            : "Diagnostics_ReadingAdapters");

        SetLastUpdatedKey(_hasSuccessfulAdapterLoad
            ? "Diagnostics_Refreshing"
            : "Diagnostics_NotRefreshed");

        // Không ẩn AdapterItemsControl và không xóa Adapters ở đây.
        // Snapshot cũ vẫn được giữ trong lúc đọc dữ liệu mới.

        try
        {
            var snapshots =
                await _diagnosticService.GetNetworkAdaptersAsync();

            // Chỉ thay dữ liệu cũ sau khi đã nhận kết quả mới.
            var newAdapters = snapshots
                .OrderBy(
                    adapter => adapter.Name,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    snapshot => new NetworkAdapterDisplayItem(snapshot))
                .ToList();

            // Bắt đầu thay danh sách adapter.
            _isUpdatingAdapters = true;

            try
            {
                // Xóa lựa chọn cũ trước khi thay collection.
                AdapterListBox.SelectedItem = null;
                AdapterComboBox.SelectedItem = null;

                AdapterInspectorContent.Content = null;

                AdapterInspectorPlaceholder.Visibility =
                    Visibility.Visible;

                // Xóa dữ liệu cũ và thêm dữ liệu mới.
                Adapters.Clear();

                foreach (var adapter in newAdapters)
                {
                    Adapters.Add(adapter);
                }
            }
            finally
            {
                // Cho phép các SelectionChanged hoạt động trở lại.
                _isUpdatingAdapters = false;
            }

            // Cập nhật grouping và sorting sau khi thay dữ liệu.
            AdapterListView.Refresh();

            _hasSuccessfulAdapterLoad = true;

            // Ưu tiên giữ adapter mà người dùng đã chọn trước Refresh.
            var adapterToSelect =
                Adapters.FirstOrDefault(adapter =>
                    string.Equals(
                        adapter.Id,
                        previouslySelectedAdapterId,
                        StringComparison.OrdinalIgnoreCase))
                ?? Adapters.FirstOrDefault(adapter =>
                    adapter.Ipv4Addresses.Count > 0 &&
                    adapter.HasIpv4Gateway)
                ?? Adapters.FirstOrDefault(adapter =>
                    adapter.Ipv4Addresses.Count > 0);

            // Chọn sau khi hoàn thành cập nhật collection.
            AdapterComboBox.SelectedItem = adapterToSelect;
            AdapterListBox.SelectedItem = adapterToSelect;
            AdapterInspectorContent.Content = adapterToSelect;
            AdapterInspectorPlaceholder.Visibility = adapterToSelect is null
                ? Visibility.Visible : Visibility.Collapsed;

            UpdateAdapterSummary();

            SetLastUpdatedTime(DateTime.Now);

            AdapterListBox.Visibility =
                Adapters.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            EmptyState.Visibility =
                Adapters.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ClearConnectivityResults();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Diagnostics] Adapter discovery failed: {ex}");
            ErrorText.Text = LocalizationManager.GetString("Diagnostics_ErrorDetail");

            ErrorCard.Visibility =
                Visibility.Visible;

            if (_hasSuccessfulAdapterLoad)
            {
                // Có snapshot cũ: tiếp tục hiển thị, nhưng cảnh báo.
                AdapterListBox.Visibility =
                    Adapters.Count > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                SetStatus("Diagnostics_PreviousSnapshot", Adapters.Count);

                SetLastUpdatedKey("Diagnostics_LastSuccessfulRefresh");

                EmptyState.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                // Lần đọc đầu tiên thất bại, chưa có dữ liệu cũ.
                AdapterListBox.Visibility =
                    Visibility.Collapsed;

                SetStatus("Diagnostics_UnableReadAdaptersShort");

                SetLastUpdatedKey("Diagnostics_NotAvailable");
            }
        }
        finally
        {
            LoadingText.Visibility =
                Visibility.Collapsed;
            AdapterLoadingProgressBar.Visibility = Visibility.Collapsed;

            SetBusyState(false);

            UpdateRunButtonState();
        }
    }

    private void SetBusyState(bool isBusy)
    {
        _isBusy = isBusy;

        AdapterComboBox.IsEnabled = !isBusy;
        SourceIpv4ComboBox.IsEnabled = !isBusy;

        TcpTargetTextBox.IsEnabled = !isBusy;
        TcpPortTextBox.IsEnabled = !isBusy;
        DnsHostTextBox.IsEnabled = !isBusy;

        AdapterListBox.IsEnabled = !isBusy;

        RefreshButton.IsEnabled = !isBusy;

        UpdateRunButtonState();
    }

    private void UpdateAdapterSummary()
    {
        int physicalCount = Adapters.Count(
            adapter => adapter.Category ==
                NetworkInterfaceCategory.Physical);

        int virtualCount = Adapters.Count(
            adapter => adapter.Category ==
                NetworkInterfaceCategory.Software);

        int tunnelCount = Adapters.Count(
            adapter => adapter.Category ==
                NetworkInterfaceCategory.Tunnel);

        int otherCount = Adapters.Count(
            adapter =>
                adapter.Category != NetworkInterfaceCategory.Physical &&
                adapter.Category != NetworkInterfaceCategory.Software &&
                adapter.Category != NetworkInterfaceCategory.Tunnel);

        int upCount = Adapters.Count(
            adapter => adapter.IsUp);

        PhysicalAdapterCountText.Text =
            physicalCount.ToString();

        VirtualAdapterCountText.Text =
            virtualCount.ToString();

        TunnelAdapterCountText.Text =
            tunnelCount.ToString();

        OtherAdapterCountText.Text =
            otherCount.ToString();

        UpAdapterCountText.Text =
            upCount.ToString();

        SetStatus("Diagnostics_NetworkInterfacesDetected", Adapters.Count);

        AdapterListCountText.Text = LocalizationManager.Format("Diagnostics_InterfaceCount", Adapters.Count);
    }

    private async void RunConnectivityTests_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        // 1. Kiểm tra adapter.
        if (AdapterComboBox.SelectedItem
            is not NetworkAdapterDisplayItem adapter)
        {
            ShowConnectivityError("Diagnostics_SelectAdapterError");

            return;
        }

        // 2. Kiểm tra Source IPv4.
        if (SourceIpv4ComboBox.SelectedItem
            is not string sourceAddress)
        {
            ShowConnectivityError("Diagnostics_InvalidSourceIPv4");

            return;
        }

        // 3. Lấy cấu hình TCP.
        string rawTcpHost =
            TcpTargetTextBox.Text.Trim();

        string rawTcpPort =
            TcpPortTextBox.Text.Trim();

        bool hasTcpHost =
            !string.IsNullOrWhiteSpace(rawTcpHost);

        bool hasTcpPort =
            !string.IsNullOrWhiteSpace(rawTcpPort);

        if (hasTcpHost != hasTcpPort)
        {
            ShowConnectivityError("Diagnostics_TcpBothFields");

            return;
        }

        string? tcpHost = null;
        int? tcpPort = null;

        if (hasTcpHost)
        {
            if (!IPAddress.TryParse(
                    rawTcpHost,
                    out var targetIp) ||
                targetIp.AddressFamily !=
                    System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ShowConnectivityError("Diagnostics_TcpTargetInvalid");

                return;
            }

            if (!int.TryParse(
                    rawTcpPort,
                    out int parsedPort) ||
                parsedPort is < 1 or > 65535)
            {
                ShowConnectivityError("Diagnostics_TcpPortInvalid");

                return;
            }

            tcpHost = targetIp.ToString();
            tcpPort = parsedPort;
        }

        // 4. Lấy cấu hình DNS.
        string? dnsHost =
            string.IsNullOrWhiteSpace(DnsHostTextBox.Text)
                ? null
                : DnsHostTextBox.Text.Trim();

        if (dnsHost is not null &&
            !adapter.HasIpv4DnsServer)
        {
            ShowConnectivityError("Diagnostics_NoDnsServer");

            return;
        }

        // 5. Lưu snapshot cấu hình của phiên kiểm tra.
        // Không đọc lại các control sau khi bắt đầu await.
        string testedAdapterName = adapter.Name;
        string testedAdapterId = adapter.Id;
        string testedSourceAddress = sourceAddress;

        // 6. Bật trạng thái Running ngay lập tức.
        EvidenceResults.Clear();

        ConnectivityErrorCard.Visibility =
            Visibility.Collapsed;

        ResultsPlaceholderCard.Visibility =
            Visibility.Collapsed;

        ResultsCard.Visibility =
            Visibility.Visible;

        SetConnectivityStatus("Diagnostics_RunningTests");
        ConnectivityProgressBar.Visibility = MotionSettings.Current.ReducedMotion
            ? Visibility.Collapsed : Visibility.Visible;

        ConnectivityInfoText.Text =
            $"{testedAdapterName} · {testedSourceAddress}";

        ConnectivityInfoText.ToolTip =
            LocalizationManager.GetString("Diagnostics_ConfigCaptured");

        // 7. Khóa các control trong khi kiểm tra.
        SetBusyState(true);

        RunTestsButtonText.SetResourceReference(TextBlock.TextProperty, "Diagnostics_Running");

        try
        {
            // 8. Gọi Application Service.
            var session = await _diagnosticService.RunAsync(
                testedAdapterId,
                testedSourceAddress,
                tcpHost,
                tcpPort,
                dnsHost);

            // 9. Hiển thị bằng chứng.
            EvidenceResults.Clear();

            foreach (var evidence in session.Evidence)
            {
                EvidenceResults.Add(evidence);
            }

            int successfulTests =
                session.Evidence.Count(
                    item => item.IsSuccessful);

            if (session.OverallStatus == DiagnosticStatus.Online)
            {
                SetConnectivityStatus(
                    "Diagnostics_OnlineSummary",
                    new object[] { successfulTests, session.Evidence.Count });
            }
            else
            {
                SetConnectivityStatus(
                    "Diagnostics_UnknownSummary",
                    tooltipResourceKey: "Diagnostics_NoSuccessfulResponse");
            }

            ConnectivityInfoText.Text =
                $"{testedAdapterName} · " +
                $"{testedSourceAddress} · " +
                $"{session.StartedAt.ToLocalTime():HH:mm:ss}";

            if (session.OverallStatus == DiagnosticStatus.Unknown)
            {
                ConnectivityStatusText.ToolTip =
                    LocalizationManager.GetString("Diagnostics_NoSuccessfulResponse");
                _connectivityTooltipResourceKey = "Diagnostics_NoSuccessfulResponse";
            }
            else
            {
                ConnectivityStatusText.ToolTip = null;
            }
        }
        catch (Exception ex)
        {
            ResultsCard.Visibility =
                Visibility.Collapsed;

            System.Diagnostics.Debug.WriteLine($"[Diagnostics] Connectivity test failed: {ex}");
            ShowConnectivityError("Diagnostics_ErrorDetail");
        }
        finally
        {
            SetBusyState(false);
            ConnectivityProgressBar.Visibility = Visibility.Collapsed;

            RunTestsButtonText.SetResourceReference(TextBlock.TextProperty, "Diagnostics_RunTests");

            UpdateDiagnosticSummary();
        }
    }

    private void ClearConnectivityResults()
    {
        // Xóa kết quả của adapter hoặc phiên kiểm tra trước.
        EvidenceResults.Clear();

        // Ẩn kết quả cũ và lỗi cũ.
        ResultsCard.Visibility = Visibility.Collapsed;
        ConnectivityErrorCard.Visibility = Visibility.Collapsed;
        _connectivityErrorResourceKey = null;

        // Hiển thị lại trạng thái chờ.
        ResultsPlaceholderCard.Visibility = Visibility.Visible;

        ConnectivityStatusText.ToolTip = null;
        ConnectivityInfoText.Text = string.Empty;
        _connectivityStatusResourceKey = null;
        _connectivityStatusArguments = Array.Empty<object>();
        _connectivityTooltipResourceKey = null;

        UpdateDiagnosticSummary();
    }

    private bool CanRunConnectivityTests()
    {
        return
            AdapterComboBox.SelectedItem
                is NetworkAdapterDisplayItem adapter &&
            SourceIpv4ComboBox.SelectedItem
                is string sourceAddress &&
            adapter.Ipv4Addresses.Contains(
                sourceAddress,
                StringComparer.OrdinalIgnoreCase) &&
            adapter.HasIpv4Gateway;
    }

    private void UpdateRunButtonState()
    {
        RunTestsButton.IsEnabled =
            !_isBusy && CanRunConnectivityTests();
    }
}