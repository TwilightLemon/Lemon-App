using LemonApp.Common.Funcs;
using System.Net;

namespace LemonApp.MusicLib.Http;
public static class ClientHeaderSetter
{
    /// <summary>
    /// c.y.qq.com
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForCYQ(this HttpClient hc,string? cookie=null,string? referer=null)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.Add("CacheControl", "max-age=0");
        hc.DefaultRequestHeaders.Add("Upgrade", "1");
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
        hc.DefaultRequestHeaders.Add("Referer", referer??"https://y.qq.com/");
        hc.DefaultRequestHeaders.Host = "c.y.qq.com";
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.8");
        if (!string.IsNullOrEmpty(cookie)) 
            hc.DefaultRequestHeaders.Add("Cookie", cookie);
        hc.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        hc.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        hc.DefaultRequestHeaders.Add("sec-fetch-site", "same-site");
        return hc;
    }

    public static HttpClient SetForPCQ(this HttpClient hc)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
        hc.DefaultRequestHeaders.Add("Referer", "https://c.y.qq.com/");
        hc.DefaultRequestHeaders.Add("Cookie", "os=pc;osver=Microsoft-Windows-10-Professional-build-16299.125-64bit;appver=2.0.3.131777;channel=netease;__remember_me=true");
        return hc;
    }

    public static HttpClient SetForUYV17(this HttpClient hc,string cookie)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "application/x-www-form-urlencoded");
        hc.DefaultRequestHeaders.Host = "u.y.qq.com";
        hc.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        hc.DefaultRequestHeaders.Add("Origin", "http://y.qq.com");
        hc.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.47.134 Safari/537.36 QBCore/3.53.47.400 QQBrowser/9.0.2524.400 pcqqmusic/17.10.5105.0801 SkinId/10001|1ecc94|145|1|||1fd4af");
        hc.DefaultRequestHeaders.Add("Referer", "http://y.qq.com/wk_v17/");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.8,en-US;q=0.6,en;q=0.5;q=0.4");
        if(!string.IsNullOrEmpty(cookie))
            hc.DefaultRequestHeaders.Add("Cookie", cookie);
        return hc;
    }

    public static Task<HttpResponseMessage> PostTextAsync(this HttpClient hc, string url, string content)
        => hc.PostAsync(url, new StringContent(content, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));
    public static Task<string> AsTextAsync(this HttpResponseMessage res) => res.Content.ReadAsStringAsync();
    /// <summary>
    /// u.y.qq.com for Musicu.fcg
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    public static HttpClient SetForMusicuFcg(this HttpClient hc,string cookie)
    {
        try
        {
            hc.DefaultRequestHeaders.Clear();
            hc.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
            hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            hc.DefaultRequestHeaders.Add("Referer", "https://y.qq.com/");
            hc.DefaultRequestHeaders.Host = "u.y.qq.com";
            hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0");
            if (!string.IsNullOrEmpty(cookie))
                hc.DefaultRequestHeaders.Add("Cookie", cookie);
        }
        catch { }
        return hc;
    }

    /// <summary>
    /// set for music.163.com
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    public static HttpClient SetForNetease(this HttpClient hc,string? cookie)
    {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("AcceptLanguage", "zh-CN,zh;q=0.9");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("ContentType", "application/x-www-form-urlencoded; charset=UTF-8");
        if (!string.IsNullOrEmpty(cookie))
            hc.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookie);
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://music.163.com/");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("UserAgent", "Mozilla/5.0 (Linux; Android 8.0.0; SM-G955U Build/R16NW) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/");
        hc.DefaultRequestHeaders.TryAddWithoutValidation("Host", "music.163.com");
        return hc;
    }
    /// <summary>
    /// pure cookie and useragent
    /// </summary>
    /// <param name="hc"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    public static HttpClient SetCookie(this HttpClient hc, string cookie) {
        hc.DefaultRequestHeaders.Clear();
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
        if(!string.IsNullOrEmpty(cookie))
            hc.DefaultRequestHeaders.Add("Cookie", cookie);
        return hc;
    }
}
