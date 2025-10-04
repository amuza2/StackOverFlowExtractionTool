using System;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToMonitoringDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isMonitoring)
        {
            return isMonitoring ? "Monitoring is actively checking for new questions" : "Monitoring is currently paused";
        }
        return "Monitoring status unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}