
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Media;

using LANRescue.ConsoleApp.Services;
using LANRescue.Application.Services;
using LANRescue.Domain.Entities;
using LANRescue.Domain.Enums;

namespace LANRescue.ConsoleApp.Views;

public partial class IpConflictView : UserControl
{
    private const double CompactLayoutBreakpoint = 1120;

    private readonly IpConflictService _service;

    // Data source được giữ ổn định.
    // Khi cập nhật kết quả, chỉ thay đổi collection,
    // không tạo ICollectionView mới.
    private readonly ObservableCollection<ConflictGridRow> _conflictRows = new();

    private readonly ICollectionView _conflictView;

    private bool _hasLoaded;
    private bool _isBusy;

    private bool _layoutInitialized;
    private bool _isCompactLayout;

    // Filter state.
    private string _ipFilter = "";
    private string _macFilter = "";
    private string _statusFilter = "";
    private string _observationCountFilter = "";
    private string _firstSeenFilter = "";
    private string _lastSeenFilter = "";

    public IpConflictView(IpConflictService service)
    {
        InitializeComponent();

        _service = service
            ?? throw new ArgumentNullException(nameof(service));

        // Tạo CollectionView để hỗ trợ filter và sorting.
        _conflictView = CollectionViewSource.GetDefaultView(_conflictRows);

        _conflictView.Filter = FilterConflictRow;

        // Sắp xếp mặc định:
        // 1. Network address tăng dần.
        // 2. Host address tăng dần.
        _conflictView.SortDescriptions.Add(
            new SortDescription(
                nameof(ConflictGridRow.NetworkSortValue),
                ListSortDirection.Ascending));

        _conflictView.SortDescriptions.Add(
            new SortDescription(
                nameof(ConflictGridRow.HostSortValue),
                ListSortDirection.Ascending));

        ConflictGrid.ItemsSource = _conflictView;

        Unloaded += IpConflictView_Unloaded;

        Loaded += IpConflictView_Loaded;
    }

    // =========================================================
    // PAGE INITIALIZATION
    // =========================================================

    private async void IpConflictView_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        LocalizationManager.LanguageChanged -=
            LocalizationManager_LanguageChanged;

        LocalizationManager.LanguageChanged +=
            LocalizationManager_LanguageChanged;

        RefreshLocalizedContent();

        if (_hasLoaded)
            return;

        _hasLoaded = true;

