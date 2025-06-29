using EleCho.WpfSuite;
using LemonApp.Common.UIBases;
using LemonApp.Components;
using LemonApp.MusicLib.Search;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;
using LemonApp.Views.UserControls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

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
           IServiceProvider serviceProvider,
           UIResourceService uiResourceService,
           PublicPopupMenuHolder publicPopupMenuHolder)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _mainNavigationService = mainNavigationService;
            _uiResourceService= uiResourceService;
            _uiResourceService.OnColorModeChanged += _uiResourceService_OnColorModeChanged;
            _publicPopupMenuHolder = publicPopupMenuHolder;

            DataContext = _vm = mainWindowViewModel;
            _vm.RequestNavigateToPage += Vm_RequireNavigateToPage;
            _vm.RequestNotify += Vm_RequestNotify;
            _vm.SyncCurrentPlayingWithPlayListPage += Vm_SyncCurrentPlayingWithPlayListPage;
            _vm.CacheStarted = Vm_CacheBegin;
            _vm.CacheFinished = Vm_CacheFinished;


            Loaded += MainWindow_Loaded;
        }

        private readonly PublicPopupMenuHolder _publicPopupMenuHolder;
        private readonly IServiceProvider _serviceProvider;
        private readonly UIResourceService _uiResourceService;
        private readonly MainNavigationService _mainNavigationService;
        private readonly MainWindowViewModel _vm;

        /// <summary>
        /// Show Notification
        /// </summary>
        /// <param name="obj"></param>
        private void Vm_RequestNotify(string obj)
        {
            App.Current.MainWindow.Dispatcher.Invoke(async () =>
            {
                NotificationBox.IsOpen = true;
                NotificationTb.Text = obj;
                await Task.Delay(4000);
                NotificationBox.IsOpen = false;
            });
        }

        public void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void _uiResourceService_OnColorModeChanged()
        {
            bool isdarkmode = _uiResourceService.GetIsDarkMode();
            //Im_Effect.Color = isdarkmode ? Colors.White : Colors.Black;
            var color = isdarkmode ? Color.FromRgb(0x1E, 0x1E, 0x1E) : Color.FromRgb(0xD0,0xD0,0xD0);
            WindowOption.SetBorderColor(App.Current.MainWindow, new WindowOptionColor() { R = color.R, G = color.G, B = color.B });
        }

        #region MainContentFrame
        private void Vm_SyncCurrentPlayingWithPlayListPage(string mid)
        {
            if(MainContentFrame.Content is PlaylistPage page&&page.ViewModel is { } vm)
            {
                vm.UpdateCurrentPlaying(mid);
            }
        }
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
            _vm.RequireCreateNewPage();
            LyricViewHost.Child = _vm.LyricView;
            _uiResourceService_OnColorModeChanged();

            //disabled for performance issue
           //visualizer.Player = _serviceProvider.GetRequiredService<MediaPlayerService>().Player;
            MainContentPage.Children.Add(_publicPopupMenuHolder.selector);
        }

        //以下是歌词页背景的旋转动画，，但是写的很烂，改日再改。🐱
        private Storyboard? LyricImgRTAni;
       public void BeginOrPauseLyricImgAnimation(bool play)
        {
            //LyricImgRT
            if(LyricImgRTAni == null)
            {
                LyricImgRTAni = new();
                DoubleAnimation da = new(0, 360, TimeSpan.FromSeconds(15))
                {
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(da, LyricImgRT);
                Storyboard.SetTargetProperty(da, new PropertyPath("(RotateTransform.Angle)"));
                LyricImgRTAni.Children.Add(da);
                LyricImgRTAni.Freeze();
                LyricImgRTAni.Begin();
            }
            else
            {
                if(play)
                {
                    LyricImgRTAni.Resume();
                }
                else
                {
                    LyricImgRTAni.Pause();
                }
            }

        }

        private void GoBackBtn_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.GoBack();
        }

        private void MainContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Content is Page content)
            {
                var trans = new TranslateTransform() { Y = e.NavigationMode==NavigationMode.Back ? -140 : 140 };
                content.RenderTransform = trans;
                //Transition Animation
                var ani = new DoubleAnimation()
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase()
                };
                ani.Completed += delegate { content.RenderTransform = null; };
                trans.BeginAnimation(TranslateTransform.YProperty, ani);
            }
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
            if (MainContentFrame.CanGoForward&& MainContentFrame.Content is Page { } page)
            {
                //处理来自GoBack的Navigation
                var selected = page.Tag as MainWindowViewModel.MainMenu;
                _vm.SelectedMenu = selected;
            }

        }
        #endregion

        #region LyricPage
        private void SwitchLrcImmerseModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchLrcImmerseModeBtn.IsChecked == true)
            {
                LyricPage_Main.Visibility = Visibility.Collapsed;
                LyricPage_Immerse.Visibility = Visibility.Visible;
            }
            else
            {
                LyricPage_Main.Visibility = Visibility.Visible;
                LyricPage_Immerse.Visibility = Visibility.Collapsed;
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
            if (UserProfilePopup.Child == null)
            {
                var popup = _serviceProvider.GetRequiredService<UserMenuPopupWindow>();
                popup.RequestCloseMenu = () =>
                {
                    UserProfilePopup.IsOpen = false;
                    UserProfilePopup.Child = null;
                };
                UserProfilePopup.Child = popup;
                UserProfilePopup.HorizontalOffset = -popup.Width;
            }
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
        /// 打开歌词页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MusicControl_Info_Click(object sender, RoutedEventArgs e)
        {
            _openLyricPageAni ??= (Storyboard)Resources["OpenLyricPageAni"];
            double offset = this.ActualHeight - MusicControl.ActualHeight;
            if (_openLyricPageAni.Children[0]is ThicknessAnimationUsingKeyFrames frames && frames.KeyFrames[0] is EasingThicknessKeyFrame frame)
            {
                frame.Value = new Thickness(0, offset, 0, -offset);
            }
            if (!_vm.IsLyricPageOpen)
            {
                _openLyricPageAni.Begin();
                _vm.IsLyricPageOpen = true;
            }
        }
        private void LyricPage_BackBtn_Click(object sender, RoutedEventArgs e)
        {
            _closeLyricPageAni ??= (Storyboard)Resources["CloseLyricPageAni"];
            double offset = this.ActualHeight - MusicControl.ActualHeight;
            if (_closeLyricPageAni.Children[0] is ThicknessAnimationUsingKeyFrames frames && frames.KeyFrames[0] is EasingThicknessKeyFrame frame)
            {
                frame.Value = new Thickness(0, offset, 0, -offset);
            }
            if (_vm.IsLyricPageOpen)
            {
                _closeLyricPageAni.Begin();
                _vm.IsLyricPageOpen= false;
            }
        }
        #endregion

        #region Play Control
        private void Vm_CacheBegin()
        {
            Dispatcher.Invoke(() => { 
                CacheProg.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
            });
        }
        private void Vm_CacheFinished()
        {
            Dispatcher.Invoke(() => {
                CacheProg.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));
            });
        }

        private bool _isSliderCtrl = false;
        private void PlaySlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed&& sender is Slider PlaySlider)
            {
                double perc = e.GetPosition(PlaySlider).X / PlaySlider.ActualWidth;
                double value = perc * PlaySlider.Maximum;
                //暂时移除PlaySlider的Value binding
                BindingOperations.ClearBinding(PlaySlider, Slider.ValueProperty);
                PlaySlider.Value = value;
                _isSliderCtrl = true;
            }
        }

        private void PlaySlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSliderCtrl && sender is Slider PlaySlider)
            {
                //提交value
                _vm.SetCurrentPlayingPosition(PlaySlider.Value);
                //重新绑定PlaySlider的Value
                var binding = new Binding("CurrentPlayingPosition")
                {
                    Source = _vm,
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                PlaySlider.SetBinding(Slider.ValueProperty, binding);
                _isSliderCtrl = false;
            }
        }

        private void GotoPlayingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistLb.SelectedItem is { } music)
                PlaylistLb.ScrollIntoView(music);
        }
        
        private void PlaylistSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (!string.IsNullOrWhiteSpace(PlaylistSearchBox.Text))
                {
                    var m = _vm.SearchPlaylist(PlaylistSearchBox.Text);
                    PlaylistLb.ScrollIntoView(m);
                }
            }
        }

        private void MainPageMenuItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // 阻止默认的选中行为
            (sender as ListBoxItem).Tag = true;
        }

        private void MainPageMenuItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item&& item.Tag is true)
            {
                item.IsSelected = true;
                item.Tag = false;
                _vm.RequireCreateNewPage();
            }
        }

        private void MainPageMenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem item && item.Tag is true)
                item.Tag = false;
        }

        #endregion

        #region Functional Button Navigation
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchHintPopup.IsOpen = false;
                _mainNavigationService.RequstNavigation(PageType.SearchPage, SearchBox.Text);
            }
            else if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (SearchHintPopup.Child is SearchHintView { } view)
                {
                    view.HintList.Focus();
                }
            }
        }
        private readonly HttpClient hcForSearch = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);

        private void LyricPage_Img_Drop(object sender, DragEventArgs e)
        {
            _vm.ReplacePlayFile(((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString());
        }

        private async void MoreMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            MoreOptionPopup.IsOpen = true;
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchHintPopup.IsOpen = false;
                return;
            }
            if (SearchHintPopup.Child == null)
            {
                var view = _serviceProvider.GetRequiredService<SearchHintView>();
                view.RequestClose = () => SearchHintPopup.IsOpen = false;
                view.RequestDefocus = () => SearchBox.Focus();
                SearchHintPopup.Child = view;
            }
            if (!SearchHintPopup.IsOpen)
            {
                await Task.Yield();
                SearchHintPopup.IsOpen = true;
            }
            var hints = await SearchHintAPI.GetSearchHintAsync(SearchBox.Text, hcForSearch);
            (SearchHintPopup.Child as SearchHintView)!.Hints = hints;
        }
        #endregion
    }
}