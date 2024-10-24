using LemonApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LemonApp.Converters;

internal class MainMenuSourceConverter : IValueConverter
{
    public MainWindowViewModel.MenuType Type { get; set; }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is IEnumerable<MainWindowViewModel.MainMenu> menus)
        {
            return menus.Where(menus => menus.Type == Type);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
