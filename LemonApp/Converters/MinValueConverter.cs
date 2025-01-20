using System;
using System.Globalization;
using System.Windows.Data;

namespace LemonApp.Converters;
public class MinValueConverter : IMultiValueConverter
{
    public static readonly MinValueConverter Instance = new();
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double width && values[1] is double height)
        {
            return Math.Min(width, height);
        }
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}