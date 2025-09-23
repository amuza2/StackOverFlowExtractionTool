using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class StringEqualityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString() ? "#1976D2" : "#E3F2FD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}