using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Album;
using LemonApp.MusicLib.Playlist;
using LemonApp.MusicLib.RankList;
using LemonApp.MusicLib.Search;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonApp.Components;
//TODO: simplify
public class PlaylistDataWrapper(IServiceProvider sp,MediaPlayerService ms, UserProfileService user)
{
    public async  Task<Page?> LoadRanklist(RankListInfo info)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        if (page != null && hc != null && vm != null)
        {
            var data = await RankListAPI.GetRankListData(info.Id, hc);
            if (data != null)
            {
                vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(info.CoverUrl));
                vm.Description = info.Description;
                vm.ListName = info.Name;
                vm.PlaylistType = PlaylistType.Ranklist;
                vm.InitMusicList(data);
                vm.CreatorName = "QQ Music Official";
                vm.CreatorAvatar = Brushes.SkyBlue;
                vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);
                page.ViewModel = vm;
                return page;
            }
        }
        return null;
    }

    public async Task<Page?> LoadAlbumPage(string AlbumId)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        if (page != null && hc != null && auth != null)
        {
            var data = await AlbumAPI.GetAlbumTracksByIdAync(hc, auth, AlbumId);
            vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(data.Photo));
            vm.Description = data.Description ?? "";
            vm.ListName = data.Name;
            vm.InitMusicList(data.Musics!);
            vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator!.Photo));
            vm.CreatorName = data.Creator.Name;
            vm.PlaylistType = PlaylistType.Album;

            vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);

            page.ViewModel = vm;
            return page;
        }
        return null;
    }

    public async Task<Page?> LoadSearchPage(string keyword)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        if (page != null && hc != null && auth != null && vm != null)
        {
            vm.ShowInfoView = false;
            var search = await SearchAPI.SearchMusicAsync(hc, auth, keyword);
            vm.InitMusicList(search);
            vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);
            vm.PlaylistType = PlaylistType.Other;

            page.ViewModel = vm;
            return page;
        }
        return page;
    }

    public async Task<PlaylistPageViewModel?> LoadUserPlaylistVm(string id)
    {
        var hc = sp.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        if (hc != null && auth != null && vm != null)
        {
            var data = await PublicPlaylistAPI.LoadPlaylistById(hc, auth, id);
            if (data != null)
            {
                vm.Cover = new ImageBrush(await ImageCacheHelper.FetchData(data.Photo));
                vm.Description = data.Description ?? "";
                vm.ListName = data.Name;
                vm.InitMusicList(data.Musics!);
                vm.CreatorAvatar = new ImageBrush(await ImageCacheHelper.FetchData(data.Creator!.Photo));
                vm.CreatorName = data.Creator.Name;
                vm.PlaylistType = PlaylistType.Playlist;

                vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);
            }
        }
       return vm;
    }
}
