using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DestinyPOS2026.Wpf.Helpers;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? text = value as string;
        return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