        await LoadAdaptersAsync();
    }

    private void IpConflictView_Unloaded(
    object sender,
    RoutedEventArgs e)
    {
        LocalizationManager.LanguageChanged -=
            LocalizationManager_LanguageChanged;
    }

    // =========================================================
    // RESPONSIVE WORKSPACE LAYOUT
    // =========================================================

    private void WorkspaceGrid_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        bool compact =
            e.NewSize.Width < CompactLayoutBreakpoint;

        ApplyWorkspaceLayout(compact);
    }

    private void ApplyWorkspaceLayout(bool compact)
    {
        if (_layoutInitialized &&
            _isCompactLayout == compact)
        {
            return;
        }

        _layoutInitialized = true;
        _isCompactLayout = compact;

        if (compact)
        {
            // =============================================
            // COMPACT MODE
            // Network Scope nằm trên.
            // Observed IP/MAC nằm dưới.
            // =============================================

            ObservedColumn.Width =
                new GridLength(1, GridUnitType.Star);

            ScopeColumn.Width =
                new GridLength(0);

            ScopeColumn.MinWidth = 0;

            WorkspaceRow0.Height = GridLength.Auto;
            WorkspaceRow1.Height = GridLength.Auto;

            Grid.SetRow(ScopePanel, 0);
            Grid.SetColumn(ScopePanel, 0);

            Grid.SetRow(ObservedPanel, 1);
            Grid.SetColumn(ObservedPanel, 0);

            ScopePanel.MinWidth = 0;
            ScopePanel.MaxWidth = double.PositiveInfinity;

            // Đường phân cách ngang.
            ScopePanel.BorderThickness =
                new Thickness(0, 0, 0, 1);
        }
        else
        {
            // =============================================
            // WIDE MODE
            // Observed IP/MAC nằm bên trái.
            // Network Scope nằm bên phải.
            // =============================================

            ObservedColumn.Width =
                new GridLength(1, GridUnitType.Star);

            ScopeColumn.Width = GridLength.Auto;
            ScopeColumn.MinWidth = 0;

            WorkspaceRow0.Height = GridLength.Auto;
            WorkspaceRow1.Height = new GridLength(0);

            Grid.SetRow(ObservedPanel, 0);
            Grid.SetColumn(ObservedPanel, 0);

            Grid.SetRow(ScopePanel, 0);
            Grid.SetColumn(ScopePanel, 1);

            ScopePanel.MinWidth = 410;
            ScopePanel.MaxWidth = 450;

            // Đường phân cách dọc.
            ScopePanel.BorderThickness =
                new Thickness(1, 0, 0, 0);
        }
    }

    // =========================================================
    // NETWORK ADAPTER
    // =========================================================

    private async Task LoadAdaptersAsync()
    {
        if (_isBusy)
            return;

        SetBusy(true);

        try
        {
            SetStatus("IpConflicts_LoadingAdapters");

            await _service.InitializeAsync();

            var adapters = await _service.GetAdaptersAsync();

            var options = new List<AdapterSourceOption>();

            foreach (var adapter in adapters)
            {
                int count = Math.Min(
                    adapter.Ipv4Addresses.Count,
                    adapter.Subnets.Count);

                for (int i = 0; i < count; i++)
                {
                    string sourceAddress = adapter.Ipv4Addresses[i];
                    string subnetMask = adapter.Subnets[i];

                    if (!IPAddress.TryParse(
                            sourceAddress,
                            out var sourceIp) ||
                        sourceIp.AddressFamily !=
                            System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    if (!IPAddress.TryParse(
                            subnetMask,
                            out var mask) ||
                        mask.AddressFamily !=
                            System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    options.Add(new AdapterSourceOption(
                        adapter.Id,
                        adapter.Name,
                        sourceAddress,
                        subnetMask));
                }
            }

            AdapterComboBox.ItemsSource = options;

            AdapterComboBox.SelectedIndex =
                options.Count > 0 ? 0 : -1;

            if (options.Count == 0)
            {
                ScopeText.Text = LocalizationManager.GetString("IpConflicts_NoActiveIPv4");

                SetStatus("IpConflicts_NoAdapterReady");

                _conflictRows.Clear();
                UpdateResultSummary();

                return;
            }

            SetStatus("IpConflicts_AdapterCountLoaded", options.Count);

            UpdateScopeText();

            await LoadSelectedRecentEvidenceAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IP Conflicts] Adapter load failed: {ex}");
            SetStatus("IpConflicts_LoadAdaptersFailed");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void AdapterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        UpdateScopeText();

        // Khi khởi tạo danh sách hoặc đang scan,
        // không tải lại dữ liệu lần thứ hai.
        if (!IsLoaded || !_hasLoaded || _isBusy)
            return;

        await LoadSelectedRecentEvidenceAsync();
    }

    private void UpdateScopeText()
    {
        if (AdapterComboBox.SelectedItem
            is not AdapterSourceOption selected)
        {
            ScopeText.Text =
                LocalizationManager.GetString("IpConflicts_SelectActiveIPv4");

            return;
        }

        ScopeText.Text = string.Join(Environment.NewLine,
            LocalizationManager.Format("IpConflicts_SelectedNetworkAdapter", selected.AdapterName),
            LocalizationManager.Format("IpConflicts_SelectedNetworkSource", selected.SourceAddress),
            LocalizationManager.Format("IpConflicts_SelectedNetworkMask", selected.SubnetMask));
    }

    private async void RefreshAdapters_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        await LoadAdaptersAsync();
    }

    // =========================================================
    // SCAN SUBNET
    // =========================================================

    private async void ScanSubnet_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        if (AdapterComboBox.SelectedItem
            is not AdapterSourceOption selected)
        {
            SetStatus("IpConflicts_SelectAdapterFirst");

            return;
        }

        SetBusy(true);

        try
        {
            SetStatus("IpConflicts_Scanning", selected.SourceAddress);

            var groups = await _service.ScanSubnetAsync(
                selected.AdapterId,
                selected.SourceAddress);

            RenderGroups(groups, selected.SubnetMask);

            int suspectedCount = groups.Count(
                group => group.Status == IpConflictStatus.Suspected);

            int observedCount = groups.Count;

            int totalObservations = groups.Sum(
                group => group.ObservationCount);

            if (groups.Count == 0)
            {
                SetStatus("IpConflicts_NoArpResponses");
            }
            else
            {
                SetStatus("IpConflicts_ScanSummary", observedCount, totalObservations, suspectedCount);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("IpConflicts_ScanCancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IP Conflicts] Scan failed: {ex}");
            SetStatus("IpConflicts_ScanFailed");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =========================================================
    // LOAD RECENT EVIDENCE
    // =========================================================

    private async void LoadRecent_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        await LoadSelectedRecentEvidenceAsync();
    }

    private async Task LoadSelectedRecentEvidenceAsync()
    {
        if (AdapterComboBox.SelectedItem
            is not AdapterSourceOption selected)
        {
            UpdateRows(Array.Empty<ConflictGridRow>());
            return;
        }

        UpdateRows(Array.Empty<ConflictGridRow>());

        try
        {
            SetStatus("IpConflicts_LoadingEvidence");
            var groups = await _service.GetRecentGroupsAsync(
                selected.AdapterId,
                selected.SourceAddress,
                TimeSpan.FromMinutes(5));

            RenderGroups(groups, selected.SubnetMask);

            if (groups.Count == 0)
                SetStatus("IpConflicts_NoRecentEvidence");
            else
                SetStatus("IpConflicts_EvidenceLoaded", groups.Count);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[IP Conflicts] Load evidence failed: {ex}");

            SetStatus("IpConflicts_LoadEvidenceFailed");
        }
    }

    // =========================================================
    // RENDER RESULT
    // =========================================================

    private void RenderGroups(
    IReadOnlyList<IpConflictGroup> groups,
    string subnetMask)
    {
        uint maskValue = ToIpv4UInt32(subnetMask);

        var rows = groups.Select(group =>
        {
            uint addressValue = ToIpv4UInt32(group.IpAddress);

            return new ConflictGridRow
            {
                IpAddress = group.IpAddress,

                MacAddresses = group.MacAddressesDisplay,

                StatusCode = group.Status,

                ObservationCount = group.ObservationCount,

                FirstSeenAt = group.FirstSeenAt,

                LastSeenAt = group.LastSeenAt,

                FirstSeen = group.FirstSeenAt
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss"),

                LastSeen = group.LastSeenAt
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss"),

                NetworkSortValue = addressValue & maskValue,

                HostSortValue = addressValue & ~maskValue
            };
        }).ToArray();

        UpdateRows(rows);
    }

    private void UpdateRows(IReadOnlyList<ConflictGridRow> rows)
    {
        // Tạm hoãn refresh trong khi thay toàn bộ dữ liệu.
        using (_conflictView.DeferRefresh())
        {
            _conflictRows.Clear();

            foreach (var row in rows)
            {
                _conflictRows.Add(row);
            }
        }

        // Chỉ cập nhật Summary sau khi DeferRefresh đã kết thúc.
        UpdateResultSummary();
    }

    // =========================================================
    // IPV4 NUMERIC CONVERSION
    // =========================================================

    private static uint ToIpv4UInt32(string address)
    {
        if (!IPAddress.TryParse(address, out var parsed) ||
            parsed.AddressFamily !=
                System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new FormatException(
                $"Invalid IPv4 address: {address}");
        }

        byte[] bytes = parsed.GetAddressBytes();

        return
            ((uint)bytes[0] << 24) |
            ((uint)bytes[1] << 16) |
            ((uint)bytes[2] << 8) |
            bytes[3];
    }

    // =========================================================
    // DATA GRID FILTERS
    // =========================================================

    private void ColumnFilter_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        string filterValue = textBox.Text.Trim();

        switch (textBox.Tag?.ToString())
        {
            case "IpAddress":
                _ipFilter = filterValue;
                break;

            case "MacAddresses":
                _macFilter = filterValue;
                break;

            case "Status":
                _statusFilter = filterValue;
                break;

            case "ObservationCount":
                _observationCountFilter = filterValue;
                break;

            case "FirstSeen":
                _firstSeenFilter = filterValue;
                break;

            case "LastSeen":
                _lastSeenFilter = filterValue;
                break;
        }

        _conflictView.Refresh();

        UpdateResultSummary();
    }

    private bool FilterConflictRow(object item)
    {
        if (item is not ConflictGridRow row)
            return false;

        return
            Matches(row.IpAddress, _ipFilter) &&
            Matches(row.MacAddresses, _macFilter) &&
            Matches(row.Status, _statusFilter) &&
            Matches(
                row.ObservationCount.ToString(),
                _observationCountFilter) &&
            Matches(row.FirstSeen, _firstSeenFilter) &&
            Matches(row.LastSeen, _lastSeenFilter);
    }

    private static bool Matches(
        string? value,
        string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return value?.Contains(
            filter,
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private void UpdateResultSummary()
    {
        int total = _conflictRows.Count;

        // Chỉ đếm collection nguồn, không duyệt ICollectionView.
        int visible = _conflictRows.Count(
            row => FilterConflictRow(row));

        if (total == 0)
        {
            ResultSummaryText.Text =
                LocalizationManager.GetString(
                    "IpConflicts_NoObservations");

            return;
        }

        if (visible == 0)
        {
            ResultSummaryText.Text =
                LocalizationManager.Format(
                    "IpConflicts_NoMatchingResults",
                    total);

            return;
        }

        ResultSummaryText.Text = visible == total
            ? LocalizationManager.Format(
                "IpConflicts_TotalGroups",
                total)
            : LocalizationManager.Format(
                "IpConflicts_ShowingGroups",
                visible,
                total);
    }

    // =========================================================
    // IPV4 SORTING
    // =========================================================

    private void ConflictGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        // Chỉ can thiệp đặc biệt cho cột IPv4.
        if (!string.Equals(
                e.Column.SortMemberPath,
                nameof(ConflictGridRow.IpAddress),
                StringComparison.Ordinal))
        {
            // Các cột còn lại sử dụng sorting mặc định của WPF.
            return;
        }

        e.Handled = true;

        // Lần đầu: tăng dần.
        // Lần tiếp theo: giảm dần.
        ListSortDirection direction =
            e.Column.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

        // Xóa biểu tượng sort của các cột khác.
        foreach (var column in ConflictGrid.Columns)
        {
            if (!ReferenceEquals(column, e.Column))
            {
                column.SortDirection = null;
            }
        }

        e.Column.SortDirection = direction;

        using (_conflictView.DeferRefresh())
        {
            _conflictView.SortDescriptions.Clear();

            // Ưu tiên Network trước.
            _conflictView.SortDescriptions.Add(
                new SortDescription(
                    nameof(ConflictGridRow.NetworkSortValue),
                    direction));

            // Sau đó mới đến Host.
            _conflictView.SortDescriptions.Add(
                new SortDescription(
                    nameof(ConflictGridRow.HostSortValue),
                    direction));
        }
    }

    private void ClearFilters_Click(
        object sender,
        RoutedEventArgs e)
    {
        foreach (var textBox in FindVisualChildren<TextBox>(ConflictGrid))
        {
            if (textBox.Tag is string)
                textBox.Clear();
        }

        _ipFilter = "";
        _macFilter = "";
        _statusFilter = "";
        _observationCountFilter = "";
        _firstSeenFilter = "";
        _lastSeenFilter = "";

        _conflictView.Refresh();

        UpdateResultSummary();
    }

    private static IEnumerable<T> FindVisualChildren<T>(
        DependencyObject parent)
        where T : DependencyObject
    {
        if (parent is null)
            yield break;

        int count = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T target)
                yield return target;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    // =========================================================
    // BUSY STATE
    // =========================================================

    private void SetBusy(bool busy)
    {
        _isBusy = busy;

        AdapterComboBox.IsEnabled = !busy;

        ScanButton.IsEnabled = !busy;

        RefreshAdaptersButton.IsEnabled = !busy;

        LoadRecentButton.IsEnabled = !busy;
    }

    // =========================================================
    // INTERNAL MODELS
    // =========================================================

    private sealed record AdapterSourceOption(
        string AdapterId,
        string AdapterName,
        string SourceAddress,
        string SubnetMask)
    {
        public string DisplayName =>
            $"{AdapterName} · {SourceAddress} · {SubnetMask}";
    }

    private sealed class ConflictGridRow : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public void RefreshLocalization() =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Status)));
        public string IpAddress { get; init; } = "";

        public string MacAddresses { get; init; } = "";

        public IpConflictStatus StatusCode { get; init; }

        public string Status => LocalizationManager.GetString(
            StatusCode switch
            {
                IpConflictStatus.Observed =>
                    "Status_Observed",

                IpConflictStatus.Suspected =>
                    "Status_Suspected",

                IpConflictStatus.Confirmed =>
                    "Status_Confirmed",

                IpConflictStatus.InsufficientEvidence =>
                    "Status_InsufficientEvidence",

                IpConflictStatus.Resolved =>
                    "Status_Resolved",

                _ => "Status_Unknown"
            });

        public int ObservationCount { get; init; }

        public DateTimeOffset FirstSeenAt { get; init; }

        public DateTimeOffset LastSeenAt { get; init; }

        public string FirstSeen { get; init; } = "";

        public string LastSeen { get; init; } = "";

        // Used for IPv4 subnet/network sorting.
        public uint NetworkSortValue { get; init; }

        // Used for host sorting inside the subnet.
        public uint HostSortValue { get; init; }
    }

    // ========================================================
    // Language Set & Status
    // ========================================================
    private string _statusResourceKey =
    "IpConflicts_WaitingAdapter";

    private object[] _statusArguments = Array.Empty<object>();

    private void SetStatus(
        string resourceKey,
        params object[] arguments)
    {
        _statusResourceKey = resourceKey;
        _statusArguments = arguments;

        StatusText.Text = LocalizationManager.Format(
            resourceKey,
            arguments);
    }

    private void LocalizationManager_LanguageChanged(
        object? sender,
        EventArgs e) => RefreshLocalizedContent();

    private void RefreshLocalizedContent()
    {
        foreach (var row in _conflictRows)
            row.RefreshLocalization();

        _conflictView.Refresh();
        UpdateResultSummary();
        UpdateScopeText();
        SetStatus(_statusResourceKey, _statusArguments);
    }
}
