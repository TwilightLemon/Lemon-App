using LemonApp.MusicLib.Abstraction.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Lyric;

public static class GatitoGetLyric
{
    public static async Task<LocalLyricData> GetTencLyricAsync(HttpClient hc,string id)
    {
        string url = $"https://api.twlmgatito.cn/lyric/get?source=qq&id={id}";
        string data = await hc.GetStringAsync(url);
        return  JsonSerializer.Deserialize<LocalLyricData>(data)!;
    }
}
