using System.Text.Json.Nodes;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.RankList;
#pragma warning disable CS8602 // 解引用可能出现空引用。
public static class RankListAPI
{
    public static async Task<List<RankListInfo>> GetRankListIndexAsync(HttpClient hc)
    {
        string url = "https://u.y.qq.com/cgi-bin/musicu.fcg?data={%22comm%22:{%22g_tk%22:5381,%22uin%22:%22%22,%22format%22:%22json%22,%22inCharset%22:%22utf-8%22,%22outCharset%22:%22utf-8%22,%22notice%22:0,%22platform%22:%22h5%22,%22needNewCode%22:1,%22ct%22:23,%22cv%22:0},%22topList%22:{%22module%22:%22musicToplist.ToplistInfoServer%22,%22method%22:%22GetAll%22,%22param%22:{}}}";
        string data = await hc.GetStringAsync(url);
        var json = JsonNode.Parse(data);
        var list = new List<RankListInfo>();
        var d0l = json["topList"]["data"]["group"].AsArray();
        foreach (var c in d0l)
        {
            var toplist = c["toplist"].AsArray();
            foreach (var d in toplist)
            {
                List<string> content = [];
                foreach (var a in d["song"].AsArray())
                    content.Add(a["title"] + " - " + a["singerName"]);
                list.Add(new RankListInfo()
                {
                    Name = d["title"].ToString(),
                    CoverUrl = d["frontPicUrl"].ToString().Replace("http://", "https://"),
                    Id = d["topId"].ToString(),
                    Description = "[" + d["titleShare"] + "] " + d["intro"].ToString().Replace("<br>", ""),
                    Content = content
                });
            }
        }
        return list;
    }
    public static async Task<List<Music>> GetRankListData(string rankId,HttpClient hc)
    {
        string url = $"https://c.y.qq.com/v8/fcg-bin/fcg_v8_toplist_cp.fcg?tpl=3&page=detail&topid={rankId}&type=top&song_begin=0&song_num=100&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";
        string data = await hc.GetStringAsync(url);
        var json = JsonNode.Parse(data);
        var list = new List<Music>();
        var s = json["songlist"].AsArray();
        foreach (var si in s)
        {
            var sid = si["data"];
            Music m = new();
            m.MusicName = sid["songname"].ToString();
            m.MusicName_Lyric = sid["albumdesc"].ToString();
            List<string> singerText = [];
            List<Profile> singers = [];
            foreach(var singer in sid["singer"].AsArray())
            {
                string name= singer["name"].ToString();
                string mid = singer["mid"].ToString();
                singers.Add(new Profile()
                {
                    Name = name,
                    Mid = mid
                });
                singerText.Add(name);
            }
            m.Singer= singers;
            m.SingerText=string.Join("/", singerText);
            m.MusicID = sid["songmid"].ToString();
            var amid = sid["albummid"]?.ToString();
            if (amid != null)
            {
                m.Album = new AlbumInfo()
                {
                    Id = amid,
                    Photo= $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000",
                    Name = sid["albumname"].ToString(),
                };
            }
            if (sid["sizeflac"].ToString() != "0")
                m.Quality = MusicQuality.SQ;
            else if (sid["size320"].ToString() != "0")
                m.Quality = MusicQuality.HQ;
            m.Mvmid = sid["vid"].ToString();
            list.Add(m);
        }
        return list;
    }
}