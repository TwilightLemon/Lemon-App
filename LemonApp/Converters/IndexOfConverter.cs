using LemonApp.MusicLib.Abstraction.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace LemonApp.Converters;

public class IndexOfConverter : IMultiValueConverter
{
    public static readonly IndexOfConverter Instance = new();
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is Music m &&values[1] is IList<Music> list)
        {
            return (list.IndexOf(m) + 1).ToString();
        }
        return string.Empty;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
