using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Playlist;

public static class NeteasePlaylistAPI
{
    public static async Task<List<Abstraction.Entities.Playlist>> GetNeteaseUserPlaylistAsync(
        HttpClient hc,NeteaseUserAuth auth)
    {
        string data = await hc.SetForNetease(auth.Cookie)
                                           .GetStringAsync($"http://music.163.com/api/user/playlist/?offset=0&limit=1000&uid={auth.Id}");
        var o = JsonObject.Parse(data);
        var dt = new List<Abstraction.Entities.Playlist>();
        foreach (var a in o["playlist"].AsArray())
        {
            int lc = -1;
            int.TryParse(a["playCount"].ToString(), out lc);
            dt.Add(new ()
            {
                Id = a["id"].ToString(),
                Name = a["name"].ToString(),
                Photo = a["coverImgUrl"].ToString(),
                Subtitle = lc != -1 ? $"{a["trackCount"]}首   {lc.IntToWn()}次播放":"",
                IsOwner = false,
                Source = Platform.wyy
            });
        }
        return dt;
    }
    public static async Task<Abstraction.Entities.Playlist> GetNeteasePlaylistByIdAsync(
            HttpClient hc, NeteaseUserAuth auth,string id)
    {
        string data = await hc.SetForNetease(auth.Cookie)
                                           .GetStringAsync($"http://music.163.com/api/v6/playlist/detail?id={id}&offset=0&total=true&limit=100000&n=10000");
        var o = JsonObject.Parse(data);
        var dt = new Abstraction.Entities.Playlist();
        var pl = o["playlist"];
        //暂时不支持网易云歌单的管理
        dt.IsOwner = false;
        dt.Source = Platform.wyy;
        dt.Name = pl["name"].ToString();
        dt.Id = pl["id"].ToString();
        dt.Photo = pl["coverImgUrl"].ToString();
        dt.Description = pl["description"]?.ToString()??"";
        dt.Creator = new Profile()
        {
            Name = pl["creator"]["nickname"].ToString(),
            Photo = pl["creator"]["avatarUrl"].ToString()
        };
        dt.Musics = [];
        var pl_t = pl["tracks"].AsArray();
        foreach (var pl_t_i in pl_t)
        {
            try
            {
                var dtname = pl_t_i["name"].ToString();
                var dtsinger = "";
                var pl_t_i_ar = pl_t_i["ar"].AsArray();
                var singers = new List<Profile>();
                foreach (var a in pl_t_i_ar)
                {
                    dtsinger += a["name"] + "/";
                    singers.Add(new Profile()
                    {
                        Name = a["name"].ToString(),
                        Mid = a["id"].ToString(),
                        Platform = Platform.wyy
                    });
                }
                dtsinger = dtsinger[..^1];
                string alia = "";
                if (pl_t_i["alia"].AsArray().Count > 0)
                    alia = pl_t_i["alia"][0].ToString();
                MusicQuality quality;
                if (!string.IsNullOrEmpty(pl_t_i["sq"]?.ToString()))
                    quality = MusicQuality.SQ;
                else if (!string.IsNullOrEmpty(pl_t_i["h"]?.ToString()))
                    quality = MusicQuality.HQ;
                else
                    quality = MusicQuality.Std;
                dt.Musics.Add(new Music()
                {
                    MusicName = dtname,
                    Singer = singers,
                    MusicName_Lyric = alia,
                    Source = Platform.wyy,
                    Quality = quality,
                    SingerText = dtsinger,
                    Album = new AlbumInfo()
                    {
                        Id = pl_t_i["al"]["id"].ToString(),
                        Name = pl_t_i["al"]["name"].ToString(),
                        Photo = pl_t_i["al"]["picUrl"].ToString(),
                        Platform= Platform.wyy
                    },
                    Mvmid = null,
                    MusicID = pl_t_i["id"].ToString()
                });
            }
            catch {  }
        }
        return dt;
    }
}
