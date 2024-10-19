using static LemonApp.MusicLib.Abstraction.Music.DataTypes;
using System.Web;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.Search;
public static class SearchAPI
{
    public static async Task<List<Music>> SearchMusicAsync(HttpClient hc,TencUserAuth auth, string Content, int osx = 1)
    {
        var url=($"https://u.y.qq.com/cgi-bin/musicu.fcg?g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0&data=" +
            HttpUtility.UrlEncode("{\"req_0\":{\"method\":\"DoSearchForQQMusicDesktop\",\"module\":\"music.search.SearchCgiService\",\"param\":{\"remoteplace\":\"txt.mqq.all\",\"searchid\":\"54355611513934060\",\"search_type\":0,\"query\":\"" + Content + "\",\"page_num\":" + osx + ",\"num_per_page\":50}},\"comm\":{\"ct\":24,\"cv\":0}}"));
        var data=await hc.SetForMusicuFcg(auth.Cookie!)
                            .GetStringAsync(url);
        List<Music> dt = [];
        if (JsonNode.Parse(data) is { } json)
        {
            try
            {
                var dsl = json["req_0"]["data"]["body"]["song"]["list"].AsArray();
                foreach (var dsli in dsl)
                {
                    Music m = new();
                    m.MusicName = dsli["title"].ToString();
                    m.MusicName_Lyric = dsli["desc"].ToString();
                    List<string> Singer = [];
                    List<Profile> lm = [];
                    foreach (var d in dsli["singer"].AsArray())
                    {
                        string mid = d["mid"].ToString(),
                            name = d["name"].ToString();
                        Singer.Add(name);
                        lm.Add(new Profile() { 
                            Name = name,
                            Mid = mid ,
                            Photo= "https://y.gtimg.cn/music/photo_new/T001R500x500M000" + mid + ".jpg"
                        });
                    }
                    m.Singer = lm;
                    m.SingerText = string.Join("/", Singer);
                    m.MusicID = dsli["mid"].ToString();
                    var amid = dsli["album"]["mid"].ToString();
                    if (amid != "")
                        m.Album = new AlbumInfo()
                        {
                            Id = amid,
                            Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000",
                            Name = dsli["album"]["name"].ToString()
                        };
                    var file = dsli["file"];
                    if (file["size_320mp3"].ToString() != "0")
                        m.Quality = MusicQuality.HQ;
                    if (file["size_flac"].ToString() != "0")
                        m.Quality = MusicQuality.SQ;
                    m.Mvmid = dsli["mv"]["vid"].ToString();
                    dt.Add(m);
                }
            }
            catch { }
        }
        return dt;
    }
}
