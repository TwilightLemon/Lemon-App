using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.ViewModels;

public class PlaylistItem(Playlist item,BitmapImage? cover)
{
    public Playlist ListInfo { get; set; } = item;
    public BitmapImage? Cover { get; set; } = cover;
}
public partial class PlaylistItemViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    [RelayCommand]
    private void Select(PlaylistItem value)
    {
        mainNavigationService.RequstNavigation(PageType.PlaylistPage, value.ListInfo.Id);
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
