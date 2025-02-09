using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.Album;
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8602 // 解引用可能出现空引用。
public static class AlbumAPI
{
    public static async Task<AlbumInfo> GetAlbumTracksByIdAync(
        HttpClient hc,
        TencUserAuth auth,
        string albumId)
    {
        string url=$"https://c.y.qq.com/v8/fcg-bin/fcg_v8_album_info_cp.fcg?ct=24&albummid={albumId}&g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&song_begin=0&song_num=50";
        string data=await hc.SetForCYQ(auth.Cookie!)
                            .GetStringAsync(url);
        JsonNode o= JsonNode.Parse(data);
        AlbumInfo md = new();
        md.Id = albumId;
        md.Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{albumId}.jpg?max_age=2592000";
        md.Name = o["data"]["name"].ToString();
        var musics = new List<Music>();
        var list = o["data"]["list"];
        md.Description = o["data"]["desc"].ToString();
        md.Creator = new Profile()
        {
            Name = o["data"]["singername"].ToString(),
            Mid = o["data"]["singermid"].ToString(),
            Photo = $"https://y.gtimg.cn/music/photo_new/T001R500x500M000{o["data"]["singermid"].ToString()}.jpg?max_age=2592000"
        };
        int i = 0;
        foreach (var a in list.AsArray())
        {
            Music m = new();
            m.Album = md;
            m.Littleid = a["songid"].ToString();
            m.MusicID = a["songmid"].ToString();
            m.MusicName = a["songname"].ToString();
            m.Singer = new List<Profile>();
            List<string> singers = [];
            foreach (var s in a["singer"].AsArray())
            {
                string mid = s["mid"].ToString(),
                    name = s["name"].ToString();
                m.Singer.Add(new Profile() { Mid = mid, Name = name,Photo="https://y.gtimg.cn/music/photo_new/T001R500x500M000" + mid + ".jpg?max_age=2592000" });
                singers.Add(name);
            }
            m.SingerText = string.Join( '/', singers);
            if (a["size320"].ToString() != "0")
                m.Quality = MusicQuality.HQ;
            if (a["sizeflac"].ToString() != "0")
                m.Quality = MusicQuality.SQ;
            m.Mvmid = a["vid"].ToString();
            i++;
            musics.Add(m);
        }
        md.Musics = musics;
        return md;
    }
}
