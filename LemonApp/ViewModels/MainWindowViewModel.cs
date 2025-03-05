using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Funcs;
using LemonApp.Native;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.Services;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Media;
using LemonApp.Common.Configs;
using LemonApp.MusicLib.Media;
using CommunityToolkit.Mvvm.Input;
using System.Timers;
using LemonApp.Views.UserControls;
using System.Collections.Generic;
using System.Threading.Tasks;
using LemonApp.Views.Windows;
using Task = System.Threading.Tasks.Task;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.User;
using LemonApp.Components;
using System.Windows;

//TODO: 将功能再细分为Component 简化ViewModel
namespace LemonApp.ViewModels;
public partial class MainWindowViewModel : ObservableObject
{
    #region fields & constructor
    private readonly IServiceProvider _serviceProvider;
    private readonly UserProfileService _userProfileService;
    private readonly MainNavigationService _mainNavigationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly AppSettingsService _appSettingsService;
    private readonly UIResourceService _uiResourceService;
    private readonly WindowBasicComponent _windowBasicComponent;
    private readonly PlaylistDataWrapper _playlistDataWrapper;
    private readonly DownloadMenuDecorator _downloadMenuDecorator;

    private readonly Timer _timer;
    private SettingsMgr<PlayingPreference>? _currentPlayingMgr;
    private SettingsMgr<PlaylistCache>? _playlistMgr;

    private DesktopLyricWindowViewModel _lyricWindowViewModel;

    public MainWindowViewModel(
        UserProfileService userProfileService,
        IServiceProvider serviceProvider,
        MainNavigationService mainNavigationService,
        MediaPlayerService mediaPlayerService,
        AppSettingsService appSettingsService,
        UIResourceService uIResourceService,
        LyricView lyricView,
        DesktopLyricWindowViewModel lyricWindowViewModel,
        WindowBasicComponent windowBasicComponent,
        PlaylistDataWrapper playlistDataWrapper,
        DownloadMenuDecorator downloadMenuDecorator)
    {
        _downloadMenuDecorator = downloadMenuDecorator;
        _userProfileService = userProfileService;
        _serviceProvider = serviceProvider;
        _mainNavigationService = mainNavigationService;
        _mediaPlayerService = mediaPlayerService;
        _appSettingsService = appSettingsService;
        _uiResourceService = uIResourceService;
        _windowBasicComponent = windowBasicComponent;
        _playlistDataWrapper = playlistDataWrapper;

        _uiResourceService.OnColorModeChanged += UIResourceService_OnColorModeChanged;

        _appSettingsService.OnExiting += AppSettingsService_OnExiting;

        _mediaPlayerService.OnLoaded += MediaPlayerService_OnLoaded;
        _mediaPlayerService.OnPlay += MediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += MediaPlayerService_OnPaused;
        _mediaPlayerService.OnNewPlaylistReceived += MediaPlayerService_OnNewPlaylistReceived;
        _mediaPlayerService.OnAddToPlayNext += MediaPlayerService_OnAddToPlayNext;
        _mediaPlayerService.OnAddListToPlayNext += MediaPlayerService_OnAddListToPlayNext;
        _mediaPlayerService.OnEnd += MediaPlayerService_OnEnd;
        _mediaPlayerService.OnPlayNext += PlayNext;
        _mediaPlayerService.OnPlayLast += PlayLast;
        _mediaPlayerService.OnQualityChanged += MediaPlayerService_OnQualityChanged;
        _mediaPlayerService.CacheProgress = CacheProgress;
        _mediaPlayerService.FailedToLoadMusic += MediaPlayerService_FailedToLoadMusic;

        LyricView = lyricView;
        LyricView.OnNextLrcReached += LyricView_OnNextLrcReached;

        _lyricWindowViewModel = lyricWindowViewModel;

        _mainNavigationService.OnNavigationRequested += MainNavigationService_OnNavigatingRequsted;
        _mainNavigationService.LoadingAniRequested += () => IsLoading = true;
        _mainNavigationService.LoadingAniCancelled += () => IsLoading = false;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;

        _timer = new();
        _timer.Elapsed += Timer_Elapsed;
        _timer.Interval = 1000;

        LoadMainMenus();
        LoadComponent();
        _playlistDataWrapper = playlistDataWrapper;
        _downloadMenuDecorator = downloadMenuDecorator;
    }

