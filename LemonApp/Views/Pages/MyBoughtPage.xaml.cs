using LemonApp.MusicLib.User;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Forms;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// MyBoughtPage.xaml 的交互逻辑
    /// </summary>
    public partial class MyBoughtPage : Page
    {
        private readonly UserProfileService user;
        private readonly MainNavigationService nav;
        private readonly IServiceProvider sp;
        public MyBoughtPage(UserProfileService userProfileService,
            MainNavigationService mainNavigationService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            user = userProfileService;
            nav = mainNavigationService;
            sp = serviceProvider;
            Loaded += MyBoughtPage_Loaded;
        }

        private async void MyBoughtPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            nav.BeginLoadingAni();
            var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
            var auth = user.GetAuth();
            if (auth != null && hc != null)
            {
                var data = await TencMyDissAPI.GetMyBoughtAlbumList(auth, hc);
                if (data != null)
                {
                    viewer.ViewModel ??= sp.GetRequiredService<AlbumItemViewModel>();
                    _ = viewer.ViewModel.SetAlbumItems(data);
                }
            }
            nav.CancelLoadingAni();
        }
    }
}
