using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class SingerPageViewModel(
    MainNavigationService navigationService,
    MediaPlayerService mediaPlayerService):ObservableObject
{
    /// <summary>
    /// 在Page中保持不变，无需为Observable
    /// </summary>
    public SingerPageData? SingerPageData { get; set; }

    [ObservableProperty]
    private Brush? _coverImg;
    [ObservableProperty]
    private Music? _selectedHotSong;
    [ObservableProperty]
    private Brush? _bigBackground;

    private bool _hasAddToPlayList;
    public async Task PlayHotSongs()
    {
        if (SelectedHotSong == null|| SingerPageData==null) return;
        if (!_hasAddToPlayList)
            mediaPlayerService.ReplacePlayList(SingerPageData.HotMusics);
        _hasAddToPlayList = true;

        await mediaPlayerService.LoadThenPlay(SelectedHotSong);
    }
    [RelayCommand]
    private void GoToAlbumPage(string albumId)
    {
        navigationService.RequstNavigation(PageType.AlbumPage, albumId);
    }
    
}
