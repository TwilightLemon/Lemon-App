using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Funcs;
using LemonApp.Services;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static LemonApp.MusicLib.Abstraction.Playlist.DataTypes;

namespace LemonApp.ViewModels;

public partial class PlaylistItem(Playlist item,BitmapImage? cover)
{
    public Playlist ListInfo { get; set; } = item;
    public BitmapImage? Cover { get; set; } = cover;
}
public partial class PlaylistItemPageViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    private readonly MainNavigationService _mainNavigationService = mainNavigationService;
    [ObservableProperty]
    private string _title = string.Empty;
    [ObservableProperty]
    private PlaylistItem? _choosenItem;

    partial void OnChoosenItemChanged(PlaylistItem? value)
    {
        if (value!=null)
        {
            _mainNavigationService.RequstNavigation(PageType.PlaylistPage, value.ListInfo.Id);
        }
    }

    public ObservableCollection<PlaylistItem> Playlists { get; set; } = [];
    public async Task SetPlaylistItems(IEnumerable<Playlist> list)
    {
        Playlists.Clear();
        foreach (var item in list)
        {
            var cover = await ImageCacheHelper.FetchData(item.Photo);
            Playlists.Add(new(item,cover));
        }
    }
}
