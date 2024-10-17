﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using LemonApp.Common.UIBases;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

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

        private void MainPageMenu_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_vm.CurrentPage != null)
                MainContentFrame.Navigate(_vm.CurrentPage);
        }

        private void GoBackBtn_Click(object sender, RoutedEventArgs e)
        {
            MainContentFrame.GoBack();
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