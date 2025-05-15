using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.ViewModels;

public record class SingerItem(Profile ListInfo,BitmapImage? Cover);
public partial class SingerItemViewModel(MainNavigationService navigationService):ObservableObject
{
    public ObservableCollection<SingerItem> Singers { get; } = [];
    [RelayCommand]
    private void Select(SingerItem item)
    {
        navigationService.RequstNavigation(PageType.ArtistPage,item.ListInfo);
    }
    public async Task SetList(IEnumerable<Profile> list)
    {
        Singers.Clear();
        foreach (var item in list)
        {
            var cover = await ImageCacheService.FetchData(item.Photo);
            Singers.Add(new(item, cover));
        }
    }
}
