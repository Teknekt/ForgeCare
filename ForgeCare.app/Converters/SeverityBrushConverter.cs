using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ForgeCare.App.Converters;

public sealed class SeverityBrushConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        string severity =
            value?.ToString()?.Trim().ToUpperInvariant()
            ?? string.Empty;

        return severity switch
        {
            "CRITICAL" or "HIGH" =>
                new SolidColorBrush(Color.FromRgb(225, 80, 80)),

            "ATTENTION" or "MEDIUM" or "MODERATE" =>
                new SolidColorBrush(Color.FromRgb(225, 170, 60)),

            "HEALTHY" or "NORMAL" or "GOOD" =>
                new SolidColorBrush(Color.FromRgb(110, 190, 140)),

            "LOW" or "INFO" or "MINIMAL" =>
                new SolidColorBrush(Color.FromRgb(90, 150, 230)),

            _ =>
                new SolidColorBrush(Color.FromRgb(140, 148, 157))
        };
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
