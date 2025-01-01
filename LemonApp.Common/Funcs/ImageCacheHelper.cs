using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace LemonApp.Common.Funcs;
/*
 图片三级缓存  Memory->File->Internet
 */

public class ImageCacheHelper
{
    private static readonly MemoryCache _cache = new MemoryCache("ImageCache");
    private static CacheItemPolicy _cachePolicy = new CacheItemPolicy
    {
        SlidingExpiration = TimeSpan.FromMinutes(10),
        RemovedCallback=new CacheEntryRemovedCallback((e) => {
            Debug.WriteLine($"Cache item '{e.CacheItem.Key}' was removed because {e.RemovedReason}");
        })
    };
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
        img = GetFromFile(url);
        if (img != null)
        {
            _cache.Add(url, img, _cachePolicy);
            return img;
        }
        img = await GetFromWeb(url);
        if (img != null)
        {
            _cache.Add(url, img, _cachePolicy);
            return img;
        }
        return null;
    }
    private static string GetLocalFilePath(string url)
    {
        var fileName = TextHelper.MD5Hash(url) + ".jpg";
        return Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Image), fileName);
    }
    private static BitmapImage? GetFromMem(string url)
    {
        if (_cache.Get(url) is BitmapImage img)
        {
            Debug.WriteLine("img loaded from cache.");
            return img;
        }
        return null;
    }
    private static async Task<BitmapImage?> GetFromWeb(string url)
    {
        try
        {
            string file = GetLocalFilePath(url);
            var fs= File.Create(file);
            var stream = await _client.GetStreamAsync(url);
            await stream.CopyToAsync(fs);
            fs.Dispose();
            stream.Dispose();
            Debug.WriteLine("img loaded from web.");
            return GetFromFile(url);
        }
        catch {
            Debug.WriteLine("failed to load img from web.");
            return null;
        }
    }
    private static BitmapImage? GetFromFile(string url)
    {
        try
        {
            string file = GetLocalFilePath(url);
            if (!Path.Exists(file)) return null;
            using var fs = File.OpenRead(file);
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = fs;
            img.EndInit();
            if (img.CanFreeze)
                img.Freeze();
            return img;
        }
        catch
        {
            Debug.WriteLine("failed to load img from file.");
            return null;
        }
    }
}
