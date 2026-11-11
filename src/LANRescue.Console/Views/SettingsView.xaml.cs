
using System.Windows;
using System.Windows.Controls;

using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Views;

public partial class SettingsView : UserControl
{
    private bool _isInitializingThemeSelector;
    private bool _isInitializingLanguageSelector;

    public SettingsView()
    {
        InitializeComponent();

        VersionText.Text =
            LocalizationManager.Format("Settings_VersionValue", AppVersion.Display);

        Loaded += SettingsView_Loaded;
        Unloaded += SettingsView_Unloaded;
    }

    private void SettingsView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        LocalizationManager.LanguageChanged +=
            LocalizationManager_LanguageChanged;

        SyncThemeSelector();
        SyncLanguageSelector();

        ReducedMotionCheckBox.IsChecked =
            MotionSettings.Current.ReducedMotion;

        MotionSettings.Current.PropertyChanged +=
            MotionSettings_PropertyChanged;
    }

    private void SettingsView_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;

        LocalizationManager.LanguageChanged -=
            LocalizationManager_LanguageChanged;

        MotionSettings.Current.PropertyChanged -=
            MotionSettings_PropertyChanged;
    }

    // =========================================================
    // LANGUAGE
    // =========================================================

    private void SyncLanguageSelector()
    {
        _isInitializingLanguageSelector = true;

        LanguageSelector.SelectedValue =
            LocalizationManager.CurrentLanguage.ToString();

        _isInitializingLanguageSelector = false;
    }

    private void LanguageSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded ||
            _isInitializingLanguageSelector ||
            LanguageSelector.SelectedValue is not string value)
        {
            return;
        }

        if (Enum.TryParse<AppLanguage>(
                value,
                ignoreCase: true,
                out var language))
        {
            LocalizationManager.SetLanguage(language);
        }
    }

    private void LocalizationManager_LanguageChanged(
        object? sender,
        EventArgs e)
    {
        SyncLanguageSelector();
    }

    // =========================================================
    // THEME
    // =========================================================

    private void SyncThemeSelector()
    {
        _isInitializingThemeSelector = true;

        ThemeSelector.SelectedIndex =
            ThemeManager.CurrentTheme == AppTheme.Dark ? 1 : 0;

        _isInitializingThemeSelector = false;
    }

    private void ThemeSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded ||
            _isInitializingThemeSelector ||
            ThemeSelector.SelectedItem is not ComboBoxItem item ||
            !Enum.TryParse<AppTheme>(
                item.Tag?.ToString(),
                true,
                out var theme))
        {
            return;
        }

        ThemeManager.SetTheme(theme);
    }

    private void ThemeManager_ThemeChanged(
        object? sender,
        EventArgs e)
    {
        SyncThemeSelector();
    }

    // =========================================================
    // REDUCED MOTION
    // =========================================================

    private void ReducedMotionCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (IsLoaded &&
            ReducedMotionCheckBox.IsChecked.HasValue)
        {
            MotionSettings.Current.ReducedMotion =
                ReducedMotionCheckBox.IsChecked.Value;
        }
    }

    private void MotionSettings_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(MotionSettings.ReducedMotion))
        {
            ReducedMotionCheckBox.IsChecked =
                MotionSettings.Current.ReducedMotion;
        }
    }
}
