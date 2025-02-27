using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Media;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace LemonApp.Services;

public class DownloadService : IHostedService
{
    private SettingsMgr<DownloadServiceCache>? settingsMgr;
    private readonly AppSettingsService appSettings;
    private readonly HttpClient hc;
    private readonly UserProfileService us;
    private readonly AudioGetter audioGetter;
    private readonly ConcurrentQueue<Music> tasks = new();

    public DownloadService(IHttpClientFactory hcFactory,
        UserProfileService user,
        AppSettingsService appSettingsService,
        MediaPlayerService mediaPlayerService)
    {
        us = user;
        audioGetter = mediaPlayerService.AudioGetter??throw new InvalidOperationException("Media Components is not ready yet.");
        appSettings = appSettingsService;
        hc = hcFactory.CreateClient(App.PublicClientFlag);
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
    }
    public void Init()
    {
        settingsMgr = appSettings.GetConfigMgr<DownloadServiceCache>()!;
        if(string.IsNullOrEmpty(settingsMgr.Data.DefaultPath) || Directory.Exists(settingsMgr.Data.DefaultPath))
        {
            settingsMgr.Data.DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),"Lemon App");
        }
    }
    public void PushTask(Music music)
    {
        
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
