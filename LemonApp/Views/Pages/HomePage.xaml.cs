using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Home;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        private readonly MainNavigationService nav;
        private readonly UserProfileService user;
        private readonly IHttpClientFactory hcf; 
        public HomePage(MainNavigationService mainNavigationService,
                        UserProfileService userProfileService,
                        IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();
            nav = mainNavigationService;
            user = userProfileService;
            hcf = httpClientFactory;
            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (RecommendPlaylist.ViewModel != null) return;
            nav.BeginLoadingAni();
            var data = await HomeDataAPI.GetHomePageDataAsync(hcf.CreateClient(App.PublicClientFlag), user.GetAuth());
            if (data != null)
            {
                RecommendPlaylist.ViewModel= App.Services.GetRequiredService<PlaylistItemViewModel>();
                RecommendPlaylist.ViewModel.SetPlaylistItems(data.Recommend);

                ExplorePlaylist.ViewModel = App.Services.GetRequiredService<PlaylistItemViewModel>();
                ExplorePlaylist.ViewModel.SetPlaylistItems(data.Explore);

                NewMusicList.ItemsSource = data.NewMusics[0..30];
            }

            if(user.GetAuth() is { IsValid: true } auth)
            {
                GreetingTb.Text = GetGreeting(user.GetNickname()!);

                var personality=await PersonalityAPI.GetPersonality(hcf.CreateClient(App.PublicClientFlag), auth);
                PersonalityView.DataContext = personality;

                var singerVm = App.Services.GetRequiredService<SingerItemViewModel>();
                singerVm.SetList(personality.Singers);
                PersonalitySingerView.DataContext = singerVm;
            }
            else
            {
                PersonalityView.Visibility = Visibility.Collapsed;
            }
                nav.CancelLoadingAni();
        }

        private static string GetGreeting(string username)
        {
            int hour = DateTime.Now.Hour;

            return hour switch
            {
                >= 5 and < 12 => $"Good morning, {username}",
                >= 12 and < 17 => $"Good afternoon, {username}",
                >= 17 and < 22 => $"Good evening, {username}",
                _ => $"Good night, {username}" // 默认情况（晚上10点~凌晨4点）
            };
        }

        private void NewMusicList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (NewMusicList.SelectedItem is Music{ } m)
                _ = App.Services.GetRequiredService<MediaPlayerService>().LoadThenPlay(m);
        }
    }
}
