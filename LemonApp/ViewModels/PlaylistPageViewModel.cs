using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LemonApp.MusicLib.Abstraction.Entities;
using System.Collections.Generic;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Playlist;

namespace LemonApp.ViewModels;

public partial class PlaylistPageViewModel(
    MainNavigationService navigationService,
    MediaPlayerService mediaPlayerService,
    UserDataManager userDataManager,
    DownloadService downloadService
    ) :ObservableObject,IDisposable
{
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
    [ObservableProperty]
    private Visibility _showGotoPlaying = Visibility.Collapsed;
    [ObservableProperty]
    private bool _isOwned = false;
    public string? Dirid;

    public void Dispose()
    {
        OnLoadMoreRequired = null;
    }


    public PlaylistType PlaylistType { get; set; } = PlaylistType.Other;

    public ObservableCollection<Music> Musics { get; set; } = [];
    private IList<Music>? _oriData;
    public void InitMusicList(IList<Music> list)
    {
        Musics.Clear();
        _oriData = list;
        foreach (var m in list)
        {
            Musics.Add(m);
        }
    }

    public async void DeleteMusicFromDirid(IList<Music> musics)
    {
        if (string.IsNullOrWhiteSpace(Dirid)) return;
        bool success=await userDataManager.DeleteSongsFromDirid(Dirid, musics, ListName);
        if (success)
        {
            foreach (var item in musics)
            {
                Musics.Remove(item);
            }
        }
    }

    //TODO: 接入LoadMore
    public event Action? OnLoadMoreRequired;
    public void LoadMore()
    {
        OnLoadMoreRequired?.Invoke();
    }

    public void DownloadMusic(IList<Music> music)
    {
        foreach (var m in music)
            downloadService.PushTask(m);
        navigationService.RequstNavigation(PageType.Notification, $"{music.Count} songs have been added to the download queue.");
    }

    public void SearchItem(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            if (_oriData != null && Musics.Count != _oriData.Count){
                InitMusicList(_oriData);
                return;
            }
        var searchResult = _oriData?.Where(m => TextHelper.FuzzySearch(m,key));
        if (searchResult != null)
        {
            Musics.Clear();
            foreach (var m in searchResult)
            {
                Musics.Add(m);
            }
        }
    }

    [RelayCommand]
    private void GotoAlbumPage(string albumId)
    {
        navigationService.RequstNavigation(PageType.AlbumPage, albumId);
    }

    private bool _hasAddToPlaylist = false;
    [RelayCommand]
    private async Task PlayMusic(Music m)
    {
        //根据列表类型选择播放所有/插入下一曲
        //在当前页面第二次点击时添加整个列表
        if (PlaylistType == PlaylistType.Other&&!_hasAddToPlaylist)
        {
            mediaPlayerService.AddToPlayNext(m);
            _hasAddToPlaylist = true;
        }
        else
            mediaPlayerService.ReplacePlayList(Musics);

        await mediaPlayerService.LoadThenPlay(m);
    }
    [RelayCommand]
    private async Task PlayAll()
    {
        if(Musics.Count == 0)
            return;

        mediaPlayerService.ReplacePlayList(Musics);
        await mediaPlayerService.LoadThenPlay(Musics[0]);
    }

    [RelayCommand]
    private void AddToPlayNext(IList<Music> list)
    {
        mediaPlayerService.AddToPlayNext(list);
    }
    [RelayCommand]
    private void AddToPlayNextSingle(Music music)
    {
        mediaPlayerService.AddToPlayNext(music);
    }
    public void UpdateCurrentPlaying(string? musicId)
    {
        if (!string.IsNullOrEmpty(musicId))
        {
            Playing = Musics.FirstOrDefault(m => m.MusicID == musicId);
        }
    }

    /// <summary>
    /// for display only
    /// </summary>
    [ObservableProperty]
    private Music? _playing = null;
    partial void OnPlayingChanged(Music? value)
    {
        ShowGotoPlaying = value==null?Visibility.Collapsed:Visibility.Visible;
    }

}
