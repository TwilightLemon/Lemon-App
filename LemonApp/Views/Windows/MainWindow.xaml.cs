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
using Microsoft.Extensions.DependencyInjection;
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
           MainNavigationService mainNavigationService,
           IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _mainNavigationService = mainNavigationService;
            DataContext = _vm = mainWindowViewModel;
            _vm.RequireNavigateToPage += Vm_RequireNavigateToPage;
            Loaded += MainWindow_Loaded;
        }
        private readonly IServiceProvider _serviceProvider;
        private readonly MainNavigationService _mainNavigationService;
        private readonly MainWindowViewModel _vm;

        #region MainContentFrame
        /// <summary>
        /// ViewModel请求跳转页面
        /// </summary>
        /// <param name="page"></param>
        private void Vm_RequireNavigateToPage(Page page)
        {
            MainContentFrame.Navigate(page);
        }
        /// <summary>
        /// Loaded->Load First Page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.SelectedMenu = _vm.MainMenus.FirstOrDefault();
        }
        /// <summary>
        /// Menu Selected -> Navigate to some page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainPageMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vm.CurrentPage is { } page && _vm.CurrentPage != MainContentFrame.Content)
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
            //GoBackBtn Animation
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

            //sync page with menu
            if (MainContentFrame.CanGoForward)
            {
                //处理来自GoBack的Navigation
                _vm.CurrentPage = MainContentFrame.Content;
                var selected = _vm.MainMenus.FirstOrDefault(page => page.PageType == e.Content?.GetType());
                //退回时不生成新页面
                if (selected != null)
                    selected.RequireCreateNewPage = false;
                _vm.SelectedMenu = selected;
            }
        }
        #endregion

        #region  Open Popups

        /// <summary>
        /// 打开UserProfile菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UserProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            var popup = _serviceProvider.GetRequiredService<UserMenuPopupWindow>();
            popup.RequestClose = () =>
            {
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
                _vm.IsLyricPageOpen = true;

            }
        }
        #endregion
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _mainNavigationService.RequstNavigation(PageType.SearchPage, SearchBox.Text);
        }

        // private void Border_MouseDown(object sender, MouseButtonEventArgs e){
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColorMode=Appearence.AccentColorType.Custome;
        //     _appSettingsService.GetConfigMgr<Appearence>().Data.AccentColor=Colors.LightYellow;

        //     _uiResourceService.UpdateAccentColor();
        // }
    }
}