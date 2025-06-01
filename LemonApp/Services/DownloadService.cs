using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Downloader;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Media;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DLService = Downloader.DownloadService;

namespace LemonApp.Services;

public partial class DownloadItemTask(Music music,string filePath,MusicQuality musicQuality) : ObservableObject
{
    [ObservableProperty]
    private Music _music = music;
    [ObservableProperty]
    private string _filePath = filePath;
    [ObservableProperty]
    private MusicQuality _quality = musicQuality;
    [ObservableProperty]
    private double _downloadingProgress = 0d;
    [ObservableProperty]
    private bool _finished = false;
    [ObservableProperty]
    private bool _dropped = false;

    public DLService? DownloadService { get; internal set; }

    [RelayCommand]
    public void Drop()
    {
        if(Dropped) return;
        Dropped = true;
        if (DownloadService is { Status: DownloadStatus.Running or DownloadStatus.Paused } ds)
            ds.CancelAsync();
    }
}

public class DownloadService(AppSettingService appSettingsService,
    MediaPlayerService mediaPlayerService) : IHostedService
{
    private SettingsMgr<DownloadPreference> settingsMgr = appSettingsService.GetConfigMgr<DownloadPreference>();

    private AudioGetter AudioGetter => mediaPlayerService.AudioGetter ?? throw new InvalidOperationException("Media Components is not ready yet.");
    private readonly ConcurrentQueue<DownloadItemTask> tasks = new();

    private CancellationTokenSource cts = new();
    private Task? downloadTask;
    private DownloadItemTask? downloadingTask;
    public event Action<bool>? OnDownloadTaskStateChanged;

    private static readonly DownloadConfiguration _downloadOpt = new()
    {
        ChunkCount = 8, // Number of file parts, default is 1
        ParallelDownload = true // Download parts in parallel (default is false)
    };
    private readonly DLService dl = new(_downloadOpt);

    public bool IsDownloading
    {
        get => downloadingTask?.DownloadService is { Status: DownloadStatus.Running };
    }

    public bool IsRunning =>downloadTask?.IsCompleted == false;

    public int TaskCount => tasks.Count;

    public MusicQuality DownloadQuality
    {
        get => settingsMgr.Data.PreferQuality;
        set => settingsMgr.Data.PreferQuality = value;
    }

    public string DownloadPath
    {
        get => settingsMgr.Data.DefaultPath!;
        set => settingsMgr.Data.DefaultPath = value;
    }

    public ObservableCollection<DownloadItemTask> History { get; internal set; } = [];
    private static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
    public void PushTask(Music music)
    {
        if(CreateTask(music) is { } task)
        {
            History.Add(task);
        }
    }

    public void PauseCurrentTask()
    {
        if (downloadingTask?.DownloadService is { Status: DownloadStatus.Running } task)
            task.Pause();
    }

    public void ResumeCurrentTask()
    {
        if (downloadingTask?.DownloadService is { Status: DownloadStatus.Paused } task)
            task.Resume();
    }

    public async void CancelAll()
    {
        await cts.CancelAsync();
        cts = new();
        downloadingTask?.Drop();
        while(!tasks.IsEmpty)
        {
            if (tasks.TryDequeue(out var task))
            {
                task.Drop();
            }
        }
    }


    private DownloadItemTask? CreateTask(Music music)
    {
        //check if cache file exists
        var finalQuality=AudioGetter.GetFinalQuality(music.Quality, DownloadQuality);
        var quality = AudioGetter.QualityMatcher(finalQuality);
        var cacheFile = Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Music), music.MusicID + quality[0]);
        var dlFile = Path.Combine(DownloadPath, SanitizeFileName($"{music.MusicName} - {music.SingerText}{quality[0]}"));
        if (File.Exists(cacheFile))
        {
            //copy to download path
            File.Copy(cacheFile, dlFile);
            return new DownloadItemTask(music, dlFile, DownloadQuality) { Finished=true,DownloadingProgress = 100 };
        }
        //add to download queue
        if (tasks.Any(m => m.Music.MusicID == music.MusicID))
            return null;//already in queue
        var task = new DownloadItemTask(music, dlFile, DownloadQuality);
        tasks.Enqueue(task);
        CheckDownloadTask();
        return task;
    }

    private void CheckDownloadTask()
    {
        if(downloadTask == null || downloadTask.IsCompleted)
        {
            downloadTask = Task.Run(ProcessQueueAsync);
        }
    }

    private async Task ProcessQueueAsync()
    {
        OnDownloadTaskStateChanged?.Invoke(true);
        while (!cts.Token.IsCancellationRequested && tasks.TryDequeue(out var task))
        {
            if (task.Dropped) continue;
            if( (await AudioGetter.GetUrlAsync(task.Music, task.Quality))?.Url is { Length:>0} url)
            {
                downloadingTask = task;
                task.DownloadService = dl;
                await dl.DownloadFileTaskAsync(url, task.FilePath,cts.Token);
            }
            OnDownloadTaskStateChanged?.Invoke(true);
        }
        OnDownloadTaskStateChanged?.Invoke(false);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(settingsMgr.Data.DefaultPath) || !Directory.Exists(settingsMgr.Data.DefaultPath))
        {
            settingsMgr.Data.DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Lemon App");
        }
        if (!Directory.Exists(settingsMgr.Data.DefaultPath))
        {
            Directory.CreateDirectory(settingsMgr.Data.DefaultPath);
        }
        dl.DownloadProgressChanged += (s, e) =>
        {
            if (downloadingTask != null)
                downloadingTask.DownloadingProgress = e.ProgressPercentage;
        };
        dl.DownloadFileCompleted += (s, e) =>
        {
            if (downloadingTask != null)
                downloadingTask.Finished = true;
        };
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cts.Cancel();
        return Task.CompletedTask;
    }
}
