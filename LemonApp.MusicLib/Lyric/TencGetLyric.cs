using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.Lyric;

public static  class TencGetLyric
{
    public static async Task<LyricData?> GetLyricDataAsync(
        HttpClient hc,TencUserAuth auth,string mid)
    {
        string url = $"https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={mid}&g_tk=5381&loginUin=0&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq.json&needNewCode=0";
        string data = await hc.SetForCYQ(auth.Cookie!, "https://y.qq.com/portal/player.html")
            .GetStringAsync(url);
        if(JsonNode.Parse(data) is { } json)
        {
            LyricData ld = new() { Id = mid };
            string lyric = WebUtility.HtmlDecode(Encoding.UTF8.GetString(Convert.FromBase64String(json["lyric"]!.ToString())));
            ld.Lyric = lyric;
            if (json["trans"]?.ToString() is { } transEnc)
            {
                string trans = WebUtility.HtmlDecode(Encoding.UTF8.GetString(Convert.FromBase64String(transEnc)));
                ld.Trans= trans;
            }
            return ld;
        }
        return null;
    }
}
