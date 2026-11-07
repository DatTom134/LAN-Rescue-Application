using System.Windows;
using System.Windows.Controls;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Views;

public partial class SettingsView : UserControl
{
    private bool _isInitializingThemeSelector;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
        Unloaded += SettingsView_Unloaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        _isInitializingThemeSelector = true;
        ThemeSelector.SelectedIndex = ThemeManager.CurrentTheme == AppTheme.Dark ? 1 : 0;
        _isInitializingThemeSelector = false;
    }

    private void SettingsView_Unloaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _isInitializingThemeSelector || ThemeSelector.SelectedItem is not ComboBoxItem item ||
            !Enum.TryParse<AppTheme>(item.Tag?.ToString(), true, out var theme))
        {
            return;
        }

        ThemeManager.SetTheme(theme);
    }

    private void ThemeManager_ThemeChanged(object? sender, EventArgs e)
    {
        _isInitializingThemeSelector = true;
        ThemeSelector.SelectedIndex = ThemeManager.CurrentTheme == AppTheme.Dark ? 1 : 0;
        _isInitializingThemeSelector = false;
    }
}
