using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.IO;
using WpfPath = System.Windows.Shapes.Path;

using LANRescue.ConsoleApp.Services;
using LANRescue.ConsoleApp.Views;
using LANRescue.Infrastructure.Network;
using LANRescue.Application.Services;
using LANRescue.Infrastructure.Persistence;

namespace LANRescue.ConsoleApp;

public partial class MainWindow : Window
{
    private readonly Button[] _navigationButtons;

    private string _activePageTitleKey = "Page_Dashboard";
    private readonly Dictionary<string, UserControl> _pageCache = new(StringComparer.Ordinal);
    private string? _activePageKey;
    private bool _isIconRail;

    public MainWindow()
    {
        InitializeComponent();
        FooterVersionText.Text = $"LAN Rescue v{AppVersion.Display} · Fresh Fluent";
        _navigationButtons = [
            DashboardNavButton,
            DevicesNavButton,
            IpConflictsNavButton,
            IpRecoveryNavButton,
            DiagnosticsNavButton,
            SettingsNavButton];
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        UpdateThemeToggleText();
        SizeChanged += MainWindow_SizeChanged;
        ApplyResponsiveShell(ActualWidth > 0 ? ActualWidth : Width);
        Navigate("Page_Dashboard", "dashboard",
            () => new DashboardView(), DashboardNavButton);
        LocalizationManager.LanguageChanged += LocalizationManager_LanguageChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowBackdrop.Apply(this, ThemeManager.CurrentTheme);
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
        LocalizationManager.LanguageChanged -= LocalizationManager_LanguageChanged;
        base.OnClosed(e);
    }

    private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
    {
        WindowBackdrop.Apply(this, ThemeManager.CurrentTheme);
        UpdateThemeToggleText();
    }

    private void UpdateThemeToggleText()
    {
        string key =
            ThemeManager.CurrentTheme == AppTheme.Dark
                ? "Theme_SwitchToLight"
                : "Theme_SwitchToDark";

        ThemeToggleText.SetResourceReference(
            TextBlock.TextProperty,
            key);
    }

    private void LocalizationManager_LanguageChanged(
    object? sender,
    EventArgs e)
    {
        PageTitleText.SetResourceReference(
            TextBlock.TextProperty,
            _activePageTitleKey);

        UpdateThemeToggleText();

        // Refresh collapsed Sidebar tooltips after label translation.
        ApplyResponsiveShell(ActualWidth);
    }

