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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using LemonApp.Common.Configs;
using LemonApp.MusicLib.Media;
using CommunityToolkit.Mvvm.Input;
using System.Timers;
using LemonApp.Views.UserControls;

namespace LemonApp.ViewModels;
public partial class MainWindowViewModel : ObservableObject
{
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

        _mediaPlayerService.OnLoaded += _mediaPlayerService_OnLoaded;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;

        LyricView= lyricView;

        _mainNavigationService.OnNavigatingRequsted += MainNavigationService_OnNavigatingRequsted;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;

        _timer = new();
        _timer.Elapsed += _timer_Elapsed;
        _timer.Interval = 1000;

        LoadComponent();
    }

    private async void LoadComponent()
    {
        //update current playing
        _currentPlayingMgr = _appSettingsService.GetConfigMgr<CurrentPlaying>();
        if (_currentPlayingMgr is { } mgr)
        {
            if (mgr.Data?.Music is { MusicID: not "" } music)
            {
                await _mediaPlayerService.Load(music);
                CurrentPlayingVolume = mgr.Data.Volume;
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

    private async void _mediaPlayerService_OnLoaded(MusicDT.Music m)
    {
        CurrentPlaying = m;
        await LyricView!.LoadFromMusic(m);
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

    private readonly IServiceProvider _serviceProvider;
    private readonly UserProfileService _userProfileService;
    private readonly MainNavigationService _mainNavigationService;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly AppSettingsService _appSettingsService;

    private readonly Timer _timer;
    private SettingsMgr<CurrentPlaying>? _currentPlayingMgr;
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

    async partial void OnCurrentPlayingChanged(MusicDT.Music? value)
    {
        //只负责更新UI的ViewModel
        if (value == null||string.IsNullOrEmpty(value.MusicID)) return;

        var hc= _serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        CurrentPlayingCover = new ImageBrush(await ImageCacheHelper.FetchData(await CoverGetter.GetCoverImgUrl(hc, value)));

        CurrentPlayingPosition = 0;
        CurrentPlayingPositionText = "00:00";
        CurrentPlayingVolume = _mediaPlayerService.Volume;
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
