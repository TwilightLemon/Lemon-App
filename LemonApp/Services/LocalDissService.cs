using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Search;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Services;
public class LocalDissService
{
    private readonly SettingsMgr<LocalPlaylist> settings;
    private readonly HttpClient hc;

    public ObservableCollection<LocalDirMeta> LocalDirs;
    public LocalDissService(AppSettingService appSettingsService,IHttpClientFactory httpClientFactory)
    {
        settings = appSettingsService.GetConfigMgr<LocalPlaylist>();
        hc = httpClientFactory.CreateClient(App.PublicClientFlag);
        LocalDirs = [.. settings.Data.LocalDirs];
        appSettingsService.OnExiting += AppSettingsService_OnExiting;
    }

    /// <summary>
    /// Sync while exiting.
    /// </summary>
    private void AppSettingsService_OnExiting()
    {
        settings.Data.LocalDirs = [.. LocalDirs];
    }

    public void AddDir(string path)
    {
        if (!Directory.Exists(path)) return;
        if (LocalDirs.Any(d => d.Path == path)) return;

        var info = new DirectoryInfo(path);
        var meta = new LocalDirMeta(path, info.Name);
        LocalDirs.Add(meta);
    }
    //TODO: 从文件元数据中获取音乐信息
    public async Task<Music?> GetRelatedMusic(Music local)
    {
        if(settings.Data.IdMap.TryGetValue(local.MusicID, out var music))
            return music;

        //按照文件名搜索
        var user = App.Services.GetRequiredService<UserProfileService>().GetAuth();
        if (user.IsValid)
        {
            var data = await SearchAPI.SearchMusicAsync(hc, user, local.MusicName);
            if(data?.FirstOrDefault() is { } match)
            {
                settings.Data.IdMap[local.MusicID] = match;
                return match;
            }
        }
        //使用hint api 无需user auth，但是不能模糊匹配，且信息不全
        var hint = await SearchHintAPI.GetSearchHintAsync(local.MusicName, hc);
        if (hint != null)
        {
            var data = hint.Hints.FirstOrDefault(i => i.Type == SearchHint.HintType.Music);
            if (data != null)
            {
                return new()
                {
                    MusicID = data.Id,
                    MusicName = data.Content
                };
            }
        }
        return null;
    }

    public static List<Music> GetMusics(string path)
    {
        if (!Directory.Exists(path)) return [];

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return [.. files.Select(static f => new Music
        {
            LocalPath = f,
            Source=Platform.none,
            MusicID=TextHelper.MD5Hash(f),
            MusicName = Path.GetFileNameWithoutExtension(f)
        })];
    }
}