    private async void UIResourceService_OnColorModeChanged()
    {
        await UpdateCover();
    }

    private void LyricView_OnNextLrcReached(LrcLine obj)
    {
        _lyricWindowViewModel.Update(obj);
    }
    #endregion
    #region common components
    private async void LoadComponent()
    {
        //update current playing from Settings:
        _currentPlayingMgr = _appSettingsService.GetConfigMgr<PlayingPreference>();
        _playlistMgr = _appSettingsService.GetConfigMgr<PlaylistCache>();
        if (_currentPlayingMgr is { } mgr&&_playlistMgr is { } pl)
        {
            if (mgr.Data?.Music is { MusicID: not "" } music)
            {
                await _mediaPlayerService.Load(music);
                CurrentPlayingVolume = mgr.Data.Volume;
                CircleMode = mgr.Data.PlayMode;
                IsShowDesktopLyric= mgr.Data.ShowDesktopLyric;

                if (pl.Data.Playlist != null)
                {
                    Playlist.Clear();
                    foreach (var item in pl.Data.Playlist)
                    {
                        Playlist.Add(item);
                    }
                    PlaylistChoosen = Playlist.FirstOrDefault(p => p.MusicID == music.MusicID);
                    pl.Data.Playlist = null;
                }
            }
            else
            {
                CurrentPlaying = new()
                {
                    MusicName = "Welcome~",
                    SingerText = "Lemon App"
                };
            }
        }
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        var dur = _mediaPlayerService.Duration;
        CurrentPlayingDuration = dur.TotalMilliseconds;
        CurrentPlayingDurationText = $"{dur.Minutes:D2}:{dur.Seconds:D2}";

        var pos = _mediaPlayerService.Position;
        CurrentPlayingPosition = pos.TotalMilliseconds;
        CurrentPlayingPositionText = $"{pos.Minutes:D2}:{pos.Seconds:D2}";

        try
        {
            LyricView.Dispatcher.Invoke(() =>
            {
                LyricView.UpdateTime(pos.TotalMilliseconds);
            });
        }
        catch { }
    }

    #endregion
    #region playlist & play controller
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
        if (index == Playlist.Count) index = -1;
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
                if (index == -1) index = 0;
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
                if (index == -1) index = 0;
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

    public void ReplacePlayFile(string path) => _mediaPlayerService.ReplacePlayFile(path);
    private void MediaPlayerService_FailedToLoadMusic(Music m)
    {
        _mainNavigationService.RequstNavigation(PageType.Notification, $"Failed to load music: {m.MusicName} - {m.SingerText}");
    }

    private void MediaPlayerService_OnEnd()
    {
        if (CircleMode == PlayingPreference.CircleMode.Single)
        {
            CurrentPlayingPosition = 0;
            _mediaPlayerService.Play();
        }else PlayNext();
    }

    private void AppSettingsService_OnExiting()
    {
        _currentPlayingMgr!.Data ??= new();
        _currentPlayingMgr!.Data.Music = CurrentPlaying;
        _currentPlayingMgr!.Data.Volume = CurrentPlayingVolume;
        _currentPlayingMgr!.Data.PlayMode = CircleMode;
        _currentPlayingMgr!.Data.ShowDesktopLyric = IsShowDesktopLyric;
        var temp = Playlist.ToList();
        foreach(var item in temp)
        {
            if(item.Album!=null)
            item.Album = new() { 
                Name=item.Album.Name,
                Id=item.Album.Id
            };
        }
        _playlistMgr!.Data.Playlist = temp;
    }

