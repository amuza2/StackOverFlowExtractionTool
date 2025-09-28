using System;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToMonitoringButtonColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isMonitoring)
        {
            return isMonitoring ? "#DC2626" : "#059669"; // Red for stop, green for start
        }
        return "#6B7280";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}