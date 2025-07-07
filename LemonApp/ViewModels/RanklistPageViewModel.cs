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
using CommunityToolkit.Mvvm.Input;

namespace LemonApp.ViewModels;

public partial class RanklistPageViewModel(
    MainNavigationService mainNavigationService,
    IHttpClientFactory httpClientFactory) : ObservableObject
{
    private readonly HttpClient hc=httpClientFactory.CreateClient(App.PublicClientFlag);

    [RelayCommand]
    private void Select(DisplayEntity<RankListInfo>? value)
    {
        if (value != null)
        {
            mainNavigationService.RequstNavigation(PageType.RankPage, value.ListInfo);
        }
    }
    public ObservableCollection<DisplayEntity<RankListInfo>> Ranklists { get; set; } = [];
    private async Task AddOne(RankListInfo item)
    {
        var rankItem = new DisplayEntity<RankListInfo>(item);
        Ranklists.Add(rankItem);
        rankItem.Cover = await ImageCacheService.FetchData(item.CoverUrl);
    }
    public async Task LoadData()
    {
        if (Ranklists.Count > 0) return;
        var data = await RankListAPI.GetRankListIndexAsync(hc);
        if (data != null)
        {
            Ranklists.Clear();
            foreach ( var item in data)
            {
                _ = AddOne(item);
            }
        }
    }
}