    private void MediaPlayerService_OnAddToPlayNext(Music obj)
    {
        int index = 0;
        if (CurrentPlaying != null)
        {
            var current = Playlist.FirstOrDefault(m => m.MusicID == CurrentPlaying.MusicID);
            index = current != null ? Playlist.IndexOf(current) : 0;
        }
        Playlist.Insert(index + 1, obj);
    }
    private void MediaPlayerService_OnAddListToPlayNext(IList<Music> obj)
    {
        int index = 0;
        if (CurrentPlaying != null)
        {
            var current = Playlist.FirstOrDefault(m => m.MusicID == CurrentPlaying.MusicID);
            index = current != null ? Playlist.IndexOf(current) : 0;
        }
        foreach (var item in obj)
        {
            Playlist.Insert(index + 1, item);
            index++;
        }
        _mainNavigationService.RequstNavigation(PageType.Notification, $"{obj.Count} songs have been added to the playlist.");
    }

    private void MediaPlayerService_OnNewPlaylistReceived(IEnumerable<Music> obj)
    {
        Playlist.Clear();
        foreach (var item in obj)
        {
            Playlist.Add(item);
        }
    }

    private WeakReference<Music[]?> _playlistSearchResult = new(null);
    private int _playlistSearchIndex = 0;
    private string? _playlistSearchKeyword;
    public Music SearchPlaylist(string keyword)
    {
        if (keyword != _playlistSearchKeyword || _playlistSearchResult == null || !_playlistSearchResult.TryGetTarget(out var result))
        {
            _playlistSearchKeyword = keyword;
            result = [.. Playlist.Where(m => TextHelper.FuzzySearch(m, keyword))];
            _playlistSearchResult = new(result);
            _playlistSearchIndex = 0;
        }
        _playlistSearchIndex= (_playlistSearchIndex + 1) % result.Length;
        return result[_playlistSearchIndex];
    }

    [RelayCommand]
    private void SetQuality()
    {
        NavigateToSettingsPage();
    }
    #endregion
    #region respond to media player controller
    [ObservableProperty]
    private double _cacheDownloadProgress = 0;
    public Action? CacheFinished { set => _mediaPlayerService.CacheFinished = value; }
    public Action? CacheStarted { set => _mediaPlayerService.CacheStarted = value; }
    private void CacheProgress(long total,long downloaded)
    {
        CacheDownloadProgress = (double)downloaded / total;
    }
    private void MediaPlayerService_OnPaused(Music obj)
    {
        IsPlaying = false;
        _timer?.Stop();
    }

