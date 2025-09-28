using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isAnswered)
        {
            return isAnswered ? new SolidColorBrush(0xFF4ADE80) : new SolidColorBrush(0xFF94A3B8); // Success : Secondary
        }
        return new SolidColorBrush(0xFFE2E8F0); // Primary text
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}