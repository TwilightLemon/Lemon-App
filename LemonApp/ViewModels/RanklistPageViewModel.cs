using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.RankList;
using LemonApp.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.ViewModels;

public class RanklistItem(RankListInfo item, BitmapImage? cover)
{
    public RankListInfo ListInfo { get; set; } = item;
    public BitmapImage? Cover { get; set; } = cover;
}
public partial class RanklistPageViewModel(
    MainNavigationService mainNavigationService,
    IHttpClientFactory httpClientFactory) : ObservableObject
{
    private readonly HttpClient hc=httpClientFactory.CreateClient(App.PublicClientFlag);

    [ObservableProperty]
    private RanklistItem? _choosenItem;

    partial void OnChoosenItemChanged(RanklistItem? value)
    {
        if (value != null)
        {
            mainNavigationService.RequstNavigation(PageType.RankPage, value.ListInfo);
        }
    }
    public ObservableCollection<RanklistItem> Ranklists { get; set; } = [];
    private async Task SetRanklistItems(IEnumerable<RankListInfo> list)
    {
        Ranklists.Clear();
        var temp = new List<RanklistItem>();
        foreach (var item in list)
        {
            var cover = await ImageCacheHelper.FetchData(item.CoverUrl);
            temp.Add(new(item, cover));
        }
        foreach (var item in temp)
        {
            Ranklists.Add(item);
        }
    }
    public async Task LoadData()
    {
        if (Ranklists.Count > 0) return;
        var data = await RankListAPI.GetRankListIndexAsync(hc);
        if (data != null)
        {
            await SetRanklistItems(data);
        }
    }
}
