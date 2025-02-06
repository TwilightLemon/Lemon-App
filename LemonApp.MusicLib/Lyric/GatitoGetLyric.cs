using LemonApp.MusicLib.Abstraction.Entities;
using System.Text.Json;

namespace LemonApp.MusicLib.Lyric;

public static class GatitoGetLyric
{
    public static async Task<LocalLyricData> GetTencLyricAsync(HttpClient hc,Music info)
    {
        string url = $"https://api.twlmgatito.cn/lyric/get?source={info.Source}&id={info.MusicID}";
        string data = await hc.GetStringAsync(url);
        return  JsonSerializer.Deserialize<LocalLyricData>(data)!;
    }
}
