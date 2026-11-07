using System.IO;
using System.Windows;

namespace LANRescue.ConsoleApp.Services;

public enum AppTheme
{
    Light,
    Dark
}

/// <summary>
/// Loads the selected theme dictionary and persists the user's choice per Windows profile.
/// </summary>
public static class ThemeManager
{
    private static readonly string ThemeSettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LANRescue", "settings", "theme.txt");

    public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public static event EventHandler? ThemeChanged;

    public static void Initialize()
    {
        var theme = AppTheme.Light;
        try
        {
            if (File.Exists(ThemeSettingsPath) &&
                Enum.TryParse<AppTheme>(File.ReadAllText(ThemeSettingsPath).Trim(), true, out var savedTheme) &&
                Enum.IsDefined(typeof(AppTheme), savedTheme))
            {
                theme = savedTheme;
            }
        }
        catch (IOException)
        {
            // A missing/unavailable preference file must never prevent application startup.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the default theme if the preference cannot be read.
        }

        Apply(theme, persist: false, raiseEvent: false);
    }

    public static void SetTheme(AppTheme theme) => Apply(theme, persist: true, raiseEvent: true);

    private static void Apply(AppTheme theme, bool persist, bool raiseEvent)
    {
        if (System.Windows.Application.Current is null)
            return;

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var themeDictionaryIndex = -1;
        for (var i = 0; i < dictionaries.Count; i++)
        {
            var source = dictionaries[i].Source?.OriginalString;
            if (source?.Contains("Colors.Light.xaml", StringComparison.OrdinalIgnoreCase) == true ||
                source?.Contains("Colors.Dark.xaml", StringComparison.OrdinalIgnoreCase) == true)
            {
                themeDictionaryIndex = i;
                break;
            }
        }

        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"/LANRescue.Console;component/Resources/Colors.{theme}.xaml", UriKind.Relative)
        };

        if (themeDictionaryIndex >= 0)
            dictionaries[themeDictionaryIndex] = dictionary;
        else
            dictionaries.Insert(0, dictionary);

        var changed = CurrentTheme != theme;
        CurrentTheme = theme;

        if (persist)
        {
            try
            {
                var directory = Path.GetDirectoryName(ThemeSettingsPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(ThemeSettingsPath, theme.ToString());
            }
            catch (IOException)
            {
                // Theme changes remain active for this session even if preferences cannot be saved.
            }
            catch (UnauthorizedAccessException)
            {
                // Theme changes remain active for this session even if preferences cannot be saved.
            }
        }

        if (raiseEvent && changed)
            ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
