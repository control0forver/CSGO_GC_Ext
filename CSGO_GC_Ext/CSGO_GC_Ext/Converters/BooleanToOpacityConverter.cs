using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace CSGO_GC_Ext.Converters;

internal class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool)
            throw new ArgumentException($"Expected a boolean value, but received {value?.GetType().Name ?? "null"}.", nameof(value));

        return ((bool)value) ? 1d : 0d;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double or float)
            throw new ArgumentException($"Expected a double/float value, but received {value?.GetType().Name ?? "null"}.", nameof(value));

        return ((double)value) != 0;
    }
}
