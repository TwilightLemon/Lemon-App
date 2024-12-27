using DataType = LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;

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
}
