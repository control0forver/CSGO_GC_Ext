using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace CSGO_GC_Ext.Converters;

public class RarityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string rarity)
        {
            return rarity switch
            {
                "common" => Brushes.Gray,
                "uncommon" => Brushes.Green,
                "rare" => Brushes.Blue,
                "epic" => Brushes.Purple,
                "legendary" => Brushes.Orange,
                _ => Brushes.White
            };
        }
        return Brushes.White;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RarityToClassConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string rarity)
        {
            return rarity switch
            {
                "epic" => "epic-item",
                "legendary" => "legendary-item",
                _ => ""
            };
        }
        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}