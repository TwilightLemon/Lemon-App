using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.ViewModels;


public partial class PlaylistItemViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    [ObservableProperty]
    private bool isLoaded = false;
    [RelayCommand]
    private void Select(DisplayEntity<Playlist> value)
    {
        mainNavigationService.RequstNavigation(PageType.PlaylistPage, value.ListInfo);
    }

    public ObservableCollection<DisplayEntity<Playlist>> Playlists { get; set; } = [];
    private async Task AddOne(Playlist item)
    {
        var playlistItem = new DisplayEntity<Playlist>(item);
        Playlists.Add(playlistItem);
        playlistItem.Cover = await ImageCacheService.FetchData(item.Photo);
    }
    public void SetPlaylistItems(IEnumerable<Playlist> list)
    {
        //compare
        if (Playlists.Count > 0)
        {
            static string selector(Playlist i) => i.Id + i.Name + i.Description;
            HashSet<string> now = Playlists.Select(i => selector(i.ListInfo)).ToHashSet();
            HashSet<string> reload = list.Select(selector).ToHashSet();
            if (now.SetEquals(reload)) return;
        }
        Playlists.Clear();
        List<Task> tasks = [];
        foreach (var item in list)
        {
            tasks.Add(AddOne(item));
        }
        Task.WhenAll(tasks).ContinueWith(t =>
        {
            if (t.IsCompleted)
                IsLoaded = true;
        });
    }
}
