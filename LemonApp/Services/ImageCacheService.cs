using LemonApp.Common.Funcs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.Services;
/*
 图片三级缓存  Memory->File->Internet
 */

public class ImageCacheService
{
    private readonly MemoryCache _cache = new MemoryCache("ImageCache");
    private readonly CacheItemPolicy _cachePolicy = new CacheItemPolicy
    {
        SlidingExpiration = TimeSpan.FromMinutes(10),
#if DEBUG
        RemovedCallback = new CacheEntryRemovedCallback((e) =>
        {
            Debug.WriteLine($"Cache item '{e.CacheItem.Key}' was removed because {e.RemovedReason}");
        })
#endif
    };
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _client;
    public ImageCacheService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _client = _httpClientFactory.CreateClient(App.PublicClientFlag);
        _client.DefaultRequestHeaders.Accept.TryParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        _client.DefaultRequestHeaders.AcceptLanguage.TryParseAdd("zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
        _client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        _client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
    }
    static readonly ImageCacheService Instance = App.Services.GetRequiredService<ImageCacheService>();
    public static Task<BitmapImage?> FetchData(string? url)
        => Instance.GetImage(url);
    public async Task<BitmapImage?> GetImage(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        //memory cache
        if (_cache.Get(url) is BitmapImage img) return img;

        //file cache
        var filePath = GetLocalFilePath(url);
        if (File.Exists(filePath))
        {
            var bitmap = LoadFromFile(filePath);
            _cache.Set(url, bitmap, _cachePolicy);
            return bitmap;
        }

        //network fetch
        try
        {
            if (await _client.GetByteArrayAsync(url) is { Length: > 0 } bytes)
            {
                _ = File.WriteAllBytesAsync(filePath, bytes);
                var bitmap = LoadFromBytes(bytes);
                _cache.Set(url, bitmap, _cachePolicy);
                return bitmap;
            }
        }
        catch { }
        return null;
    }
    public static string GetLocalFilePath(string url)
    {
        var fileName = TextHelper.MD5Hash(url) + ".jpg";
        return Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Image), fileName);
    }
    private static BitmapImage LoadFromFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        return LoadFromStream(fs);
    }
    private static BitmapImage LoadFromBytes(byte[] bytes)
    {
        using var ms=new MemoryStream(bytes);
        return LoadFromStream(ms);
    }
    private static BitmapImage LoadFromStream(Stream stream)
    {
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = stream;
        img.EndInit();
        img.Freeze(); //cross-thread safety
        return img;
    }
}
