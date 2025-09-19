using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class GreaterThanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        try
        {
            double number = System.Convert.ToDouble(value, culture);
            double threshold = System.Convert.ToDouble(parameter, culture);

            return number > threshold;
        }
        catch
        {
            return false;
        }
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}