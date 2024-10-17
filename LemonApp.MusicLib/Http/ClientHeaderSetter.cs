using LemonApp.Common.Funcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Http;

internal static class ClientHeaderSetter
{
    /// <summary>
    /// c.y.qq.com
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForCYQ(this HttpClient hc,string cookie)
    {
        hc.DefaultRequestHeaders.Add("CacheControl", "max-age=0");
        hc.DefaultRequestHeaders.Add("Upgrade", "1");
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
        hc.DefaultRequestHeaders.Add("Referer", "https://y.qq.com/");
        hc.DefaultRequestHeaders.Host = "c.y.qq.com";
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.8");
        hc.DefaultRequestHeaders.Add("Cookie", cookie);
        hc.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        hc.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        hc.DefaultRequestHeaders.Add("sec-fetch-site", "same-site");
        return hc;
    }
    /// <summary>
    /// u.y.qq.com for Musicu.fcg
    /// </summary>
    /// <param name="_hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForMusicuFcg(this HttpClient _hc,string cookie)
    {
        _hc.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        _hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
        _hc.DefaultRequestHeaders.Add("Referer", "https://y.qq.com/");
        _hc.DefaultRequestHeaders.Host = "u.y.qq.com";
        _hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0");
        _hc.DefaultRequestHeaders.Add("Cookie", cookie);
        return _hc;
    }
}
