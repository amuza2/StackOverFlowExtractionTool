using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class BooleanToUnreadBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? "#FFF5F5" : "Transparent"; // Light red background for unread
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}