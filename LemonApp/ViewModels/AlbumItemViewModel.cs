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

public class AlbumItem(AlbumInfo item,BitmapImage? cover)
{
    public AlbumInfo ListInfo { get; set; } = item;
    public BitmapImage? Cover { get; set; } = cover;
}
public partial class AlbumItemViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    [RelayCommand]
    private void Select(AlbumItem? value)
    {
        if (value!=null)
        {
            mainNavigationService.RequstNavigation(PageType.AlbumPage, value.ListInfo.Id);
        }
    }

    public ObservableCollection<AlbumItem> Albums { get; set; } = [];
    public async Task SetAlbumItems(IEnumerable<AlbumInfo> list)
    {
        Albums.Clear();
        foreach (var item in list)
        {
            var cover = await ImageCacheHelper.FetchData(item.Photo);
            Albums.Add(new(item,cover));
        }
    }
}