    private void Navigate(
        string titleResourceKey,
        string key,
        Func<UserControl> factory,
        Button activeButton)
    {
        _activePageTitleKey = titleResourceKey;

        if (!_pageCache.TryGetValue(key, out var view))
        {
            view = factory();
            _pageCache.Add(key, view);
        }

        // Use DynamicResource instead of a fixed English string.
        PageTitleText.SetResourceReference(
            TextBlock.TextProperty,
            titleResourceKey);

        if (!ReferenceEquals(PageHost.Content, view))
            PageHost.Content = view;

        foreach (var button in _navigationButtons)
        {
            button.ClearValue(Button.BackgroundProperty);
            button.FontWeight = FontWeights.Medium;
        }

        activeButton.SetResourceReference(
            Button.BackgroundProperty,
            "NavSelectedBrush");

        activeButton.FontWeight = FontWeights.SemiBold;

        // Preserve the existing page transition logic.
        if (MotionSettings.Current.ReducedMotion)
        {
            PageHost.BeginAnimation(UIElement.OpacityProperty, null);
            PageHost.RenderTransform = Transform.Identity;
            PageHost.Opacity = 1;
        }
        else if (_activePageKey != key)
        {
            PageHost.BeginAnimation(UIElement.OpacityProperty, null);
            PageHost.Opacity = 0;

            var slide = new TranslateTransform(0, 6);
            PageHost.RenderTransform = slide;

            var fade = new DoubleAnimation(
                0, 1, TimeSpan.FromMilliseconds(145))
            {
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var move = new DoubleAnimation(
                6, 0, TimeSpan.FromMilliseconds(165))
            {
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            PageHost.BeginAnimation(UIElement.OpacityProperty, fade);
            slide.BeginAnimation(TranslateTransform.YProperty, move);
        }

        _activePageKey = key;
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyResponsiveShell(e.NewSize.Width);

    private void ApplyResponsiveShell(double width)
    {
        // Keep the navigation usable on compact desktop windows while reserving the main canvas.
        bool iconRail = width < 1180;
        _isIconRail = iconRail;
        SidebarColumn.Width = new GridLength(iconRail ? 76 : width < 1440 ? 208 : 240);
        SidebarDockPanel.Margin = new Thickness(iconRail ? 10 : 18, 18, iconRail ? 10 : 18, 18);
        SidebarBrandText.Visibility = iconRail ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceSectionLabel.Visibility = iconRail ? Visibility.Collapsed : Visibility.Visible;
        LocalInventoryCard.Visibility = iconRail ? Visibility.Collapsed : Visibility.Visible;
        foreach (var button in _navigationButtons)
        {
            if (button.Content is StackPanel panel && panel.Children.Count >= 2)
            {
                var icon = (WpfPath)panel.Children[0];
                var label = (TextBlock)panel.Children[1];
                label.Visibility = iconRail ? Visibility.Collapsed : Visibility.Visible;
                icon.Margin = iconRail ? new Thickness(0) : new Thickness(0, 0, 12, 0);
                panel.HorizontalAlignment = iconRail ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                if (iconRail)
                {
                    button.ToolTip = label.Text;
                }
                else
                {
                    string tooltipKey = button == DashboardNavButton ? "Tooltip_Dashboard"
                        : button == DevicesNavButton ? "Tooltip_Devices"
                        : button == IpConflictsNavButton ? "Tooltip_IpConflicts"
                        : button == IpRecoveryNavButton ? "Tooltip_IpRecovery"
                        : button == DiagnosticsNavButton ? "Tooltip_Diagnostics"
                        : "Tooltip_Settings";
                    button.SetResourceReference(FrameworkElement.ToolTipProperty, tooltipKey);
                }
                button.HorizontalContentAlignment = iconRail ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            }
        }
        HeaderContentGrid.Margin = new Thickness(width < 1180 ? 14 : 24, 0, width < 1180 ? 14 : 24, 0);
        FooterContentPanel.Margin = new Thickness(width < 1180 ? 14 : 24, 0, width < 1180 ? 14 : 24, 0);
        FooterStatusText.Visibility = width < 1180 ? Visibility.Collapsed : Visibility.Visible;
        System.Windows.Application.Current.Resources["PageContentPadding"] = width < 1180
            ? new Thickness(16, 16, 16, 18)
            : width < 1500 ? new Thickness(20, 18, 20, 20) : new Thickness(28, 22, 28, 24);
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e) =>
        ThemeManager.SetTheme(ThemeManager.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

    private void Dashboard_Click(
    object sender,
    RoutedEventArgs e) =>
    Navigate(
        "Page_Dashboard",
        "dashboard",
        () => new DashboardView(),
        DashboardNavButton);

    private void Devices_Click(
        object sender,
        RoutedEventArgs e) =>
        Navigate(
            "Page_Devices",
            "devices",
            () => new DevicesView(),
            DevicesNavButton);
    private void IpConflicts_Click(
    object sender,
    RoutedEventArgs e)
    {
        Navigate("Page_IpConflicts", "conflicts", () =>
        {
            var databasePath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "LANRescue",
                "data",
                "lanrescue.db");

            var adapterProvider =
                new WindowsNetworkAdapterProvider();

            var arpProbe =
                new WindowsArpProbe();

            var repository =
                new SqliteIpConflictRepository(databasePath);

            var service =
                new IpConflictService(
                    adapterProvider,
                    arpProbe,
                    repository);

            return new IpConflictView(service);

        }, IpConflictsNavButton);
    }

    private void IpRecovery_Click(
        object sender,
        RoutedEventArgs e)
    {
        Navigate(
            "Page_IpRecovery",
            "recovery",
            () =>
            {
                var databasePath = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "LANRescue",
                    "data",
                    "lanrescue.db");

                // Persistence
                var repository =
                    new SqliteDeviceRepository(databasePath);

                // Application service
                var catalog =
                    new DeviceCatalogService(repository);

                // IP Recovery service
                var recoveryService =
                    new IpRecoveryService(catalog);

                // View receives the service
                return new IpRecoveryView(recoveryService);
            },
            IpRecoveryNavButton);
    }

    private void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        Navigate("Page_Diagnostics", "diagnostics", () =>
        {
            var adapterProvider = new WindowsNetworkAdapterProvider();
            var connectivityProbe = new WindowsConnectivityProbe();
            return new DiagnosticsView(new DiagnosticService(adapterProvider, connectivityProbe));
        }, DiagnosticsNavButton);
    }
    private void Settings_Click(
    object sender,
    RoutedEventArgs e) =>
    Navigate(
        "Page_Settings",
        "settings",
        () => new SettingsView(),
        SettingsNavButton);
}
