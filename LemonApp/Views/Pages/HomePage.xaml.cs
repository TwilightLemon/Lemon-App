using LemonApp.Services;
using System.Windows;
using System.Windows.Controls;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage(MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            _mainNavigationService = mainNavigationService;
        }
        private readonly MainNavigationService _mainNavigationService;

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _mainNavigationService.RequstNavigation(PageType.SearchPage,"Taylor Swift");
        }
    }
}
