using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MediaDownloader.Helpers;

public class ComparisonConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return parameter?.ToString() ?? string.Empty;
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
