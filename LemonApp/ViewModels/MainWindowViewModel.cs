using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Album;
using LemonApp.MusicLib.Search;
using LemonApp.Services;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Media;
using MusicDT = LemonApp.MusicLib.Abstraction.Music.DataTypes;
using LemonApp.Common.Configs;
using LemonApp.MusicLib.Media;
using CommunityToolkit.Mvvm.Input;
using System.Timers;
using LemonApp.Views.UserControls;
using System.Collections.Generic;

namespace LemonApp.ViewModels;
public partial class MainWindowViewModel : ObservableObject
{
    #region fields & constructor
    private readonly IServiceProvider _serviceProvider;
    private readonly UserProfileService _userProfileService;
    private readonly MainNavigationService _mainNavigationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly AppSettingsService _appSettingsService;

    private readonly Timer _timer;
    private SettingsMgr<PlayingPreference>? _currentPlayingMgr;

    public MainWindowViewModel(
        UserProfileService userProfileService,
        IServiceProvider serviceProvider,
        MainNavigationService mainNavigationService,
        MediaPlayerService mediaPlayerService,
        AppSettingsService appSettingsService,
        LyricView lyricView)
    {
        _userProfileService = userProfileService;
        _serviceProvider = serviceProvider;
        _mainNavigationService = mainNavigationService;
        _mediaPlayerService = mediaPlayerService;
        _appSettingsService = appSettingsService;
        _appSettingsService.OnExiting += _appSettingsService_OnExiting;

        _mediaPlayerService.OnLoaded += _mediaPlayerService_OnLoaded;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;
        _mediaPlayerService.OnNewPlaylistReceived += _mediaPlayerService_OnNewPlaylistReceived;
        _mediaPlayerService.OnAddToPlayNext += _mediaPlayerService_OnAddToPlayNext;
        _mediaPlayerService.OnEnd += _mediaPlayerService_OnEnd;
        _mediaPlayerService.OnPlayNext += PlayNext;
        _mediaPlayerService.OnPlayLast += PlayLast;

        LyricView = lyricView;

        _mainNavigationService.OnNavigatingRequsted += MainNavigationService_OnNavigatingRequsted;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;

        _timer = new();
        _timer.Elapsed += _timer_Elapsed;
        _timer.Interval = 1000;

        LoadComponent();
    }
    #endregion
    #region common components
    private async void LoadComponent()
    {
        //update current playing
        _currentPlayingMgr = _appSettingsService.GetConfigMgr<PlayingPreference>();
        if (_currentPlayingMgr is { } mgr)
        {
            if (mgr.Data?.Music is { MusicID: not "" } music)
            {
                await _mediaPlayerService.Load(music);
                CurrentPlayingVolume = mgr.Data.Volume;
                CircleMode = mgr.Data.PlayMode;

                if (_currentPlayingMgr!.Data.Playlist != null)
                {
                    Playlist.Clear();
                    foreach (var item in _currentPlayingMgr!.Data.Playlist)
                    {
                        Playlist.Add(item);
                    }
                    PlaylistChoosen = Playlist.FirstOrDefault(p => p.MusicID == music.MusicID);
                    _currentPlayingMgr!.Data.Playlist = null;
                }
            }
            else
            {
                CurrentPlaying = new()
                {
                    MusicName = "暂无播放",
                    SingerText = "Lemon App"
                };
            }
        }
    }

    private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var dur = _mediaPlayerService.Duration;
        CurrentPlayingDuration = dur.TotalMilliseconds;
        CurrentPlayingDurationText = $"{dur.Minutes:D2}:{dur.Seconds:D2}";

        var pos = _mediaPlayerService.Position;
        CurrentPlayingPosition = pos.TotalMilliseconds;
        CurrentPlayingPositionText = $"{pos.Minutes:D2}:{pos.Seconds:D2}";

