using LemonApp.Services;
using LemonApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// RanklistPage.xaml 的交互逻辑
    /// </summary>
    public partial class RanklistPage : Page
    {
        private readonly MainNavigationService _mainNavigationService;
        private readonly RanklistPageViewModel vm;
        public RanklistPage(RanklistPageViewModel ranklistPageViewModel,MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            DataContext = vm = ranklistPageViewModel;
            _mainNavigationService = mainNavigationService;
            Loaded += RanklistPage_Loaded;
        }

        private async void RanklistPage_Loaded(object sender, RoutedEventArgs e)
        {
            _mainNavigationService.BeginLoadingAni();
            await vm.LoadData();
            _mainNavigationService.CancelLoadingAni();
        }
    }
}
