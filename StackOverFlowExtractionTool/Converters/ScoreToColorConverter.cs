using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace StackOverFlowExtractionTool.Converters;

public class ScoreToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int score)
        {
            if (score > 0) return Brushes.Green;
            if (score < 0) return Brushes.Red;
        }
        return Brushes.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}