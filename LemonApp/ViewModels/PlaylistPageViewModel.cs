using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.ViewModels;

public partial class PlaylistPageViewModel(
    MainNavigationService navigationService,
    MediaPlayerService mediaPlayerService
    ) :ObservableObject
{
    private readonly MainNavigationService _navigationService =navigationService;
    private readonly MediaPlayerService _mediaPlayerService = mediaPlayerService;
    [ObservableProperty]
    private string _listName= "";
    [ObservableProperty]
    private string _description = "";
    [ObservableProperty]
    private Brush? _cover = null;
    [ObservableProperty]
    private string _creatorName = "";
    [ObservableProperty]
    private Brush? _creatorAvatar = null;

    [ObservableProperty]
    private bool _showInfoView = true;

    public ObservableCollection<Music> Musics { get; set; } = [];

    //TODO: 接入LoadMore
    public event Action? OnLoadMoreRequired;
    public void LoadMore()
    {
        OnLoadMoreRequired?.Invoke();
    }

    [RelayCommand]
    private void GotoAlbumPage(string albumId)
    {
        _navigationService.RequstNavigation(PageType.AlbumPage, albumId);
    }
    private void GotoArtistPage(string artistId)
    {
        _navigationService.RequstNavigation(PageType.ArtistPage, artistId);
    }

    [RelayCommand]
    private void CheckIfGotoArtistsPopup(Music m)
    {
        if (m.Singer.Count == 1)
        {
            GotoArtistPage(m.Singer[0].Mid);
        }
        else
        {
            ToChoosenArtists.Clear();
            foreach(var s in m.Singer)
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
        if(value !=null){
            GotoArtistPage(value.Mid);
            ShowCheckArtistsPopup = false;
        }
    }
    public ObservableCollection<Profile> ToChoosenArtists { get; set; } = [];

    [RelayCommand]
    private async Task PlayMusic(Music m)
    {
        _mediaPlayerService.ReplacePlayList(Musics);

        await _mediaPlayerService.Load(m);
        _mediaPlayerService.Play();
    }
    public void UpdateCurrentPlaying(string? musicId)
    {
        if (!string.IsNullOrEmpty(musicId))
            Playing = Musics.FirstOrDefault(m => m.MusicID == musicId);
    }

    /// <summary>
    /// for display only
    /// </summary>
    [ObservableProperty]
    private Music? _playing = null;

}
