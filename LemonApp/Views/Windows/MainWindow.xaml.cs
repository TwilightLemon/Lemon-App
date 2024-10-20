using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using LemonApp.Common.UIBases;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using static LemonApp.ViewModels.MainWindowViewModel;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LemonApp.MusicLib.Search;
using System.Net.Http;
using System.Windows.Input;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindowBase
    {
        public MainWindow( 
           MainWindowViewModel mainWindowViewModel,
            IServiceProvider serviceProvider,
            MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            DataContext = _vm = mainWindowViewModel;
            _serviceProvider = serviceProvider;
            _mainNavigationService = mainNavigationService;
            _mainNavigationService.OnNavigatingRequsted += MainNavigationService_OnNavigatingRequsted;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.SelectedMenu = _vm.MainMenus.FirstOrDefault();
        }

        private readonly MainWindowViewModel _vm;
        private readonly IServiceProvider _serviceProvider;
        private readonly MainNavigationService _mainNavigationService;

        private void MainNavigationService_OnNavigatingRequsted(PageType type, object? arg)
        {
            switch (type)
            {
                case PageType.SettingsPage:
                    NavigateToSettingsPage();
                    break;
                case PageType.AlbumPage:
                    if (arg is string{ } id)
                        NavigateToAlbumPage(id);
                    break;
                case PageType.SearchPage:
                    if(arg is string { } keyword)
                        NavigateToSearchPage(keyword);
                    break;
                default:
                    break;
            }
        }

        private void NavigateToSettingsPage()
        {
            var sp = _serviceProvider.GetRequiredService<SettingsPage>();
            if (sp != null)
            {
                MainContentFrame.Navigate(sp);
            }
            _vm.SelectedMenu = null;
        }
        private void NavigateToAlbumPage(string AlbumId)
        {
            var sp = _serviceProvider.GetRequiredService<PlaylistPage>();
            PlaylistPageViewModel vm = _serviceProvider.GetRequiredService<PlaylistPageViewModel>();
            if (sp != null)
            {
/*                vm.Cover = new ImageBrush(new BitmapImage(new Uri(info.Photo)));
                vm.CreatorAvatar = info.Creator != null ? new ImageBrush(new BitmapImage(new Uri(info.Creator.Photo))) : null;
                vm.CreatorName = info.Creator != null ? info.Creator.Name : "";
                vm.Description = info.Description ?? "";
                vm.ListName = info.Name;*/

                sp.ViewModel = vm;
                MainContentFrame.Navigate(sp);
            }
            _vm.SelectedMenu = null;
        }
        private async void NavigateToSearchPage(string keyword)
        {
            if(string.IsNullOrWhiteSpace(keyword))
                return;

            var sp = _serviceProvider.GetRequiredService<PlaylistPage>();
            var hc = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
            var auth = _serviceProvider.GetRequiredService<UserProfileService>().GetAuth();
            if (sp != null&&hc!=null&&auth!=null)
            {
                PlaylistPageViewModel vm = new()
                {
                    ShowInfoView = false
                };
                var search = await SearchAPI.SearchMusicAsync(hc, auth, keyword);
                foreach(var item in search)
                {
                    vm.Musics.Add(item);
                }

                sp.ViewModel = vm;
                MainContentFrame.Navigate(sp);
            }
            _vm.SelectedMenu = null;
        }


            /// <summary>
            /// 打开UserProfile菜单
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private async void UserProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            var popup = _serviceProvider.GetRequiredService<UserMenuPopupWindow>();
            popup.RequestClose = () => {
                UserProfilePopup.IsOpen = false;
            };
            UserProfilePopup.Child = popup;
            UserProfilePopup.HorizontalOffset = -popup.Width;
            await Task.Yield();
            UserProfilePopup.IsOpen = true;
        }

        /// <summary>
        /// 打开音量调节
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AudioBtn_Click(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            AudioAdjustPopup.IsOpen = true;
        }

        private Storyboard? _openLyricPageAni, _closeLyricPageAni;

        private void MainPageMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.CurrentPage is { } page&&_vm.CurrentPage!=MainContentFrame.Content)
            {
                MainContentFrame.Navigate(_vm.CurrentPage);
            }
        }

        private void GoBackBtn_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.GoBack();
        }

        private Storyboard? _showGoBackBtnAni = null;
        private bool _isGoBackBtnShow = false;
        private void MainContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            _showGoBackBtnAni ??= (Storyboard)Resources["ShowGoBackBtnAni"];
            if (MainContentFrame.CanGoBack)
            {
                if (!_isGoBackBtnShow)
                {
                    _showGoBackBtnAni.Begin();
                    _isGoBackBtnShow = true;
                }
            }
            else
            {
                _showGoBackBtnAni.Stop();
                _isGoBackBtnShow = false;
            }

            if (MainContentFrame.CanGoForward)
            {
                //处理来自GoBack的Navigation
                _vm.CurrentPage = MainContentFrame.Content;
                var selected = _vm.MainMenus.FirstOrDefault(page => page.PageType == e.Content?.GetType());
                //退回时不生成新页面
                if (selected != null)
                    selected.RequireCreateNewPage = false;
                _vm.SelectedMenu= selected;
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _mainNavigationService.RequstNavigation(PageType.SearchPage, SearchBox.Text);
        }

        /// <summary>
        /// 打开/关闭歌词页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MusicControl_Info_Click(object sender, RoutedEventArgs e)
        {
            _openLyricPageAni ??= (Storyboard)Resources["OpenLyricPageAni"];
            _closeLyricPageAni ??= (Storyboard)Resources["CloseLyricPageAni"];
            if (_vm.IsLyricPageOpen)
            {
                _closeLyricPageAni.Begin();
                _vm.IsLyricPageOpen = false;
            }
            else
            {
                var point = MusicControl_Img.TranslatePoint(new Point(0, 0), this);
                (_closeLyricPageAni.Children[4] as ThicknessAnimationUsingKeyFrames)!.KeyFrames[0].Value =
                (_openLyricPageAni.Children[6] as ThicknessAnimationUsingKeyFrames)!.KeyFrames[0].Value =
                            new Thickness(point.X, (double)656 / 681 * point.Y, 0, 0);
                _openLyricPageAni.Begin();
                _vm.IsLyricPageOpen=true;

            }
        }


        // private void Border_MouseDown(object sender, MouseButtonEventArgs e){
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColorMode=Appearence.AccentColorType.Custome;
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColor=Colors.LightYellow;

        //     _uiResourceService.UpdateAccentColor();
        // }
    }
}