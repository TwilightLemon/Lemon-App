using DataType = LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using LemonApp.Common.Funcs;

namespace LemonApp.MusicLib.User;

public static class TencMyDissAPI
{
    public static async Task<List<DataType.AlbumInfo>> GetMyBoughtAlbumList(TencUserAuth auth,HttpClient hc)
    {
        string url = $"https://c.y.qq.com/shop/fcgi-bin/fcg_get_order?from=1&cmd=sales_album&type=1&format=json&inCharset=utf-8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=1&uin={auth.Id}&g_tk={auth.G_tk}&start=0&num=50";
        string data = await hc.SetForCYQ(auth.Cookie!)
                        .GetStringAsync(url);
        var o = JsonNode.Parse(data)["data"]["albumlist"].AsArray();
        List<DataType.AlbumInfo> list = [];
        foreach (var item in o)
        {
            var album = new DataType.AlbumInfo();
            album.Id = item["albummid"].ToString();
            album.Name = item["album_name"].ToString();
            album.Photo = $"https://y.qq.com/music/photo_new/T002R300x300M000{item["albummid"]}.jpg?max_age=2592000";
            list.Add(album);
        }
        return list;
    }

    public static async Task<List<DataType.Playlist>> GetMyFavoritePlaylist(TencUserAuth auth,HttpClient hc)
    {
        string url = $"https://c.y.qq.com/fav/fcgi-bin/fcg_get_profile_order_asset.fcg?g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0&ct=20&cid=205360956&userid={auth.Id}&reqtype=3&sin=0&ein=25";
        var o = JsonNode.Parse(await hc.SetForCYQ(auth.Cookie!).GetStringAsync(url))["data"]["cdlist"].AsArray();
        var list = new List<DataType.Playlist>();
        foreach(var  item in o)
        {
            var pl = new DataType.Playlist();
            pl.Id = item["dissid"].ToString();
            pl.Name=item["dissname"].ToString();
            pl.Subtitle = TextHelper.IntToWn(int.Parse(item["listennum"].ToString()));
            if (item["logo"] is not null)
                pl.Photo = item["logo"].ToString();
            else pl.Photo = "https://y.gtimg.cn/mediastyle/global/img/cover_playlist.png?max_age=31536000";
            list.Add(pl);
        }
        return list;
    }
}