    private void MediaPlayerService_OnPlay(Music m)
    {
        IsPlaying = true;
        _timer?.Start();
    }
    public event Action<string>? SyncCurrentPlayingWithPlayListPage;
    private void MediaPlayerService_OnLoaded(Music m)
    {
        App.Current.Dispatcher.Invoke(async () =>
        {
            CurrentPlaying = m;
            SyncCurrentPlayingWithPlayListPage?.Invoke(m.MusicID);
            await LyricView.LoadFromMusic(m);
        });
    }
    private void MediaPlayerService_OnQualityChanged()
    {
        App.Current.Dispatcher.Invoke(() => {
            CurrentQuality = _mediaPlayerService.CurrentQuality;
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
        if (ExMessageBox.Show("Login token has expired. Log in again."))
        {
            UserMenuViewModel.Menu_LoginQQ();
        }
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
    #region main menu definition
    [ObservableProperty]
    private Brush? avator;
    public enum MenuType { MusicPool,Mine}
    public class MainMenu(string name, Geometry icon, Type pageType, MenuType type =0,Func<object,Task>? process=null)
    {
        public string Name { get; } = name;
        public Geometry Icon { get; } = icon;
        public Type PageType { get; } = pageType;
        public MenuType Type { get; set; } = type;
        public Func<object,Task>? ProcessPage { get; } = process;
        public UIElement? Decorator { get; set; }
    }
    /// <summary>
    /// 主菜单——音乐库
    /// </summary>
    public ObservableCollection<MainMenu> MainMenus { get; set; } = [];
    private void LoadMainMenus()
    {
        IEnumerable<MainMenu> list = [
        new MainMenu("Home", (Geometry)App.Current.FindResource("Menu_Home"), typeof(HomePage)),
        new MainMenu("Rank",(Geometry)App.Current.FindResource("Menu_Ranklist"), typeof(RanklistPage)),
        new MainMenu("Singer", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(Page)),
        new MainMenu("Playlists", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(Page)),
        new MainMenu("Radio", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(Page)),

        new MainMenu("Bought", (Geometry)App.Current.FindResource("Menu_Bought"), typeof(MyBoughtPage),MenuType.Mine),
        new MainMenu("Download", (Geometry)App.Current.FindResource("Menu_Download"), typeof(DownloadPage),MenuType.Mine){
            Decorator=_downloadMenuDecorator
        },
        new MainMenu("Favorite",(Geometry)App.Current.FindResource("Menu_Favorite"), typeof(PlaylistPage),MenuType.Mine,LoadMyFavorite),
        new MainMenu("My Diss", (Geometry) App.Current.FindResource("Menu_MyDiss"), typeof(MyDissPage),MenuType.Mine)
        ];
        foreach (var item in list)
        {
            MainMenus.Add(item);
        }
    }

    [ObservableProperty]
    private MainMenu? selectedMenu;
    public Task RequireCreateNewPage()
    {
        return CreateMenuPage(SelectedMenu);
    }
    private async Task CreateMenuPage(MainMenu? value)
    {
        if (value != null)
        {
            if (value.PageType is { } type)
            {
                if (App.Host!.Services.GetService(type) is Page{ } page)
                {
                    if (value.ProcessPage != null)
                        await value.ProcessPage(page);
                    //tag of page refers to tagetted main menu
                    page.Tag = value;
                    RequestNavigateToPage?.Invoke(page);
                }
            }
        }
    }
    #endregion
    #region navigation
    public bool IsLyricPageOpen { get; set; } = false;

    [ObservableProperty]
    private bool _isLoading = false;

    public event Action<Page>? RequestNavigateToPage;
    public event Action<string>? RequestNotify;

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
            case PageType.RankPage:
                if (arg is RankListInfo { } info)
                    NavigateToRanklist(info);
                break;
            case PageType.SearchPage:
                if (arg is string { } keyword)
                    NavigateToSearchPage(keyword);
                break;
            case PageType.PlaylistPage:
                if (arg is Playlist { } listId)
                    NavigateToPlaylistPage(listId);
                break;
            case PageType.AccountInfoPage:
                NavigateToAccountInfoPage();
                break;
            case PageType.ArtistPage:
                if(arg is Profile pro)
                {
                    NavigateToSearchPage(pro.Name);
                }
                break;
            case PageType.Notification:
                if(arg is string { } str)
                RequestNotify?.Invoke(str);
                break;
            default:
                break;
        }
    }
    private void NavigateToAccountInfoPage()
    {
        var sp = _serviceProvider.GetRequiredService<AccountInfoPage>();
        if (sp != null)
        {
            RequestNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
    }
    private void NavigateToSettingsPage()
    {
        var sp = _serviceProvider.GetRequiredService<SettingsPage>();
        if (sp != null)
        {
            RequestNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
    }
    private async void NavigateToRanklist(RankListInfo info)
    {
        IsLoading = true;
        if (await _playlistDataWrapper.LoadRanklist(info) is { } page)
            RequestNavigateToPage?.Invoke(page);
        IsLoading = false;
    }
    private async void NavigateToAlbumPage(string AlbumId)
    {
        IsLoading = true;
        if(await _playlistDataWrapper.LoadAlbumPage(AlbumId) is { } page)
            RequestNavigateToPage?.Invoke(page);
        SelectedMenu = null;
        IsLoading = false;
    }
    private async void NavigateToSearchPage(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return;
        IsLoading = true;
        if(await _playlistDataWrapper.LoadSearchPage(keyword) is { } page)
            RequestNavigateToPage?.Invoke(page);
        SelectedMenu = null;
        IsLoading = false;
    }
    private async void NavigateToPlaylistPage(Playlist info)
    {
        var sp=_serviceProvider.GetRequiredService<PlaylistPage>();
        if (sp != null)
        {
            if (await LoadUserPlaylist(info) is { } vm)
            {
                sp.ViewModel = vm;
                RequestNavigateToPage?.Invoke(sp);
            }
        }
    }

    private async Task LoadMyFavorite(object page)
    {
        if(page is PlaylistPage view)
        {
            if (_userProfileService.UserProfileGetter.MyFavorite is { } info)
            {
                if(await LoadUserPlaylist(info) is { } vm)
                {
                    view.ViewModel = vm;
                }
            }
        }
    }
    private async Task<PlaylistPageViewModel?> LoadUserPlaylist(Playlist info)
    {
        IsLoading = true;
        var vm=await _playlistDataWrapper.LoadUserPlaylistVm(info);
        IsLoading = false;
        return vm;
    }

    #endregion
    #region playing
    [ObservableProperty]
    private bool _isPlaying = false;
    [ObservableProperty]
    private Music? _currentPlaying = null;
    [ObservableProperty]
    private MusicQuality _currentQuality = MusicQuality.SQ;
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
    private LyricView _lyricView;
    [ObservableProperty]
    private PlayingPreference.CircleMode _circleMode = PlayingPreference.CircleMode.Circle;
    public ObservableCollection<Music> Playlist { get;private set; } = [];
    [ObservableProperty]
    private Music? _playlistChoosen = null;

    [ObservableProperty]
    private bool _isShowDesktopLyric = false;
    private DesktopLyricWindow? lrcWindow;

    [ObservableProperty]
    private Brush? _lyricPageBackgound = null;
    partial void OnIsShowDesktopLyricChanged(bool value)
    {
        if (value)
        {
            lrcWindow = _serviceProvider.GetRequiredService<DesktopLyricWindow>();
            lrcWindow.Closed += LrcWindow_Closed;
            lrcWindow.Show();
        }
        else
        {
            lrcWindow?.Close();
            lrcWindow = null;
        }
    }

    private void LrcWindow_Closed(object? sender, EventArgs e)
    {
        lrcWindow = null;
        IsShowDesktopLyric = false;
    }

    partial void OnIsPlayingChanged(bool value)
    {
        _windowBasicComponent.UpdateThumbButtonState();
    }

    async partial void OnPlaylistChoosenChanged(Music? value)
    {
        if (value != null&&value.MusicID!=CurrentPlaying?.MusicID)
        {
            await _mediaPlayerService.LoadThenPlay(value);
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

    private async Task UpdateCover()
    {
        var hc = () => _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var cover = await ImageCacheService.FetchData(await CoverGetter.GetCoverImgUrl(hc, _userProfileService.GetAuth(), CurrentPlaying!));
        if (cover != null)
        {
            CurrentPlayingCover = new ImageBrush(cover);
            var bitmap = cover.ToBitmap();
            //Update TaskBar Info
            _windowBasicComponent.UpdateThumbInfo(bitmap);

            bool isDarkMode = _uiResourceService.GetIsDarkMode();
            if (_uiResourceService.UsingMusicTheme)
            {
                _uiResourceService.SettingsMgr.Data.AccentColor = bitmap.GetMajorColor().AdjustColor();
                _uiResourceService.UpdateAccentColor();
            }
            //process img
            bitmap.ApplyMicaEffect(isDarkMode);
            LyricPageBackgound = new ImageBrush(bitmap.ToBitmapImage());
        }
        else
        {
            LyricPageBackgound = null;
        }
    }

    async partial void OnCurrentPlayingChanged(Music? value)
    {
        if (value == null||string.IsNullOrEmpty(value.MusicID)) return;

        await UpdateCover();

        CurrentPlayingPosition = 0;
        CurrentPlayingPositionText = "00:00";

        PlaylistChoosen = value;
    }
    partial void OnCurrentPlayingVolumeChanged(double value)
    {
       _mediaPlayerService.Volume = value;
    }
    public void SetCurrentPlayingPosition(double value)
    {
        CurrentPlayingPosition = value;
        _mediaPlayerService.Position = TimeSpan.FromMilliseconds(value);
    }
    #endregion
}
