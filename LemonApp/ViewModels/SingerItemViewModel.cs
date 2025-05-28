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

public partial class SingerItemViewModel(MainNavigationService navigationService):ObservableObject
{
    public ObservableCollection<DisplayEntity<Profile>> Singers { get; } = [];
    [RelayCommand]
    private void Select(DisplayEntity<Profile> item)
    {
        navigationService.RequstNavigation(PageType.ArtistPage,item.ListInfo);
    }
    private async Task AddOne(Profile item)
    {
        var singerItem = new DisplayEntity<Profile>(item);
        Singers.Add(singerItem);
        singerItem.Cover = await ImageCacheService.FetchData(item.Photo);
    }
    public void SetList(IEnumerable<Profile> list)
    {
        Singers.Clear();
        foreach (var item in list)
        {
            _ = AddOne(item);
        }
    }
}
