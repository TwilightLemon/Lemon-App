using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Funcs;
using LemonApp.Native;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Album;
using LemonApp.MusicLib.Search;
using LemonApp.Services;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Taskbar;
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
using LemonApp.MusicLib.Playlist;
using System.Threading.Tasks;
using System.Windows;
using LemonApp.Views.Windows;
using Task = System.Threading.Tasks.Task;
using LemonApp.MusicLib.RankList;
using LemonApp.MusicLib.Abstraction.Entities;

//TODO: 将功能再细分为Component 简化ViewModel
namespace LemonApp.ViewModels;
public partial class MainWindowViewModel : ObservableObject,IDisposable
{
    #region fields & constructor
    private readonly IServiceProvider _serviceProvider;
    private readonly UserProfileService _userProfileService;
    private readonly MainNavigationService _mainNavigationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly AppSettingsService _appSettingsService;
    private readonly UIResourceService _uiResourceService;

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
        DesktopLyricWindowViewModel lyricWindowViewModel)
    {
        _userProfileService = userProfileService;
        _serviceProvider = serviceProvider;
        _mainNavigationService = mainNavigationService;
        _mediaPlayerService = mediaPlayerService;
        _appSettingsService = appSettingsService;
        _uiResourceService = uIResourceService;

        _uiResourceService.OnColorModeChanged += UIResourceService_OnColorModeChanged;

        _appSettingsService.OnExiting += _appSettingsService_OnExiting;

        _mediaPlayerService.OnLoaded += _mediaPlayerService_OnLoaded;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;
        _mediaPlayerService.OnNewPlaylistReceived += _mediaPlayerService_OnNewPlaylistReceived;
        _mediaPlayerService.OnAddToPlayNext += _mediaPlayerService_OnAddToPlayNext;
        _mediaPlayerService.OnAddListToPlayNext += _mediaPlayerService_OnAddListToPlayNext;
        _mediaPlayerService.OnEnd += _mediaPlayerService_OnEnd;
        _mediaPlayerService.OnPlayNext += PlayNext;
        _mediaPlayerService.OnPlayLast += PlayLast;
        _mediaPlayerService.CacheProgress = CacheProgress;

        LyricView = lyricView;
        LyricView.OnNextLrcReached += LyricView_OnNextLrcReached;

        _lyricWindowViewModel = lyricWindowViewModel;

        _mainNavigationService.OnNavigationRequested += MainNavigationService_OnNavigatingRequsted;
        _mainNavigationService.LoadingAniRequested+=()=>IsLoading=true;
        _mainNavigationService.LoadingAniCancelled+=()=>IsLoading=false;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;

        _timer = new();
        _timer.Elapsed += _timer_Elapsed;
        _timer.Interval = 1000;

        LoadMainMenus();
        LoadComponent();
        FetchIconResource();
    }


    private async void UIResourceService_OnColorModeChanged()
    {
        await UpdateCover();
    }

    private void LyricView_OnNextLrcReached(LrcLine obj)
    {
        _lyricWindowViewModel.Update(obj);
    }
    public void Dispose()
    {
        NotifyIcon?.Dispose();
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

    private void _mediaPlayerService_OnAddToPlayNext(Music obj)
    {
        int index = 0;
        if (CurrentPlaying != null)
        {
            var current = Playlist.FirstOrDefault(m => m.MusicID == CurrentPlaying.MusicID);
            index = current != null ? Playlist.IndexOf(current) : 0;
        }
        Playlist.Insert(index + 1, obj);
    }
    private void _mediaPlayerService_OnAddListToPlayNext(IEnumerable<Music> obj)
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
    }

    private void _mediaPlayerService_OnNewPlaylistReceived(IEnumerable<Music> obj)
    {
        Playlist.Clear();
        foreach (var item in obj)
        {
            Playlist.Add(item);
        }
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
    private void _mediaPlayerService_OnPaused(Music obj)
    {
        IsPlaying = false;
        _timer?.Stop();
    }

    private void _mediaPlayerService_OnPlay(Music m)
    {
        IsPlaying = true;
        _timer?.Start();
    }
    public event Action<string>? SyncCurrentPlayingWithPlayListPage;
    private void _mediaPlayerService_OnLoaded(Music m)
    {
        App.Current.Dispatcher.Invoke(async () =>
        {
            CurrentPlaying = m;
            CurrentQuality = _mediaPlayerService.CurrentQuality;
            SyncCurrentPlayingWithPlayListPage?.Invoke(m.MusicID);
            await LyricView.LoadFromMusic(m);
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
        if (ExMessageBox.Show("登录已失效，重新登录？"))
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
    #region main menu
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

        new MainMenu("Bought", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(Page),MenuType.Mine),
        new MainMenu("Download", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(Page),MenuType.Mine),
        new MainMenu("Favorite",(Geometry)App.Current.FindResource("Menu_Favorite"), typeof(PlaylistPage),MenuType.Mine,LoadMyFavorite),
        new MainMenu("My Diss", (Geometry) App.Current.FindResource("Menu_MyDiss"), typeof(PlaylistItemPage),MenuType.Mine,LoadMyDiss)
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
                if (arg is string { } listId)
                    NavigateToPlaylistPage(listId);
                break;
            case PageType.AccountInfoPage:
                NavigateToAccountInfoPage();
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
        var page=_serviceProvider.GetRequiredService<PlaylistPage>();
        var vm= _serviceProvider.GetRequiredService<PlaylistPageViewModel>();
        var hc = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        if (page != null && hc != null && vm!=null)
        {
            var data = await RankListAPI.GetRankListData(info.Id, hc);
            if (data != null)
            {
                vm.Cover= new ImageBrush(await ImageCacheHelper.FetchData(info.CoverUrl));
                vm.Description = info.Description;
                vm.ListName = info.Name;
                vm.PlaylistType = PlaylistType.Ranklist;
                vm.InitMusicList(data);
                vm.CreatorName = "QQ Music Official";
                vm.CreatorAvatar = Brushes.SkyBlue;
                vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);
                page.ViewModel = vm;
                RequestNavigateToPage?.Invoke(page);
            }
        }
        IsLoading = false;
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
            vm.InitMusicList(data.Musics!);
            vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator!.Photo));
            vm.CreatorName = data.Creator.Name;
            vm.PlaylistType = PlaylistType.Album;

            vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);

            sp.ViewModel = vm;
            RequestNavigateToPage?.Invoke(sp);
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
            vm.InitMusicList(search);
            vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);
            vm.PlaylistType = PlaylistType.Other;

            sp.ViewModel = vm;
            RequestNavigateToPage?.Invoke(sp);
        }
        SelectedMenu = null;
        IsLoading = false;
    }
    private async void NavigateToPlaylistPage(string id)
    {
        var sp=_serviceProvider.GetRequiredService<PlaylistPage>();
        if (sp != null)
        {
            if (await LoadUserPlaylist(id) is { } vm)
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
            if (_userProfileService.UserProfileGetter.MyFavorite?.Id is { } id)
            {
                if(await LoadUserPlaylist(id) is { } vm)
                {
                    view.ViewModel = vm;
                }
            }
        }
    }
    private async Task LoadMyDiss(object page)
    {
        IsLoading = true;
        if (page is PlaylistItemPage view)
        {
            if (_userProfileService.UserProfileGetter.MyPlaylists is { } list)
            {
                if (_serviceProvider.GetRequiredService<PlaylistItemViewModel>() is { } vm)
                {
                    await vm.SetPlaylistItems(list);
                    view.MyDissViewModel = vm;
                }
            }
        }
        IsLoading = false;
    }
    private async Task<PlaylistPageViewModel?> LoadUserPlaylist(string id)
    {
        IsLoading = true;
        var hc = _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = _userProfileService.GetAuth();
        var vm = _serviceProvider.GetRequiredService<PlaylistPageViewModel>();
        if (hc != null && auth != null && vm != null)
        {
            var data = await PublicPlaylistAPI.LoadPlaylistById(hc, auth, id);
            if (data != null)
            {
                vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(data.Photo));
                vm.Description = data.Description ?? "";
                vm.ListName = data.Name;
                vm.InitMusicList(data.Musics!);
                vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator!.Photo));
                vm.CreatorName = data.Creator.Name;
                vm.PlaylistType = PlaylistType.Playlist;

                vm.UpdateCurrentPlaying(CurrentPlaying?.MusicID);
            }
        }
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
    private MusicQuality _currentQuality;
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
        UpdateThumbButtonState();
    }

    async partial void OnPlaylistChoosenChanged(Music? value)
    {
        if (value != null&&value.MusicID!=CurrentPlaying?.MusicID)
        {
            await _mediaPlayerService.Load(value);
            _mediaPlayerService.Play();
        }
    }
    [RelayCommand]
    private void ShowDesktopLyric()
    {
        IsShowDesktopLyric = !IsShowDesktopLyric;
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
        var cover = await ImageCacheHelper.FetchData(await CoverGetter.GetCoverImgUrl(hc, _userProfileService.GetAuth(), CurrentPlaying!), true);
        if (cover != null)
        {
            CurrentPlayingCover = new ImageBrush(cover);
            var bitmap = cover.ToBitmap();
            //Update TaskBar Info
            UpdateThumbInfo(bitmap);

            //process img
            bitmap.ApplyMicaEffect(_uiResourceService.GetIsDarkMode());
            LyricPageBackgound = new ImageBrush(bitmap.ToBitmapImage());
        }
        else
        {
            LyricPageBackgound = null;
        }
    }

    async partial void OnCurrentPlayingChanged(Music? value)
    {
        //只负责更新UI的ViewModel
        if (value == null||string.IsNullOrEmpty(value.MusicID)) return;

        await UpdateCover();

        CurrentPlayingPosition = 0;
        CurrentPlayingPositionText = "00:00";
        //CurrentPlayingVolume = _mediaPlayerService.Volume;

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
    #region Task Bar Thumb
    TabbedThumbnail? TaskBarImg;
    ThumbnailToolBarButton? TaskBarBtn_Last;
    ThumbnailToolBarButton? TaskBarBtn_Play;
    ThumbnailToolBarButton? TaskBarBtn_Next;

    System.Drawing.Icon? icon_play, icon_pause, icon_last, icon_next,icon_app;

    private void FetchIconResource()
    {
        icon_play = Properties.Resources.play;
        icon_pause = Properties.Resources.pause;
        icon_last = Properties.Resources.left;
        icon_next = Properties.Resources.right;
        icon_app = Properties.Resources.icon;
    }

    public void InitTaskBarThumb()
    {
        var _window = App.Current.MainWindow;
        TaskBarImg = new(_window, _window, new Vector());
        TaskBarImg.Title = "Lemon App";
        TaskBarImg.SetWindowIcon(Properties.Resources.icon);
        TaskBarImg.TabbedThumbnailActivated += delegate
        {
            _window.WindowState = WindowState.Normal;
            _window.Activate();
        };
        TaskBarBtn_Last = new (icon_last, "上一曲");
        TaskBarBtn_Last.Enabled = true;
        TaskBarBtn_Last.Click += delegate {
            PlayLast();
        };

        TaskBarBtn_Play = new (icon_play, "播放|暂停");
        TaskBarBtn_Play.Enabled = true;
        TaskBarBtn_Play.Click += delegate {
            PlayPause();
        };

        TaskBarBtn_Next = new(icon_next, "下一曲");
        TaskBarBtn_Next.Enabled = true;
        TaskBarBtn_Next.Click += delegate {
            PlayNext();
        };

        TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(TaskBarImg);
        TaskbarManager.Instance.ThumbnailToolBars.AddButtons(_window, TaskBarBtn_Last, TaskBarBtn_Play, TaskBarBtn_Next);
    }

    private void UpdateThumbButtonState()
    {
        if(TaskBarBtn_Play!=null)
        TaskBarBtn_Play.Icon = IsPlaying ? icon_pause : icon_play;
    }

    private void UpdateThumbInfo(System.Drawing.Bitmap cover)
    {
        if (cover == null||TaskBarImg==null) return;
        TaskBarImg.SetImage(cover);
        TaskBarImg.Tooltip = CurrentPlaying?.MusicName+" - "+ CurrentPlaying?.SingerText;
    }
    #endregion
    #region NotifyIcon
    System.Windows.Forms.NotifyIcon? NotifyIcon;
    public void InitNotifyIcon()
    {
        NotifyIcon = new()
        {
            Icon = icon_app,
            Text = "Lemon App",
            Visible=true
        };
        NotifyIcon.MouseClick += NotifyIcon_MouseClick;
        NotifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
    }

    private void NotifyIcon_MouseDoubleClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        var _window = App.Current.MainWindow;
        _window.ShowWindow();
    }
    private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Right)
        {
            if (_serviceProvider.GetRequiredService<NotifyIconMenuWindow>() is { } menu)
            {
                //计算窗口弹出的坐标
                var point = System.Windows.Forms.Cursor.Position;
                var dpi = VisualTreeHelper.GetDpi(App.Current.MainWindow);
                menu.Left = point.X / dpi.DpiScaleX;
                menu.Top = point.Y / dpi.DpiScaleY - menu.Height;

                menu.Show();
                menu.Activate();
            }
        }
    }
    #endregion
}
