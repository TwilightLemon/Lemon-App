using LemonApp.Common.Funcs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.Services;
/*
 图片三级缓存  Memory->File->Internet
 */

public class ImageCacheService
{
    private readonly MemoryCache _cache = new MemoryCache("ImageCache");
    private CacheItemPolicy _cachePolicy = new CacheItemPolicy
    {
        SlidingExpiration = TimeSpan.FromMinutes(10),
        RemovedCallback = new CacheEntryRemovedCallback((e) =>
        {
            Debug.WriteLine($"Cache item '{e.CacheItem.Key}' was removed because {e.RemovedReason}");
        })
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

        var img = GetFromMem(url);
        if (img is { })
        {
            return img;
        }
        img = await GetFromFile(url);
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
    private BitmapImage? GetFromMem(string url)
    {
        if (_cache.Get(url) is BitmapImage img)
        {
            Debug.WriteLine("img loaded from cache.");
            return img;
        }
        return null;
    }
    private async Task<BitmapImage?> GetFromWeb(string url)
    {
        var res = await _client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync();

        string file = GetLocalFilePath(url);
        using var fs = File.Create(file);
        await stream.CopyToAsync(fs);
        await fs.DisposeAsync();
        Debug.WriteLine("img loaded from web.");
        return await GetFromFile(url);
    }
    private static Task<BitmapImage?> GetFromFile(string url)
    {
        return Task.Run(() =>
        {
            string file = GetLocalFilePath(url);
            if (!Path.Exists(file)) return null;

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(file);
            img.EndInit();
            img.Freeze();
            return img;
        });
    }
}
