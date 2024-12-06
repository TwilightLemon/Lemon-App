using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LemonApp.Converters
{
    public class SubtractConverter:IValueConverter
    {
        public static readonly SubtractConverter Instance = new();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double originalWidth && double.TryParse(parameter.ToString(), out double subtractValue))
            {
                return originalWidth - subtractValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
