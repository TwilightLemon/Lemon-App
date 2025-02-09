using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Playlist;
using LemonApp.Services;
using LemonApp.Views.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// PopupSelector.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class PopupSelector : UserControl
    {
        public PopupSelector(MainNavigationService mainNavigationService,
            IHttpClientFactory hcFactory,
            UserProfileService userProfile)
        {
            InitializeComponent();
            DataContext = this;
            navigationService = mainNavigationService;
            httpClientFactory = hcFactory;
            userProfileService = userProfile;
        }
        private readonly IHttpClientFactory httpClientFactory;
        private readonly UserProfileService userProfileService;
        private readonly MainNavigationService navigationService;

        [RelayCommand]
        private async Task AddToMyDiss(IList<Music> musics)
        {
            var hc = httpClientFactory.CreateClient(App.PublicClientFlag);
            var auth = userProfileService.GetAuth();
            var data = await PlaylistManageAPI.GetMyDissListAsync(hc,auth);
            MyDissList.Clear();
            foreach(var item in data)
            {
                MyDissList.Add(item);
            }
            ToAddedMusics = musics;
            IsShowDissSelector = true;
        }
        private IList<Music> ToAddedMusics;
        public ObservableCollection<Playlist> MyDissList { get; set; } = [];
        [ObservableProperty]
        private bool _isShowDissSelector = false;
        [ObservableProperty]
        private Playlist? _selectedDiss;
        partial void OnSelectedDissChanged(Playlist? value)
        {
            //add to diss
            if (value == null) return;
            var hc = httpClientFactory.CreateClient(App.PublicClientFlag);
            var auth = userProfileService.GetAuth();
            var success = PlaylistManageAPI.WriteMusicsToMyDissAsync(hc,auth,value.DirId,ToAddedMusics);
            navigationService.RequstNavigation(PageType.Notification, $"Successfully added {ToAddedMusics.Count} songs to {value.Name}");
            IsShowDissSelector = false;
        }


        private void GotoArtistPage(Profile artist)
        {
            navigationService.RequstNavigation(PageType.ArtistPage, artist);
        }

        [RelayCommand]
        private void CheckIfGotoArtistsPopup(Music m)
        {
            if (m.Singer.Count == 1)
            {
                GotoArtistPage(m.Singer[0]);
            }
            else
            {
                ToChoosenArtists.Clear();
                foreach (var s in m.Singer)
                    ToChoosenArtists.Add(s);
                ShowCheckArtistsPopup = true;
            }
        }

        [ObservableProperty]
        private bool _showCheckArtistsPopup = false;
        [ObservableProperty]
        private Profile? _choosenArtist = null;

        partial void OnChoosenArtistChanged(Profile? value)
        {
            if (value != null)
            {
                GotoArtistPage(value);
                ShowCheckArtistsPopup = false;
            }
        }
        public ObservableCollection<Profile> ToChoosenArtists { get; set; } = [];
    }
}
