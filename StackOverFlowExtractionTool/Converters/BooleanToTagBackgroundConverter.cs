using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToTagBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "#1976D2" : "#E3F2FD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}