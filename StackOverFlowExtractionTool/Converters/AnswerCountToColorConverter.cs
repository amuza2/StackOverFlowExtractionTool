using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StackOverFlowExtractionTool.Converters;

public class AnswerCountToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int answerCount)
        {
            if (answerCount > 0) return new SolidColorBrush(0xFF4ADE80); // Success (green)
        }
        return new SolidColorBrush(0xFF94A3B8); // Secondary (gray)
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}