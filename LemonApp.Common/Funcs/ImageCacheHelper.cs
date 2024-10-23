using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace LemonApp.Common.Funcs;
/*
 图片二级缓存  web->memory
似乎不需要进行本地缓存..?
 */

public class ImageCacheHelper
{
    private static readonly ConditionalWeakTable<string, BitmapImage> _cache = new();
    private static readonly HttpClient _client = new(new SocketsHttpHandler()
    {
        AutomaticDecompression = DecompressionMethods.GZip,
        AllowAutoRedirect = true
    });
    static ImageCacheHelper()
    {
        _client.DefaultRequestHeaders.Accept.TryParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        _client.DefaultRequestHeaders.AcceptLanguage.TryParseAdd("zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
        _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
    }

    public static async Task<BitmapImage?> FetchData(string url)
    {
        var img = GetFromMem(url);
        if (img is { })
        {
            return img;
        }
        img = await GetFromWeb(url);
        if (img != null)
        {
            _cache.Add(url, img);
            return img;
        }
        return null;
    }
    private static BitmapImage? GetFromMem(string url)
    {
        if (_cache.TryGetValue(url, out var img))
        {
            return img;
        }
        return null;
    }
    private static async Task<BitmapImage?> GetFromWeb(string url)
    {
        try
        {
            var stream = await _client.GetStreamAsync(url);
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = stream;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }
        catch {
            return null;
        }
    }
}
