using System;
using System.Globalization;
using System.Windows.Data;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Converters;

public class MusicEqualConverter : IMultiValueConverter
{
    public static readonly MusicEqualConverter Instance = new();
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is Music{ } currentMusic && values[1] is Music{ } playingMusic)
        {
            return currentMusic.MusicID == playingMusic.MusicID;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
