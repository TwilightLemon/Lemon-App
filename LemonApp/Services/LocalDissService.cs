using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        var meta = new LocalDirMeta()
        {
            Path = path,
            Name = info.Name
        };
        LocalDirs.Add(meta);
    }

    public void SyncWithDir(string path)
    {
        var info = new DirectoryInfo(path);
        var musics = new Dictionary<string, Music>();
        foreach(var file in info.EnumerateFiles())
        {

        }
    }

}
