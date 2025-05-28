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
                if (str.Length == 3) return str[^2..];
                int sub = rnd.Next(2,4);
                return str[^sub..];
            }
            var spilt = str.Split(' ');
            int choose = rnd.Next(0, 2);
            if (choose == 0)
            {
                return spilt.MaxBy(word => word.Length) ?? spilt[rnd.Next(spilt.Length-1)];
            }
            else if(choose ==1)
            {
                if (spilt.Length > 2)
                    return string.Join(' ',spilt[^2..]);
                else return spilt.Last();
            }
            else
            {
                return spilt[rnd.Next(spilt.Length - 1)];
            }
            return string.Empty;
        }
        return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
