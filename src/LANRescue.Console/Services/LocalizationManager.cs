
using System;
using System.Globalization;
using System.IO;
using System.Windows;

namespace LANRescue.ConsoleApp.Services;

public enum AppLanguage
{
    English,
    Vietnamese
}

/// <summary>
/// Centralized runtime localization for the LAN Rescue WPF application.
/// </summary>
public static class LocalizationManager
{
    private static readonly string LanguageSettingsPath = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "LANRescue",
        "settings",
        "language.txt");

    public static AppLanguage CurrentLanguage { get; private set; }
        = AppLanguage.English;

    public static event EventHandler? LanguageChanged;

    public static void Initialize()
    {
        AppLanguage language = AppLanguage.English;

        try
        {
            if (File.Exists(LanguageSettingsPath))
            {
                string savedValue =
                    File.ReadAllText(LanguageSettingsPath).Trim();

                if (Enum.TryParse<AppLanguage>(
                        savedValue,
                        ignoreCase: true,
                        out var savedLanguage) &&
                    Enum.IsDefined(typeof(AppLanguage), savedLanguage))
                {
                    language = savedLanguage;
                }
            }
        }
        catch (IOException)
        {
            // Keep the default language if the preference is unavailable.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the default language if the preference cannot be read.
        }

        ApplyLanguage(
            language,
            persist: false,
            raiseEvent: false);
    }

    public static void SetLanguage(AppLanguage language)
    {
        if (!Enum.IsDefined(typeof(AppLanguage), language))
            throw new ArgumentOutOfRangeException(nameof(language));

        ApplyLanguage(
            language,
            persist: true,
            raiseEvent: true);
    }

    public static string GetString(
        string key,
        string? fallback = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var application = System.Windows.Application.Current;

        if (application?.TryFindResource(key) is string value)
            return value;

        return fallback ?? $"[{key}]";
    }

    public static string Format(
        string key,
        params object[] arguments)
    {
        string format = GetString(key);

        return string.Format(
            CultureInfo.InvariantCulture,
            format,
            arguments);
    }

    private static void ApplyLanguage(
        AppLanguage language,
        bool persist,
        bool raiseEvent)
    {
        var application = System.Windows.Application.Current;

        if (application is null)
            return;

        string languageCode =
            language == AppLanguage.Vietnamese
                ? "vi"
                : "en";

        // Create the new dictionary before changing the active one.
        var newDictionary = new ResourceDictionary
        {
            Source = new Uri(
                $"/LANRescue.Console;component/Resources/Strings.{languageCode}.xaml",
                UriKind.Relative)
        };

        var dictionaries =
            application.Resources.MergedDictionaries;

        int existingIndex = -1;

        for (int i = 0; i < dictionaries.Count; i++)
        {
            string source =
                dictionaries[i].Source?.OriginalString ?? "";

            if (source.Contains(
                    "Strings.en.xaml",
                    StringComparison.OrdinalIgnoreCase) ||
                source.Contains(
                    "Strings.vi.xaml",
                    StringComparison.OrdinalIgnoreCase))
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            dictionaries[existingIndex] = newDictionary;
        }
        else
        {
            dictionaries.Add(newDictionary);
        }

        bool changed = CurrentLanguage != language;

        CurrentLanguage = language;

        if (persist)
            SavePreference(language);

        if (raiseEvent && changed)
            LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void SavePreference(AppLanguage language)
    {
        try
        {
            string? directory =
                Path.GetDirectoryName(LanguageSettingsPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(
                LanguageSettingsPath,
                language.ToString());
        }
        catch (IOException)
        {
            // Language still changes for the current session.
        }
        catch (UnauthorizedAccessException)
        {
            // Language still changes for the current session.
        }
    }
}
