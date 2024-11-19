using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.MusicLib.Media;

public static class CoverGetter
{
    public static async Task<string> GetCoverImgUrl(Func<HttpClient> hc,TencUserAuth auth,Music m)
    {
        if (m.Album != null && m.Album.Id != null)
            return $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{m.Album.Id}.jpg?max_age=2592000";
        else
        {
            string json = await hc().SetCookie(auth.Cookie).GetStringAsync("https://y.qq.com/n/ryqq/songDetail/" + m.MusicID);
            var id = TextHelper.FindTextByAB(json, "photo_new\\u002F", ".jpg", 0);
            return $"https://y.qq.com/music/photo_new/{id}.jpg";
        }
    }
}
