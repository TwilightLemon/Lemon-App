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

        private void OpenMenu(string key)
        {
            if (FindResource(key) is ContextMenu menu)
            {
                menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                menu.DataContext = this;
                menu.IsOpen = true;
            }
        }

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
        private Task AddSingleToMyDiss(Music music)
            => AddToMyDiss([music]);
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
            OpenMenu("DissSelectorMenu");
        }
        private IList<Music>? ToAddedMusics;
        public ObservableCollection<Playlist> MyDissList { get; set; } = [];
        
        [RelayCommand]
        private async Task SelectDiss(Playlist value)
        {
            //add to diss
            if (value == null||value.DirId==null||ToAddedMusics==null) return;

            await userDataManager.AddSongsToDirid(value.DirId, ToAddedMusics, value.Name);
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
                foreach (var s in m.Singer)
                    SingerMenuItems.Add(s);
                OpenMenu("ArtistSelectorMenu");
            }
        }

        [RelayCommand]
        private void SelectArtist(Profile value)
        {
            if (value != null)
            {
                GotoArtistPage(value);
            }
        }
        public ObservableCollection<Profile> SingerMenuItems { get; set; } = [];

        [ObservableProperty]
        private Music? _selectedMusic;

        [RelayCommand]
        private void ShowMusicOptionsPopup(Music m)
        {
            SelectedMusic = m;
            SingerMenuItems.Clear();
            if (m.Singer.Count > 1)
            {
                foreach (var s in m.Singer)
                    SingerMenuItems.Add(s);
            }
            OpenMenu("MusicOptionsMenu");
        }

        [RelayCommand]
        private void GoToCommentPage(Music m)
        {
            navigationService.RequstNavigation(PageType.CommentPage, m);
        }
        [RelayCommand]
        private void DownloadMusic(Music m)
        {
            App.Services.GetRequiredService<DownloadService>().PushTask(m);
            navigationService.RequstNavigation(PageType.Notification, $"Music {m.MusicName} has been added to the download queue.");
        }
    }
}