        LyricView!.Dispatcher.Invoke(() => {
            LyricView.UpdateTime(pos.TotalMilliseconds);
        });
    }

    #endregion
    #region playlist
    private Random? _random;
    private List<int>? _randomIndexList;
    private int GetIndexOfPlaylist(string? musicId)
    {
        if (musicId == null) return -1;
        int index = 0;
        foreach(var item in Playlist)
        {
            if (item.MusicID == musicId)
                break;
            index++;
        }
        return index;
    }
    [RelayCommand]
    private void PlayNext()
    {
        if (CircleMode == PlayingPreference.CircleMode.Circle)
        {
            if (CurrentPlaying != null)
            {
                int index = GetIndexOfPlaylist(CurrentPlaying.MusicID);
                index = index == Playlist.Count - 1 ? 0 : index + 1;
                PlaylistChoosen = Playlist[index];
            }
        }
        else if (CircleMode == PlayingPreference.CircleMode.Random)
        {
            _random ??= new();
            _randomIndexList ??= [];
            int currentIndex = GetIndexOfPlaylist(CurrentPlaying?.MusicID);
            if (currentIndex != -1)
            {
                _randomIndexList.Add(currentIndex);
            }

            var index = _random.Next(0, Playlist.Count);
            while (_randomIndexList.Contains(index))
            {
                index = _random.Next(0, Playlist.Count);
            }
            if (_randomIndexList.Count == Playlist.Count)
            {
                _randomIndexList.RemoveAt(0);
            }
            PlaylistChoosen = Playlist[index];
        }
    }
    [RelayCommand]
    private void PlayLast()
    {
        if (CircleMode == PlayingPreference.CircleMode.Circle)
        {
            if (CurrentPlaying != null)
            {
                int index = GetIndexOfPlaylist(CurrentPlaying.MusicID);
                index = index == 0 ? Playlist.Count - 1 : index - 1;
                PlaylistChoosen = Playlist[index];
            }
        }
        else if (CircleMode == PlayingPreference.CircleMode.Random)
        {
            if (_randomIndexList != null && _randomIndexList.Count > 0)
            {
                var index = _randomIndexList[^1];
                _randomIndexList.RemoveAt(_randomIndexList.Count - 1);
                PlaylistChoosen = Playlist[index];
            }
        }
    }

    private void _mediaPlayerService_OnEnd()
    {
        if (CircleMode == PlayingPreference.CircleMode.Single)
        {
            CurrentPlayingPosition = 0;
            _mediaPlayerService.Play();
        }else PlayNext();
    }

    private void _appSettingsService_OnExiting()
    {
        _currentPlayingMgr!.Data ??= new();
        _currentPlayingMgr!.Data.Music = CurrentPlaying;
        _currentPlayingMgr!.Data.Volume = CurrentPlayingVolume;
        _currentPlayingMgr!.Data.PlayMode = CircleMode;
        var temp = Playlist.ToList();
        foreach(var item in temp)
        {
            if(item.Album!=null)
            item.Album = new() { 
                Name=item.Album.Name,
                Id=item.Album.Id
            };
        }
        _currentPlayingMgr!.Data.Playlist = temp;
    }

    private void _mediaPlayerService_OnAddToPlayNext(MusicDT.Music obj)
    {
        var current=Playlist.FirstOrDefault(m=>m.MusicID==obj.MusicID);
        int index = current!=null? Playlist.IndexOf(current) : 0;
        Playlist.Insert(index + 1, obj);
    }

    private void _mediaPlayerService_OnNewPlaylistReceived(IEnumerable<MusicDT.Music> obj)
    {
        Playlist.Clear();
        foreach (var item in obj)
        {
            Playlist.Add(item);
        }
    }
    #endregion
    #region respond to media player controller
    private void _mediaPlayerService_OnPaused(MusicDT.Music obj)
    {
        IsPlaying = false;
        _timer?.Stop();
    }

    private void _mediaPlayerService_OnPlay(MusicDT.Music m)
    {
        IsPlaying = true;
        _timer?.Start();
    }
    public event Action<string>? SyncCurrentPlayingWithPlayListPage;
    private void _mediaPlayerService_OnLoaded(MusicDT.Music m)
    {
        App.Current.Dispatcher.Invoke(async () =>
        {
            CurrentPlaying = m;
            SyncCurrentPlayingWithPlayListPage?.Invoke(m.MusicID);
            await LyricView!.LoadFromMusic(m);
        });
    }
    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying)
        {
            _mediaPlayerService.Pause();
        }
        else
        {
            _mediaPlayerService.Play();
        }
    }
    #endregion
    #region userprofile
    private void UserProfileService_OnAuthExpired()
    {
        //TODO: notify main window to show msg
        Debug.WriteLine("Auth expired.....");
    }

    private async void UserProfileService_OnAuth(TencUserAuth auth)
    {
        //update user profile viewmodel
        if (await _userProfileService.GetAvatorImg() is { } img)
        {
            Avator = new ImageBrush(img);
        }
    }
    #endregion
    #region main menu
    [ObservableProperty]
    private Brush? avator;
    public class MainMenu(string name, Geometry icon, Type pageType)
    {
        public string Name { get; } = name;
        public Geometry Icon { get; } = icon;
        public Type PageType { get; } = pageType;
        public bool RequireCreateNewPage = true;
    }
    public ObservableCollection<MainMenu> MainMenus { get; set; } = [
        new MainMenu("Home", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(HomePage)),
        new MainMenu("Rank", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(RankPage))
        ];


    public object? CurrentPage;

    [ObservableProperty]
    private MainMenu? selectedMenu;

    partial void OnSelectedMenuChanged(MainMenu? value)
    {
        if (value != null)
        {
            if (value.PageType is { } type && value.RequireCreateNewPage)
            {
                var page = App.Host!.Services.GetRequiredService(type);
                CurrentPage = page;
            }
            else if (!value.RequireCreateNewPage)
            {
                value.RequireCreateNewPage = true;//reset
            }
        }
        else
        {
            CurrentPage = null;
        }
    }
    #endregion
    #region navigation
    public bool IsLyricPageOpen { get; set; } = false;

    [ObservableProperty]
    private bool _isLoading = false;

    public event Action<Page>? RequireNavigateToPage;

    private void MainNavigationService_OnNavigatingRequsted(PageType type, object? arg)
    {
        switch (type)
        {
            case PageType.SettingsPage:
                NavigateToSettingsPage();
                break;
            case PageType.AlbumPage:
                if (arg is string { } id)
                    NavigateToAlbumPage(id);
                break;
            case PageType.SearchPage:
                if (arg is string { } keyword)
                    NavigateToSearchPage(keyword);
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
            RequireNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
    }
    private async void NavigateToAlbumPage(string AlbumId)
    {
        IsLoading = true;
        var sp = _serviceProvider.GetRequiredService<PlaylistPage>();
        PlaylistPageViewModel vm = _serviceProvider.GetRequiredService<PlaylistPageViewModel>();
        var hc = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = _userProfileService.GetAuth();
        if (sp != null && hc != null && auth != null)
        {
            var data = await AlbumAPI.GetAlbumTracksByIdAync(hc, auth, AlbumId);
            vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(data.Photo));
            vm.Description = data.Description ?? "";
            vm.ListName = data.Name;
            foreach (var item in data.Musics!)
            {
                vm.Musics.Add(item);
            }
            vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator!.Photo));
            vm.CreatorName = data.Creator.Name;

            vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);

            sp.ViewModel = vm;
            RequireNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
        IsLoading = false;
    }
    private async void NavigateToSearchPage(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return;
        IsLoading = true;
        var sp = _serviceProvider.GetRequiredService<PlaylistPage>();
        var hc = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = _userProfileService.GetAuth();
        var vm = _serviceProvider.GetRequiredService<PlaylistPageViewModel>();
        if (sp != null && hc != null && auth != null && vm != null)
        {
            vm.ShowInfoView = false;
            var search = await SearchAPI.SearchMusicAsync(hc, auth, keyword);
            foreach (var item in search)
            {
                vm.Musics.Add(item);
            }
            vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);

            sp.ViewModel = vm;
            RequireNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
        IsLoading = false;
    }
    #endregion
    #region playing
    [ObservableProperty]
    private bool _isPlaying = false;
    [ObservableProperty]
    private MusicDT.Music? _currentPlaying = null;
    [ObservableProperty]
    private string _currentPlayingPositionText = "00:00";
    [ObservableProperty]
    private string _currentPlayingDurationText = "00:00";
    [ObservableProperty]
    private double _currentPlayingPosition = 0;
    [ObservableProperty]
    private double _currentPlayingDuration = 0;
    [ObservableProperty]
    private double _currentPlayingVolume = 0.5;
    [ObservableProperty]
    private Brush? _currentPlayingCover;
    [ObservableProperty]
    private LyricView? _lyricView;
    [ObservableProperty]
    private PlayingPreference.CircleMode _circleMode = PlayingPreference.CircleMode.Circle;
    public ObservableCollection<MusicDT.Music> Playlist { get;private set; } = [];
    [ObservableProperty]
    private MusicDT.Music? _playlistChoosen = null;
    async partial void OnPlaylistChoosenChanged(MusicDT.Music? value)
    {
        if (value != null&&value.MusicID!=CurrentPlaying?.MusicID)
        {
            await _mediaPlayerService.Load(value);
            _mediaPlayerService.Play();
        }
    }

    [RelayCommand]
    private void ChangeCircleMode()
    {
        CircleMode = CircleMode switch
        {
            PlayingPreference.CircleMode.Circle => PlayingPreference.CircleMode.Single,
            PlayingPreference.CircleMode.Single => PlayingPreference.CircleMode.Random,
            PlayingPreference.CircleMode.Random => PlayingPreference.CircleMode.Circle,
            _ => PlayingPreference.CircleMode.Circle
        };
    }

    async partial void OnCurrentPlayingChanged(MusicDT.Music? value)
    {
        //只负责更新UI的ViewModel
        if (value == null||string.IsNullOrEmpty(value.MusicID)) return;

        var hc= _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        CurrentPlayingCover = new ImageBrush(await ImageCacheHelper.FetchData(await CoverGetter.GetCoverImgUrl(hc, value)));

        CurrentPlayingPosition = 0;
        CurrentPlayingPositionText = "00:00";
        CurrentPlayingVolume = _mediaPlayerService.Volume;

        PlaylistChoosen = value;
    }
    partial void OnCurrentPlayingVolumeChanged(double value)
    {
       _mediaPlayerService.Volume = value;      
    }
    public void SetCurrentPlayingPosition(double value)
    {
        _mediaPlayerService.Position = TimeSpan.FromMilliseconds(value);
    }
    #endregion
}
