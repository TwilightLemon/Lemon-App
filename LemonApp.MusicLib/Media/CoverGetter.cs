using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using LemonApp.MusicLib.Abstraction.Entities;
using System.Text.RegularExpressions;
using System.Text;

namespace LemonApp.MusicLib.Media;

public static class CoverGetter
{
    public static async Task<string> GetCoverImgUrl(Func<HttpClient> hc,TencUserAuth auth,Music m)
    {
        if (string.IsNullOrEmpty(m.MusicID)) return "";
        if (m.Source == Platform.qq)
        {
            if (m.Album != null && m.Album.Id != null)
                return $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{m.Album.Id}.jpg?max_age=2592000";
            else
            {
                string json = await hc().SetCookie(auth.Cookie).GetStringAsync("https://y.qq.com/n/ryqq/songDetail/" + m.MusicID);
                var id = TextHelper.FindTextByAB(json, "photo_new\\u002F", ".jpg", 0);
                if (id != null)
                    return $"https://y.qq.com/music/photo_new/{id}.jpg";
                else return "https://y.gtimg.cn/mediastyle/global/img/album_300.png?max_age=31536000";
            }
        }else if (m.Source == Platform.wyy)
        {
            return await GetCoverNetease(hc(), m.MusicID);
        }
        return "https://y.gtimg.cn/mediastyle/global/img/album_300.png?max_age=31536000";
    }

    public static async Task<string> GetCoverNetease(HttpClient hc, string id)
    {
        try
        {
            var bytes = await hc.SetForNetease(null).GetByteArrayAsync($"https://music.163.com/song?id={id}");
            var data = Encoding.UTF8.GetString(bytes);
            Regex regex = new Regex(@"<meta\s+property=""og:image""\s+content=""([^""]+\.jpg)""\s*/>");
            var match = regex.Match(data);
            if (match.Success)
            {
                return match.Groups[1].Value + "?param=500y500";//分辨率参数
            }
        }
        catch { }
        return "https://y.gtimg.cn/mediastyle/global/img/album_300.png?max_age=31536000";
    }
}
