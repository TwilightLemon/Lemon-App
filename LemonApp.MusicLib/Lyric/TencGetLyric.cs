using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using Lyricify.Lyrics.Decrypter.Qrc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml;

namespace LemonApp.MusicLib.Lyric;
public class QqLyricsResponse
{
    public string? Lyrics { get; set; }
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}
public static class TencGetLyric
{
    private static readonly Dictionary<string, string> VerbatimXmlMappingDict = new()
        {
            { "content", "orig" }, // 原文
            { "contentts", "ts" }, // 译文
            { "contentroma", "roma" }, // 罗马音
            { "Lyric_1", "lyric" }, // 解压后的内容
        };
    public static async Task<QqLyricsResponse?> GetLyricsAsync(HttpClient hc, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var param = new Dictionary<string, string>
                {
                    { "version", "15" },
                    { "miniversion", "82" },
                    { "lrctype", "4" },
                    { "musicid", id },
                };
        var content = new FormUrlEncodedContent(param);
        var response = await hc.SetForPCQ().PostAsync("https://c.y.qq.com/qqmusic/fcgi-bin/lyric_download.fcg", content);
        var resp = await response.Content.ReadAsStringAsync();

        resp = resp.Replace("<!--", "").Replace("-->", "");

        var dict = new Dictionary<string, XmlNode>();

        XmlUtils.RecursionFindElement(XmlUtils.Create(resp), VerbatimXmlMappingDict, dict);

        var result = new QqLyricsResponse
        {
            Lyrics = "",
            Trans = "",
            Romaji = ""
        };

        foreach (var pair in dict)
        {
            var text = pair.Value.InnerText;

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            string decompressText;
            try
            {
                decompressText = Decrypter.DecryptLyrics(text) ?? "";
            }
            catch
            {
                continue;
            }

            var s = "";
            if (decompressText.Contains("<?xml"))
            {
                var doc = XmlUtils.Create(decompressText);

                var subDict = new Dictionary<string, XmlNode>();

                XmlUtils.RecursionFindElement(doc, VerbatimXmlMappingDict, subDict);

                if (subDict.TryGetValue("lyric", out var d))
                {
                    s = d.Attributes?["LyricContent"]?.InnerText;
                }
            }
            else
            {
                s = decompressText;
            }

            if (!string.IsNullOrWhiteSpace(s))
            {
                switch (pair.Key)
                {
                    case "orig":
                        result.Lyrics = s;
                        break;
                    case "ts":
                        result.Trans = s;
                        break;
                    case "roma":
                        result.Romaji = s;
                        break;
                }
            }
        }

        if (result.Lyrics == "" && result.Trans == "")
        {
            return null;
        }
        return result;
    }
}
