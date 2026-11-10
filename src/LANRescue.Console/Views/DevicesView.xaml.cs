using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LANRescue.ConsoleApp.Services;
using LANRescue.Application.Services;
using LANRescue.ConsoleApp.Models;
using LANRescue.ConsoleApp.Views.Dialogs;
using LANRescue.Domain.Entities;
using LANRescue.Infrastructure.Persistence;

namespace LANRescue.ConsoleApp.Views;

public partial class DevicesView : UserControl
{
    private readonly DeviceCatalogService _catalog;
    private IReadOnlyList<ManagedDevice> _devices = Array.Empty<ManagedDevice>();
    private bool _isInitialized;
    private bool _isCompactLayout;
    private bool _catalogAvailable;

    public ObservableCollection<DeviceRecord> Devices { get; } = [];

    public DevicesView()
    {
        InitializeComponent();
        DataContext = this;
        DevicesGrid.ItemsSource = Devices;

        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LANRescue", "data", "lanrescue.db");
        _catalog = new DeviceCatalogService(new SqliteDeviceRepository(databasePath));
        Loaded += DevicesView_Loaded;
    }

    private async void DevicesView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        CatalogLoadingState.Visibility = Visibility.Visible;
        CatalogLoadingProgressBar.Visibility = MotionSettings.Current.ReducedMotion
            ? Visibility.Collapsed : Visibility.Visible;
        CatalogErrorState.Visibility = Visibility.Collapsed;
        try
        {
            await _catalog.InitializeAsync();
            var existing = await _catalog.GetAllAsync();
            if (existing.Count == 0)
            {
                foreach (var sample in SampleDeviceData.Create())
                    await _catalog.SaveAsync(sample.Source);
            }

            _catalogAvailable = true;
            await ReloadDevicesAsync();
        }
        catch (Exception ex)
        {
            _catalogAvailable = false;
            RecordCountText.Text = "Database unavailable";
            CatalogErrorMessage.Text = ex.Message;
            CatalogErrorState.Visibility = Visibility.Visible;
        }
        finally
        {
            CatalogLoadingState.Visibility = Visibility.Collapsed;
        }
    }

    private async Task ReloadDevicesAsync(Guid? selectDeviceId = null)
    {
        _devices = await _catalog.GetAllAsync();
        ApplyFilter(selectDeviceId);
    }

    private void ApplyFilter(Guid? selectDeviceId = null)
    {
        if (DevicesGrid is null || !_catalogAvailable)
            return;

        CatalogErrorState.Visibility = Visibility.Collapsed;
        IEnumerable<ManagedDevice> query = _devices;
        query = FilterComboBox.SelectedIndex switch
        {
            1 => query.Where(device => device.IsManaged),
            2 => query.Where(device => !device.IsManaged),
            _ => query
        };

        var text = SearchBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(device =>
                Contains(device.DeviceCode, text) ||
                Contains(device.DeviceName, text) ||
                Contains(device.Location, text) ||
                Contains(device.Department, text) ||
                Contains(device.Manufacturer, text) ||
                Contains(device.Model, text) ||
                Contains(device.SerialNumber, text) ||
                device.NetworkInterfaces.Any(nic =>
                    Contains(nic.MacAddress, text) ||
                    Contains(nic.DefaultGateway, text) ||
                    nic.Addresses.Any(address => Contains(address.Address, text))));
        }

        var records = query.Select(device => new DeviceRecord(device)).ToList();
        DevicesGrid.ItemsSource = new ObservableCollection<DeviceRecord>(records);
        CatalogEmptyState.Visibility = records.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RecordCountText.Text = $"{records.Count} shown · {_devices.Count} total · {_devices.Count(device => device.IsManaged)} managed";

        var target = selectDeviceId.HasValue
            ? records.FirstOrDefault(record => record.Source.Id == selectDeviceId.Value)
            : records.FirstOrDefault();
        DevicesGrid.SelectedItem = target;
        if (target is null)
            DetailsPanel.DataContext = null;
        UpdateActionButtons();
    }

    private static bool Contains(string? source, string query) =>
        source?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private void DevicesLayoutGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 1040;
        if (compact == _isCompactLayout) return;
        _isCompactLayout = compact;
        if (compact)
        {
            DevicesLayoutGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            DevicesLayoutGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            DevicesLayoutGrid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
            DevicesLayoutGrid.RowDefinitions[0].Height = GridLength.Auto;
            DevicesLayoutGrid.RowDefinitions[1].Height = GridLength.Auto;
            Grid.SetRow(DetailsPanel, 1);
            Grid.SetColumn(DetailsPanel, 0);
            Grid.SetColumnSpan(DetailsPanel, 3);
            DevicesTableCard.Margin = new Thickness(0, 0, 0, 12);
            DetailsPanel.Margin = new Thickness(0);
            SearchBox.Width = 180;
            DevicesGrid.Columns[2].Visibility = Visibility.Collapsed;
            DevicesGrid.Columns[4].Visibility = Visibility.Collapsed;
        }
        else
        {
            DevicesLayoutGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            DevicesLayoutGrid.ColumnDefinitions[1].Width = new GridLength(14, GridUnitType.Pixel);
            DevicesLayoutGrid.ColumnDefinitions[2].Width = new GridLength(300, GridUnitType.Pixel);
            DevicesLayoutGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            DevicesLayoutGrid.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Pixel);
            Grid.SetRow(DetailsPanel, 0);
            Grid.SetColumn(DetailsPanel, 2);
            Grid.SetColumnSpan(DetailsPanel, 1);
            DevicesTableCard.Margin = new Thickness(0);
            DetailsPanel.Margin = new Thickness(0);
            SearchBox.Width = 220;
            DevicesGrid.Columns[2].Visibility = Visibility.Visible;
            DevicesGrid.Columns[4].Visibility = Visibility.Visible;
        }
    }

    private void RetryCatalog_Click(object sender, RoutedEventArgs e)
    {
        _isInitialized = false;
        DevicesView_Loaded(this, new RoutedEventArgs());
    }

    private void ClearDeviceFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        FilterComboBox.SelectedIndex = 0;
        ApplyFilter();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            ApplyFilter();
    }

    private void DevicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DetailsPanel.DataContext = DevicesGrid.SelectedItem as DeviceRecord;
        if (MotionSettings.Current.ReducedMotion)
        {
            DetailsPanel.BeginAnimation(UIElement.OpacityProperty, null);
            DetailsPanel.RenderTransform = Transform.Identity;
            DetailsPanel.Opacity = 1;
            UpdateActionButtons();
            return;
        }
        DetailsPanel.Opacity = 0.72;
        var slide = new TranslateTransform(7, 0);
        DetailsPanel.RenderTransform = slide;

        var fade = new DoubleAnimation(0.72, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DetailsPanel.BeginAnimation(UIElement.OpacityProperty, fade);

        var move = new DoubleAnimation(7, 0, TimeSpan.FromMilliseconds(170))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        slide.BeginAnimation(TranslateTransform.XProperty, move);
        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        var selected = DevicesGrid?.SelectedItem as DeviceRecord;
        EditDeviceButton.IsEnabled = selected is not null;
        StopManagingButton.IsEnabled = selected?.Source.IsManaged == true;
        ReactivateButton.IsEnabled = selected is not null && !selected.Source.IsManaged;
    }

    private async void NewDevice_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DeviceEditorWindow { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true || dialog.ResultDevice is null)
            return;

        await SaveDeviceAsync(dialog.ResultDevice);
    }

    private async void EditDevice_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRecord selected)
            return;

        var dialog = new DeviceEditorWindow(selected.Source)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true || dialog.ResultDevice is null)
            return;

        await SaveDeviceAsync(dialog.ResultDevice);
    }

    private async Task SaveDeviceAsync(ManagedDevice device)
    {
        try
        {
            var saved = await _catalog.SaveAsync(device);
            await ReloadDevicesAsync(saved.Id);
        }
        catch (DeviceValidationException ex)
        {
            MessageBox.Show(Window.GetWindow(this), string.Join(Environment.NewLine, ex.Errors),
                "Cannot save device", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this),
                $"The device could not be saved. Check that the database file is accessible.\n\n{ex.Message}",
                "Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StopManaging_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRecord selected)
            return;

        var result = MessageBox.Show(Window.GetWindow(this),
            $"Stop managing '{selected.Source.DeviceCode}'?\n\nThe record will be retained in the database and can be reactivated later. No network configuration will be changed.",
            "Stop managing device", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
            return;

        await SetManagedAsync(selected.Source.Id, false);
    }

    private async void Reactivate_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRecord selected)
            return;

        await SetManagedAsync(selected.Source.Id, true);
    }


    private void DevicesGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var current = e.OriginalSource as DependencyObject;
        while (current is not null && current is not DataGridRow)
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);

        if (current is DataGridRow row)
        {
            row.IsSelected = true;
            row.Focus();
        }
    }

    private void CopyDeviceCode_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRecord selected)
            return;

        try
        {
            Clipboard.SetText(selected.DeviceCode);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(Window.GetWindow(this), "The device code could not be copied to the clipboard.",
                "Clipboard unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CopyIpv4_Click(object sender, RoutedEventArgs e)
    {
        if (DevicesGrid.SelectedItem is not DeviceRecord selected || selected.IPv4 == "—" || string.IsNullOrWhiteSpace(selected.IPv4))
            return;

        var address = selected.IPv4.Split('/', StringSplitOptions.TrimEntries)[0];
        try
        {
            Clipboard.SetText(address);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show(Window.GetWindow(this), "The IPv4 address could not be copied to the clipboard.",
                "Clipboard unavailable", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async Task SetManagedAsync(Guid id, bool isManaged)
    {
        try
        {
            var updated = await _catalog.SetManagedAsync(id, isManaged);
            await ReloadDevicesAsync(updated.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), ex.Message, "Update inventory state failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
