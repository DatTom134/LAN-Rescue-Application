using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using LANRescue.ConsoleApp.Services;
using LANRescue.ConsoleApp.Views;
using LANRescue.Infrastructure.Network;
using LANRescue.Application.Services;

namespace LANRescue.ConsoleApp;

public partial class MainWindow : Window
{
    private readonly Button[] _navigationButtons;

    public MainWindow()
    {
        InitializeComponent();
        _navigationButtons =
        [
            DashboardNavButton,
            DevicesNavButton,
            IpConflictsNavButton,
            IpRecoveryNavButton,
            DiagnosticsNavButton,
            SettingsNavButton
        ];

        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        UpdateThemeToggleText();
        Navigate("Dashboard", new DashboardView(), DashboardNavButton);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowBackdrop.Apply(this, ThemeManager.CurrentTheme);
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
        base.OnClosed(e);
    }

    private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
    {
        WindowBackdrop.Apply(this, ThemeManager.CurrentTheme);
        UpdateThemeToggleText();
    }

    private void UpdateThemeToggleText()
    {
        ThemeToggleButton.Content = ThemeManager.CurrentTheme == AppTheme.Dark ? "☼  Light" : "◐  Dark";
    }

    private void Navigate(string title, UIElement view, Button activeButton)
    {
        PageTitleText.Text = title;
        PageHost.Content = view;
        PageHost.Opacity = 0;
        var slide = new TranslateTransform(0, 7);
        PageHost.RenderTransform = slide;

        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        PageHost.BeginAnimation(UIElement.OpacityProperty, fade);

        var move = new DoubleAnimation(7, 0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        slide.BeginAnimation(TranslateTransform.YProperty, move);

        foreach (var button in _navigationButtons)
        {
            button.ClearValue(Button.BackgroundProperty);
            button.FontWeight = FontWeights.Medium;
        }

        activeButton.SetResourceReference(Button.BackgroundProperty, "NavSelectedBrush");
        activeButton.FontWeight = FontWeights.SemiBold;
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.SetTheme(ThemeManager.CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e) =>
        Navigate("Dashboard", new DashboardView(), DashboardNavButton);

    private void Devices_Click(object sender, RoutedEventArgs e) =>
        Navigate("Devices", new DevicesView(), DevicesNavButton);

    private void IpConflicts_Click(object sender, RoutedEventArgs e) =>
        Navigate("IP Conflicts", new IpConflictView(), IpConflictsNavButton);

    private void IpRecovery_Click(object sender, RoutedEventArgs e) =>
        Navigate("IP Recovery", new IpRecoveryView(), IpRecoveryNavButton);

    private void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        var adapterProvider = new WindowsNetworkAdapterProvider();

        var connectivityProbe = new WindowsConnectivityProbe();

        var diagnosticService = new DiagnosticService(
            adapterProvider,
            connectivityProbe);

        Navigate(
            "Diagnostics",
            new DiagnosticsView(diagnosticService),
            DiagnosticsNavButton);
    }

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        Navigate("Settings", new SettingsView(), SettingsNavButton);
}
