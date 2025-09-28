using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToMonitoringBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isMonitoring)
        {
            return isMonitoring ? new SolidColorBrush(0xFF065F46) : new SolidColorBrush(0xFF374151); // Dark green : Dark gray
        }
        return new SolidColorBrush(0xFF374151);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



