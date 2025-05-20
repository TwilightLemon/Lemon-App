using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LemonApp.Converters;

public class WordRandomSpiltConverter : IValueConverter
{
    public static readonly WordRandomSpiltConverter Instance = new();
    public readonly Random rnd = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is string { Length:> 0 } str)
        {
            if(!str.Contains(' '))
            {
                int a = rnd.Next(str.Length - 1);
                int b = rnd.Next(str.Length - 1);
                return a > b?str[b..a] :str[a..b];
            }
            var spilt = str.Split(' ');
            if (rnd.Next(0, 2) == 0)
            {
                return spilt.MaxBy(word => word.Length) ?? spilt[rnd.Next(spilt.Length-1)];
            }
            else
            {
                return spilt.Last();
            }
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
