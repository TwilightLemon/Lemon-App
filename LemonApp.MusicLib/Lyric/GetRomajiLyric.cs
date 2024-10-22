using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Web;
using LemonApp.MusicLib.Http;
using System.Xml;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.Lyric;
#pragma warning disable CS8602
public static class GetRomajiLyric
{
    public static async Task<List<string>> Trans(HttpClient hc, string text)
    {
        string url = "https://www.ezlang.net/ajax/tool_data.php";
        string postData = $"txt={HttpUtility.UrlDecode(text.ToString())}&sn=romaji";
        var post =await hc.SetForEzlang().PostAsync(url,new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded"));
        string data = await post.Content.ReadAsStringAsync();

        List<string> vs = [];
        if (JsonArray.Parse(data) is { } arr)
        {
            string doc = "<List>" + arr[1] + "</List>";
            XmlDocument xd = new();
            xd.LoadXml(doc);
            var root = xd.SelectSingleNode("List");
            foreach (XmlElement div in root)
            {
                string line = "";
                var spans = div.SelectNodes("span");
                foreach (XmlElement span in spans)
                {
                    if (span.InnerXml.Contains("ruby"))
                    {
                        var d = span.SelectSingleNode("ruby").SelectSingleNode("rt").InnerText;
                        line += " " + d;
                    }
                }
                vs.Add(line);
            }
        }
        return vs;
    }
}
