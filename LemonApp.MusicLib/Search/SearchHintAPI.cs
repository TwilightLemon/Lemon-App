using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using System.Web;

namespace LemonApp.MusicLib.Search;

public static class SearchHintAPI
{
    public static async Task<SearchHint> GetSearchHintAsync(string keyword,HttpClient hc)
    {
        string url = $"https://c.y.qq.com/splcloud/fcgi-bin/smartbox_new.fcg?key={HttpUtility.UrlEncode(keyword)}&utf8=1&is_xml=0&loginUin=0";
        string data=await hc.SetForCYQ().GetStringAsync(url);
        SearchHint tips = new();
        var json = JsonNode.Parse(data)["data"];
        var song = json["song"]["itemlist"].AsArray();
        foreach (var o in song)
        {
            tips.Hints.Add(new(o["name"].ToString(), o["mid"].ToString(), o["singer"].ToString(),SearchHint.HintType.Music));
        }
        var album = json["album"]["itemlist"].AsArray();
        foreach (var o in album)
        {
            tips.Hints.Add(new(o["name"].ToString(), o["mid"].ToString(), o["singer"].ToString(), SearchHint.HintType.Album));
        }
        var singer = json["singer"]["itemlist"].AsArray();
        foreach (var o in singer)
        {
            tips.Hints.Add(new(o["name"].ToString(), o["mid"].ToString(),string.Empty, SearchHint.HintType.Singer));
        }
        return tips;
    }
}
