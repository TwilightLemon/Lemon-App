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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _mainNavigationService.RequstNavigation(PageType.AlbumPage, new AlbumInfo()
            {
                Creator = new Profile()
                {
                    Name = "Taylor Swift",
                    Mid = "",
                    Photo = "https://y.gtimg.cn/music/photo_new/T001R150x150M000000qrPik2w6lDr_24.jpg"
                },
                Description = "desc.....",
                Id = "123",
                Name = "Fortnight",
                Photo = "https://y.gtimg.cn/music/photo_new/T002R300x300M000001YsT7x0M11By_1.jpg",
                Source = Platform.qq
            });
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _mainNavigationService.RequstNavigation(PageType.SearchPage,"Taylor Swift");
        }
    }
}
