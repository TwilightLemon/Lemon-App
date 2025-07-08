using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Search;
using LemonApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// PopupSelector.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class PopupSelector : UserControl
    {
        public PopupSelector(MainNavigationService mainNavigationService, UserDataManager userDataManager)
        {
            InitializeComponent();
            DataContext = this;
            navigationService = mainNavigationService;
            this.userDataManager = userDataManager;
        }
        private readonly UserDataManager userDataManager;
        private readonly MainNavigationService navigationService;

        [RelayCommand]
        private void GoToAlbumPage(AlbumInfo info)
        {
            if (info.Platform == Platform.wyy)
            {
                async Task search()
                {
                    var data = await SearchHintAPI.GetSearchHintAsync(info.Name, App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag));
                    if (data?.Hints.FirstOrDefault(item => item.Type == SearchHint.HintType.Album) is { } hint)
                    {
                        navigationService.RequstNavigation(PageType.AlbumPage, hint.Id);
                    }
                    else
                    {
                        navigationService.RequstNavigation(PageType.Notification, "Failed to redirect this album");
                    }
                }
                _ = search();
            }
            else if (info.Platform == Platform.qq)
                navigationService.RequstNavigation(PageType.AlbumPage, info.Id);
        }

        [RelayCommand]
        private async Task AddToMyDiss(IList<Music> musics)
        {
            var data = await userDataManager.GetUserPlaylists();
            MyDissList.Clear();
            foreach(var item in data)
            {
                MyDissList.Add(item);
            }
            ToAddedMusics = musics;
            IsShowDissSelector = true;
        }
        private IList<Music>? ToAddedMusics;
        public ObservableCollection<Playlist> MyDissList { get; set; } = [];
        [ObservableProperty]
        private bool _isShowDissSelector = false;
        [ObservableProperty]
        private Playlist? _selectedDiss;
        async partial void OnSelectedDissChanged(Playlist? value)
        {
            //add to diss
            if (value == null||value.DirId==null||ToAddedMusics==null) return;

            await userDataManager.AddSongsToDirid(value.DirId, ToAddedMusics, value.Name);
            IsShowDissSelector = false;
        }

        private void GotoArtistPage(Profile artist)
        {
            if (artist.Platform == Platform.wyy)
            {
                //search artist 
                async Task search()
                {
                    var data = await SearchHintAPI.GetSearchHintAsync(artist.Name, App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag));
                    if (data?.Hints.FirstOrDefault(item => item.Type == SearchHint.HintType.Singer) is { } hint)
                    {
                        navigationService.RequstNavigation(PageType.ArtistPage, new Profile()
                        {
                            Mid = hint.Id,
                            Name = hint.Content
                        });
                    }
                    else
                    {
                        navigationService.RequstNavigation(PageType.Notification, "Failed to redirect this album");
                    }
                }
                _ = search();
            }
            else if (artist.Platform == Platform.qq)
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
