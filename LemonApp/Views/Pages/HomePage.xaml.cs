using LemonApp.Services;
using LemonApp.Views.Windows;
using System.Windows;
using System.Windows.Controls;

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

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mainNavigationService.RequstNavigation(PageType.Notification,"Powered by TwlmGatito. Meowwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww!");
        }
    }
}
