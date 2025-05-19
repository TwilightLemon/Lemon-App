using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Album;
using LemonApp.MusicLib.Playlist;
using LemonApp.MusicLib.RankList;
using LemonApp.MusicLib.Search;
using LemonApp.MusicLib.Singer;
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

public class PlaylistDataWrapper(IServiceProvider sp,
                                 MediaPlayerService ms,
                                 UserProfileService user,
                                 IHttpClientFactory hcf)
{
    public async Task<Page?> LoadSingerPage(string singerId)
    {
        var page = sp.GetRequiredService<SingerPage>();
        var vm = sp.GetRequiredService<SingerPageViewModel>();
        var hc = hcf.CreateClient(App.PublicClientFlag);
        var data = await SingerAPI.GetPageDataAsync(hc, singerId, user.GetAuth());
        if (data != null)
        {
            vm.SingerPageData = data;
            vm.CoverImg= new ImageBrush(await ImageCacheService.FetchData(data.SingerProfile.Photo));
            if (!string.IsNullOrEmpty(data.BigBackground))
            {
                vm.BigBackground = new ImageBrush(await ImageCacheService.FetchData(data.BigBackground)) { Stretch = Stretch.UniformToFill };
            }

            page.DataContext = vm;
        }
        return page;
    }
    public async  Task<Page?> LoadRanklist(RankListInfo info)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        var hc = hcf.CreateClient(App.PublicClientFlag);
        if (page != null && hc != null && vm != null)
        {
            var data = await RankListAPI.GetRankListData(info.Id, hc);
            if (data != null)
            {
                vm.Cover = new ImageBrush(await ImageCacheService.FetchData(info.CoverUrl));
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
        var hc = hcf.CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        if (page != null && hc != null && auth != null)
        {
            var data = await AlbumAPI.GetAlbumTracksByIdAync(hc, auth, AlbumId);
            vm.Cover = new ImageBrush(await ImageCacheService.FetchData(data.Photo));
            vm.Description = data.Description ?? "";
            vm.ListName = data.Name;
            vm.InitMusicList(data.Musics!);
            vm.CreatorAvatar = new ImageBrush(await ImageCacheService.FetchData(data.Creator!.Photo));
            vm.CreatorName = data.Creator.Name;
            vm.PlaylistType = PlaylistType.Album;

            vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);

            page.ViewModel = vm;
            return page;
        }
        return null;
    }

    public async Task<Page?> LoadSongsOfSinger(string singerId)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        var hc = hcf.CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        if (page != null && hc != null && auth != null && vm != null)
        {
            vm.ShowInfoView = false;
            var data = await SingerAPI.GetSongsOfSingerAsync(hc, singerId, auth);
            vm.InitMusicList(data);
            vm.PlaylistType = PlaylistType.Other;
            vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);
            vm.OnLoadMoreRequired = async (sender, index) => { 
            sender.AppendMusicList(await SingerAPI.GetSongsOfSingerAsync(hc,singerId,auth,index));
            };

            page.ViewModel = vm;
            return page;
        }
        return null;
    }

    public async Task<Page?> LoadSearchPage(string keyword)
    {
        var page = sp.GetRequiredService<PlaylistPage>();
        var hc = hcf.CreateClient(App.PublicClientFlag);
        var auth = user.GetAuth();
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        if (page != null && hc != null && auth != null && vm != null)
        {
            vm.ShowInfoView = false;
            var search = await SearchAPI.SearchMusicAsync(hc, auth, keyword);
            vm.InitMusicList(search);
            vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);//??优化一下
            vm.PlaylistType = PlaylistType.Other;
            vm.OnLoadMoreRequired =async (sender, index) => {//index在内部自增
                sender.AppendMusicList(await SearchAPI.SearchMusicAsync(hc, auth, keyword, index));
            };

            page.ViewModel = vm;
            return page;
        }
        return page;
    }

    public async Task<PlaylistPageViewModel?> LoadUserPlaylistVm(Playlist info)
    {
        var hc = hcf.CreateClient(App.PublicClientFlag);
        var vm = sp.GetRequiredService<PlaylistPageViewModel>();
        if (hc != null && vm != null)
        {
            var data =info.Source==Platform.qq? await PublicPlaylistAPI.LoadPlaylistById(hc, user.GetAuth(), info.Id)
                :await NeteasePlaylistAPI.GetNeteasePlaylistByIdAsync(hc,user.GetNeteaseAuth(),info.Id);
            if (data != null)
            {
                vm.Cover = new ImageBrush(await ImageCacheService.FetchData(data.Photo));
                vm.Description = data.Description ?? "";
                vm.ListName = data.Name;
                vm.InitMusicList(data.Musics!);
                vm.CreatorAvatar = new ImageBrush(await ImageCacheService.FetchData(data.Creator!.Photo));
                vm.CreatorName = data.Creator.Name;
                vm.PlaylistType = PlaylistType.Playlist;
                vm.IsOwned = info.IsOwner;
                vm.Dirid = info.DirId;

                vm.UpdateCurrentPlaying(ms.CurrentMusic?.MusicID);
            }
        }
       return vm;
    }
}
