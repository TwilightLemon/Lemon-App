using LemonApp.Common.Funcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Http;

/*
 TODO: re-design HttpClientFactory for each header, and use DI to inject them.
 */

internal static class ClientHeaderSetter
{
    /// <summary>
    /// c.y.qq.com
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForCYQ(this HttpClient hc,string cookie)
    {
        hc.DefaultRequestHeaders.Clear();
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
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForMusicuFcg(this HttpClient hc,string cookie)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
        hc.DefaultRequestHeaders.Add("Referer", "https://y.qq.com/");
        hc.DefaultRequestHeaders.Host = "u.y.qq.com";
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0");
        hc.DefaultRequestHeaders.Add("Cookie", cookie);
        return hc;
    }

    /// <summary>
    /// set for music.163.com
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    public static HttpClient SetForNetease(this HttpClient hc,string cookie)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.9");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "application/x-www-form-urlencoded; charset=UTF-8");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie);
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://music.163.com/");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("UserAgent", "Mozilla/5.0 (Linux; Android 8.0.0; SM-G955U Build/R16NW) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Host", "music.163.com");
        return hc;
    }
    public static HttpClient SetForEzlang(this HttpClient hc) {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.Host = "www.ezlang.net";
        hc.DefaultRequestHeaders.Add("Origin", "https://www.ezlang.net");
        hc.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.120 Safari/537.36 Core/1.77.119.400 QQBrowser/10.9.4817.400");
        hc.DefaultRequestHeaders.Add("Referer", "https://www.ezlang.net/zh-Hans/tool/romaji");
        hc.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
        hc.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        hc.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        hc.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return hc;
    }
    public static HttpClient SetCookie(this HttpClient hc, string cookie) {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
        if(!string.IsNullOrEmpty(cookie))
        hc.DefaultRequestHeaders.Add("Cookie", cookie);
        return hc;
    }
}
