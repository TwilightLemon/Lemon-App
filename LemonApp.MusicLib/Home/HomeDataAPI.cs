using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.Home;

public static class HomeDataAPI
{
    public static async Task<HomePageEntities?> GetHomePageDataAsync(HttpClient hc,TencUserAuth auth)
    {
        //1. Client Recommended playlists for user
        string req = "{\"req_0\":{\"module\":\"playlist.HotRecommendServer\",\"method\":\"get_new_hot_recommend\",\"param\":{\"cmd\":0,\"page\":0,\"daily_page\":0,\"size\":1}},\"comm\":{\"g_tk\":" + auth.G_tk + ",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1751}}";
        string data1 = await (await hc.SetForUYV17(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", req)).AsTextAsync();

        var json1 = JsonObject.Parse(data1)["req_0"]["data"]["modules"].AsArray();
        var gf = json1[0]["grids"];
        List <Abstraction.Entities.Playlist> recommendPlaylists = [];
        foreach (var ab in gf.AsArray())
        {
            Abstraction.Entities.Playlist d = new()
            {
                Id = ab["id"].ToString(),
                Name = ab["title"].ToString(),
                Photo = ab["picurl"].ToString()
            };
            int lc = int.Parse(ab["listeners"].ToString());
            d.Subtitle =TextHelper.IntToWn(lc)??"Just For Today";
            recommendPlaylists.Add(d);
        }

        var explore = new List<Abstraction.Entities.Playlist>();
        var dr = json1[1]["grids"];
        foreach (var ab in dr.AsArray())
        {
            Abstraction.Entities.Playlist d = new()
            {
                Id = ab["id"].ToString(),
                Name = ab["title"].ToString(),
                Photo = ab["picurl"].ToString(),
                Subtitle =TextHelper.IntToWn( int.Parse(ab["listeners"].ToString()))
            };
            explore.Add(d);
        }

        //2.
        string data2 = await hc.SetForMusicuFcg(auth.Cookie).GetStringAsync($"https://u.y.qq.com/cgi-bin/musicu.fcg?-=recom9439610432420651&g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&data=%7B%22comm%22%3A%7B%22ct%22%3A24%7D%2C%22recomPlaylist%22%3A%7B%22method%22%3A%22get_hot_recommend%22%2C%22param%22%3A%7B%22async%22%3A1%2C%22cmd%22%3A2%7D%2C%22module%22%3A%22playlist.HotRecommendServer%22%7D%2C%22new_song%22%3A%7B%22module%22%3A%22newsong.NewSongServer%22%2C%22method%22%3A%22get_new_song_info%22%2C%22param%22%3A%7B%22type%22%3A5%7D%7D%2C%22new_album%22%3A%7B%22module%22%3A%22newalbum.NewAlbumServer%22%2C%22method%22%3A%22get_new_album_info%22%2C%22param%22%3A%7B%22area%22%3A1%2C%22sin%22%3A0%2C%22num%22%3A10%7D%7D%7D");
        var json2 = JsonObject.Parse(data2);

        //----歌单推荐---
        var recomPlaylist_obj = json2["recomPlaylist"]["data"]["v_hot"].AsArray();
        foreach (var rp in recomPlaylist_obj)
        {
            Abstraction.Entities.Playlist md = new()
            {
                Id = rp["content_id"].ToString(),
                Name = rp["title"].ToString(),
                Photo = rp["cover"].ToString().Replace("http://", "https://"),
                Subtitle =TextHelper.IntToWn( int.Parse(rp["listen_num"].ToString()))
            };
            explore.Add(md);
        }

        //----新歌首发---
        var newMusic = new List<Music>();
        var new_song_obj = json2["new_song"]["data"]["songlist"].AsArray();
        foreach (var ns in new_song_obj)
        {
            Music m = new Music();
            var amid = ns["album"]["mid"].ToString();
            if (amid != "")
                m.Album = new AlbumInfo()
                {
                    Id = amid,
                    Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000",
                    Name = ns["album"]["name"].ToString()
                };
            m.MusicID = ns["mid"].ToString();
            m.MusicName = ns["name"].ToString();
            m.MusicName_Lyric = ns["subtitle"].ToString();
            m.Singer = new List<Profile>();
            m.SingerText = "";
            foreach (var s in ns["singer"].AsArray())
            {
                m.Singer.Add(new() { Mid = s["mid"].ToString(), Name = s["name"].ToString() });
            }
            m.SingerText = string.Join('/',m.Singer.Select(s => s.Name));
            newMusic.Add(m);
        }
        return new(newMusic, recommendPlaylists, explore);
    }
}
