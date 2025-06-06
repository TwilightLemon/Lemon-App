﻿using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace LemonApp.MusicLib.Singer; 

public static class SingerAPI
{
    public static async Task<List<AlbumInfo>> GetAlbumOfSingerAsync(HttpClient hc, string id, TencUserAuth auth,int page=0,int capcity=20)
    {
        int begin = page * capcity;
        var data = await hc.SetForUYV17(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", "{\"comm\":{\"g_tk\":\"" + auth.G_tk + "\",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1710},\"singerAlbum\":{\"method\":\"get_singer_album\",\"param\":{\"singermid\":\"" + id + "\",\"order\":\"time\",\"begin\":"+begin+",\"num\":"+capcity+",\"exstatus\":1},\"module\":\"music.web_singer_info_svr\"}}");
        var albumNode = JsonNode.Parse(await data.AsTextAsync());
        List<AlbumInfo> albums = [];
        var datac = albumNode["singerAlbum"]["data"]["list"].AsArray();
        foreach (var c in datac)
        {
            AlbumInfo m = new();
            m.Id = c["album_mid"].ToString();
            m.Name = c["album_name"].ToString();
            m.Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{m.Id}.jpg?max_age=2592000";
            albums.Add(m);
        }
        return albums;
    }
    public static async Task<List<Music>> GetSongsOfSingerAsync(HttpClient hc, string id, TencUserAuth auth, int page = 0, int capcity = 20)
    {
        int begin = page * capcity;
        var jsonData = await hc.SetForUYV17(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", "{\"comm\":{\"g_tk\":\"" + auth.G_tk + "\",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1710},\"req_1\":{\"method\":\"GetSingerSongList\",\"param\":{\"singermid\":\"" + id + "\",\"order\":1,\"begin\":" + begin + ",\"num\":" + capcity + "},\"module\":\"musichall.song_list_server\"}}");
        var json = JsonNode.Parse(await jsonData.AsTextAsync());
        var req1 = json["req_1"]["data"]["songList"].AsArray();
        List<Music> list = [];
        try
        {
            foreach (var c in req1)
            {
                var data = c["songInfo"];
                Music m = new Music();
                m.MusicName = data["name"].ToString();
                m.MusicName_Lyric = data["subtitle"].ToString();
                m.MusicID = data["mid"].ToString();
                m.Littleid = data["id"].ToString();
                List<Profile> lm = [];
                foreach (var s in data["singer"].AsArray())
                {
                    lm.Add(new() { Name = s["name"].ToString(), Mid = s["mid"].ToString() });
                }
                m.Singer = lm;
                m.SingerText = string.Join("/", lm.Select(x => x.Name));
                string amid = data["album"]["mid"].ToString();
                if (amid != "")
                    m.Album = new AlbumInfo() { Name = data["album"]["name"].ToString(), Id = amid, Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000" };
                var file = data["file"];
                if (file["size_320mp3"].ToString() != "0")
                    m.Quality = MusicQuality.HQ;
                if (file["size_flac"].ToString() != "0")
                    m.Quality = MusicQuality.SQ;
                m.Mvmid = data["mv"]["vid"].ToString();
                list.Add(m);
            }
        }
        catch { }
        return list;
    }
    public static async Task<SingerPageData> GetPageDataAsync(HttpClient hc, string id,TencUserAuth auth)
    {

        var param = "{\"req_0\":{\"module\":\"musichall.singer_info_server\",\"method\":\"GetSingerDetail\",\"param\":{\"singer_mids\":[\"" + id + "\"],\"pic\":1,\"group_singer\":1,\"wiki_singer\":1,\"ex_singer\":1}},\"req_1\":{\"module\":\"musichall.song_list_server\",\"method\":\"GetSingerSongList\",\"param\":{\"singerMid\":\"" + id + "\",\"begin\":0,\"num\":10,\"order\":1}},\"req_2\":{\"module\":\"Concern.ConcernSystemServer\",\"method\":\"cgi_qry_concern_status\",\"param\":{\"vec_userinfo\":[{\"usertype\":1,\"userid\":\"" + id + "\"}],\"opertype\":5,\"encrypt_singerid\":1}},\"req_3\":{\"module\":\"music.musichallAlbum.SelectedAlbumServer\",\"method\":\"SelectedAlbumList\",\"param\":{\"singerMid\":\"" + id + "\"}},\"comm\":{\"g_tk\":" + auth.G_tk + ",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1710}}";
        var data1 = await hc.SetForUYV17(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg", param);
        var json = JsonNode.Parse(await data1.AsTextAsync());

        var req0 = json["req_0"]["data"]["singer_list"][0];
        Profile profile = new();
        profile.Mid = id;
        profile.Name = req0["basic_info"]["name"].ToString();
        profile.Photo= req0["pic"]["pic"].ToString();
        var bigBackground=req0["pic"]["big_black"]?.ToString();

        var req1 = json["req_1"]["data"]["songList"].AsArray();
        List<Music> hotSongs = [];
        try
        {
            foreach (var c in req1)
            {
                var data = c["songInfo"];
                Music m = new Music();
                m.MusicName = data["name"].ToString();
                m.MusicName_Lyric = data["subtitle"].ToString();
                m.MusicID = data["mid"].ToString();
                m.Littleid = data["id"].ToString();
                List<Profile> lm = [];
                foreach (var s in data["singer"].AsArray())
                {
                    lm.Add(new () { Name = s["name"].ToString(), Mid = s["mid"].ToString() });
                }
                m.Singer = lm;
                m.SingerText = string.Join("/",lm.Select(x => x.Name));
                string amid = data["album"]["mid"].ToString();
                if (amid != "")
                    m.Album = new AlbumInfo() { Name = data["album"]["name"].ToString(), Id = amid, Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000" };
                var file = data["file"];
                if (file["size_320mp3"].ToString() != "0")
                    m.Quality = MusicQuality.HQ;
                if (file["size_flac"].ToString() != "0")
                    m.Quality = MusicQuality.SQ;
                m.Mvmid = data["mv"]["vid"].ToString();
                hotSongs.Add(m);
            }
        }
        catch { }

        bool followed = false;
        try { followed = (json["req_2"]["data"]["map_singer_status"][id].ToString() != "0"); } catch { }

        var req3 = json["req_3"]["data"]["albumList"].AsArray();
        List<AlbumInfo> recentAlbum = [];
        foreach (var c in req3)
        {
            AlbumInfo m = new();
            m.Id = c["albumMid"].ToString();
            m.PublishDate = c["publishDate"].ToString();
            m.Name = c["albumName"].ToString();
            m.Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{m.Id}.jpg?max_age=2592000";
            recentAlbum.Add(m);
        }

        List<AlbumInfo> albums = await GetAlbumOfSingerAsync(hc,id,auth,0,8);

        var data2 = await hc.SetForCYQ(auth.Cookie).GetStringAsync("https://c.y.qq.com/v8/fcg-bin/fcg_v8_simsinger.fcg?utf8=1&singer_mid=" + id + "&start=0&num=5&g_tk=" +auth.G_tk + "&loginUin=" + auth.Id + "&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0");
        var ss = JsonNode.Parse(data2)["singers"]["items"];
        List<Profile> similarSingers = [];
        if (ss != null)
        {
            foreach (var c in ss.AsArray())
            {
                Profile m = new();
                m.Mid = c["mid"].ToString();
                m.Name = c["name"].ToString();
                m.Photo = c["pic"].ToString();
                similarSingers.Add(m);
            }
        }

        var data3 =await hc.GetStringAsync("https://c.y.qq.com/rsc/fcgi-bin/fcg_order_singer_getnum.fcg?g_tk=" + auth.G_tk + "&loginUin=" + auth.Id + "&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&singermid=" + id + "&utf8=1&rnd=1565074512297");
        long fansCount =long.Parse( JsonNode.Parse(data3)["num"].ToString());

        string data4 = await hc.GetStringAsync("https://c.y.qq.com/splcloud/fcgi-bin/fcg_get_singer_desc.fcg?singermid=" + id + "&utf8=1&outCharset=utf-8&format=xml&r=1565243621590");

        XElement x = XDocument.Parse(data4).Element("result").Element("data").Element("info");
        var desc = x.Element("desc").Value.Replace("<![CDATA[", "").Replace("]]>", "");

        var a = from b in x.Element("basic").Descendants("item")
                select new { key = b.Element("key").Value.Replace("<![CDATA[", "").Replace("]]>", ""), value = b.Element("value").Value.Replace("<![CDATA[", "").Replace("]]>", "") };
        var basic = new Dictionary<string, string>();
        foreach (var c in a)
            basic.Add(c.key, c.value);

        var other = new Dictionary<string, string>();
        try
        {
            var d = from b in x.Element("other").Descendants("item")
                    select new { key = b.Element("key").Value.Replace("<![CDATA[", "").Replace("]]>", ""), value = b.Element("value").Value.Replace("<![CDATA[", "").Replace("]]>", "") };
            foreach (var c in d)
                other.Add(c.key, c.value);
        }
        catch { }
        var singerDesc=new SingerDesc(desc,basic,other);

        return new(profile, bigBackground, recentAlbum, albums, hotSongs, followed, similarSingers, fansCount,singerDesc);
    }
}
