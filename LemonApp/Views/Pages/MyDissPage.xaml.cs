using LemonApp.MusicLib.Playlist;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// MyDissPage.xaml 的交互逻辑
    /// </summary>
    public partial class MyDissPage : Page
    {
        private readonly UserProfileService user;
        private readonly MainNavigationService nav;
        private readonly IServiceProvider sp;
        public MyDissPage(
            UserProfileService userProfileService,
            MainNavigationService mainNavigationService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Loaded += MyDissPage_Loaded;
            user = userProfileService;
            nav = mainNavigationService;
            sp= serviceProvider;
        }

        private async void MyDissPage_Loaded(object sender, RoutedEventArgs e)
        {
            nav.BeginLoadingAni();
            await user.UpdateAuthAndNotify(user.GetAuth());
            //load my diss
            MyDissList.ViewModel ??= sp.GetRequiredService<PlaylistItemViewModel>();
            MyDissList.ViewModel.SetPlaylistItems(user.UserProfileGetter.MyPlaylists);
            //load my favorite
            MyFarvoriteDissList.ViewModel ??= sp.GetRequiredService<PlaylistItemViewModel>();
            MyFarvoriteDissList.ViewModel.SetPlaylistItems(user.UserProfileGetter.MyFavoritePlaylists);
            //load netease diss
            if(user.GetNeteaseAuth() is { } netease)
            {
                NeteaseTb.Visibility = NeteaseDissList.Visibility = Visibility.Visible;
                NeteaseDissList.ViewModel ??= sp.GetRequiredService<PlaylistItemViewModel>();
                NeteaseDissList.ViewModel.SetPlaylistItems(await NeteasePlaylistAPI.GetNeteaseUserPlaylistAsync(
                    new HttpClient(),netease));
            }
            nav.CancelLoadingAni();
        }
    }
}
