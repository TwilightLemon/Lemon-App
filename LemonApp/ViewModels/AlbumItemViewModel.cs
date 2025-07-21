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

public partial class DisplayEntity<T>(T item) : ObservableObject where T :notnull 
{
    public T ListInfo { get; set; } = item;
    [ObservableProperty]
    public BitmapImage? _cover;
}

public partial class AlbumItemViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    [RelayCommand]
    private void Select(DisplayEntity<AlbumInfo>? value)
    {
        if (value!=null)
        {
            mainNavigationService.RequstNavigation(PageType.AlbumPage, value.ListInfo.Id);
        }
    }
    [ObservableProperty]
    private bool isLoaded = false;
    public ObservableCollection<DisplayEntity<AlbumInfo>> Albums { get; set; } = [];
    private async Task AddOne(AlbumInfo item)
    {
        var albumItem=new DisplayEntity<AlbumInfo>(item);
        Albums.Add(albumItem);
        albumItem.Cover =await ImageCacheService.FetchData(item.Photo);
    }
    public void SetAlbumItems(IEnumerable<AlbumInfo> list)
    {
        //compare
        if (Albums.Count>0)
        {
            HashSet<string> now = Albums.Select(i => i.ListInfo.Id).ToHashSet();
            HashSet<string> reload = list.Select(i => i.Id).ToHashSet();
            if (now.SetEquals(reload)) return;
        }
        Albums.Clear();
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

    public void AppendMore(IEnumerable<AlbumInfo> list)
    {
        foreach (var item in list)
        {
            _ = AddOne(item);
        }
    }
}
