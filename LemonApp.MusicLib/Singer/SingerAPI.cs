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

namespace LemonApp.MusicLib.Singer; 

public static class SingerAPI
{
    public static async Task<SingerPageData> GetPageDataAsync(HttpClient hc, string id,TencUserAuth auth)
    {
        var param = "{\"req_0\":{\"module\":\"musichall.singer_info_server\",\"method\":\"GetSingerDetail\",\"param\":{\"singer_mids\":[\"" + id + "\"],\"pic\":1,\"group_singer\":1,\"wiki_singer\":1,\"ex_singer\":1}},\"req_1\":{\"module\":\"musichall.song_list_server\",\"method\":\"GetSingerSongList\",\"param\":{\"singerMid\":\"" + id + "\",\"begin\":0,\"num\":10,\"order\":1}},\"req_2\":{\"module\":\"Concern.ConcernSystemServer\",\"method\":\"cgi_qry_concern_status\",\"param\":{\"vec_userinfo\":[{\"usertype\":1,\"userid\":\"" + id + "\"}],\"opertype\":5,\"encrypt_singerid\":1}},\"req_3\":{\"module\":\"music.musichallAlbum.SelectedAlbumServer\",\"method\":\"SelectedAlbumList\",\"param\":{\"singerMid\":\"" + id + "\"}},\"comm\":{\"g_tk\":" + auth.G_tk + ",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1710}}";
        var data = await hc.SetForUYV17(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", param);
        var json = JsonNode.Parse(await data.AsTextAsync());

        var req0 = json["req_0"]["data"]["singer_list"][0];
        Profile profile = new();
        profile.Mid = id;
        profile.Name = req0["basic_info"]["name"].ToString();
        profile.Photo= req0["pic"]["pic"].ToString();

        bool followed = (json["req_2"]["data"]["map_singer_status"][id].ToString() != "0");

        //TODO...

        return null;
    }
}
