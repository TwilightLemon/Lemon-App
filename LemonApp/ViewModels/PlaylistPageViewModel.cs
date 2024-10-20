using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Navigation;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.ViewModels;

public partial class PlaylistPageViewModel(
    MainNavigationService navigationService
    ) :ObservableObject
{
    private readonly MainNavigationService _navigationService =navigationService;
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

    [RelayCommand]
    private void GotoAlbumPage(string albumId)
    {
        _navigationService.RequstNavigation(PageType.AlbumPage, albumId);
    }

}
