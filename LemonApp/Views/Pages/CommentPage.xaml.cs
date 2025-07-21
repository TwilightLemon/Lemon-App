using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EleCho.WpfSuite.Controls;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Media;
using LemonApp.MusicLib.Other;
using LemonApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using Button = EleCho.WpfSuite.Controls.Button;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// CommentPage.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class CommentPage : Page
    {
        public CommentPage(IHttpClientFactory httpClientFactory,
                           UserProfileService userProfileService,
                           MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            hc = httpClientFactory.CreateClient(App.PublicClientFlag);
            user = userProfileService;
            DataContext = this;
            nav = mainNavigationService;
        }
        private readonly HttpClient hc;
        private readonly UserProfileService user;
        private readonly MainNavigationService nav;
        [ObservableProperty]
        private CommentPageData? data;
        [ObservableProperty]
        private string songName = "";
        [ObservableProperty]
        private Brush songCover;
        [ObservableProperty]
        private Music? musicEntity;
        [ObservableProperty]
        private bool isLoaded = false;
        public async Task LoadCommentAsync(Music m)
        {
            using var _= nav.BeginLoading();
           SongName = $"{m.MusicName} - {m.SingerText}";

            var cover = await ImageCacheService.FetchData(await CoverGetter.GetCoverImgUrl(()=>hc, user.GetAuth(), m));
            SongCover = new ImageBrush(cover);
            MusicEntity = m;

            Data =await CommentAPI.GetCommentsAsync(m.MusicID, hc, user.GetAuth());
            IsLoaded = true;
        }
        [RelayCommand]
        private void SyncWithCurrent()
        {
            if (App.Services.GetRequiredService<MediaPlayerService>().CurrentMusic is { } m)
                _ = LoadCommentAsync(m);
        }
        [ObservableProperty]
        private Comment? selectedComment;
        [ObservableProperty]
        private bool isCommentSelected;
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if(sender is Button { DataContext:Comment{ } cmt })
            {
                SelectedComment = cmt;
                IsCommentSelected = true;
            }
        }
    }
}
