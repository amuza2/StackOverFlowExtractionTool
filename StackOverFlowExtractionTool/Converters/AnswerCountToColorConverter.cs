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
            if (answerCount > 0) return Brushes.Green;
        }
        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}