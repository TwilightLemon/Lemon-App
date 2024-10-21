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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LemonApp.ViewModels;
public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        UserProfileService userProfileService,
        IServiceProvider serviceProvider,
        MainNavigationService mainNavigationService)
    {
        _userProfileService = userProfileService;
        _serviceProvider = serviceProvider;
        _mainNavigationService = mainNavigationService;
        _mainNavigationService.OnNavigatingRequsted += MainNavigationService_OnNavigatingRequsted;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;
    }
    private readonly IServiceProvider _serviceProvider;
    private readonly UserProfileService _userProfileService;
    private readonly MainNavigationService _mainNavigationService;
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


    public bool IsLyricPageOpen { get; set; } = false;

    [ObservableProperty]
    private Brush avator = Brushes.LightPink;

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
        var auth = _serviceProvider.GetRequiredService<UserProfileService>().GetAuth();
        if (sp != null && hc != null && auth != null)
        {
            var data = await AlbumAPI.GetAlbumTracksByIdAync(hc, auth, AlbumId);
            vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(data.Photo));
            vm.Description = data.Description ?? "";
            vm.ListName = data.Name;
            foreach (var item in data.Musics)
            {
                vm.Musics.Add(item);
            }
            vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator.Photo));
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
        var auth = _serviceProvider.GetRequiredService<UserProfileService>().GetAuth();
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

}
