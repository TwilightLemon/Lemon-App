using LemonApp.Common.Configs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace LemonApp.Converters;

[ValueConversion(typeof(Enum), typeof(Geometry))]
public class CircleModeToPathDataConverter : IValueConverter
{
    public static CircleModeToPathDataConverter Instance { get; } = new CircleModeToPathDataConverter();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PlayingPreference.CircleMode mode)
        {
            return (Geometry)App.Current.FindResource($"CircleMode_{mode switch {
                PlayingPreference.CircleMode.Circle =>"Circle",
                PlayingPreference.CircleMode.Single => "Single",
                PlayingPreference.CircleMode.Random => "Random",
                _=>throw new InvalidOperationException("Unknown CircleMode")
            }}");
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}
