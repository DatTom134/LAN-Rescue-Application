using System;
using System.Globalization;
using System.Windows.Data;
using LANRescue.ConsoleApp.Services;

namespace LANRescue.ConsoleApp.Converters;

/// <summary>Converts enum values to the current UI language without changing domain values.</summary>
public sealed class EnumLocalizationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue)
            return value?.ToString() ?? string.Empty;

        string key = $"Enum_{enumValue.GetType().Name}_{enumValue}";
        return LocalizationManager.GetString(key, enumValue.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
