using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace StackOverFlowExtractionTool.Converters;

public class IsMouseOverConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool isParentOver && values[1] is bool isButtonOver)
        {
            return isParentOver || isButtonOver;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}