using LemonApp.MusicLib.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LemonApp.Common.Funcs;
using LemonApp.Common.WinAPI;
using System.IO;
using LemonApp.Common.Configs;
using LemonApp.Common;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LemonApp.Services;

public class MediaPlayerService(UserProfileService userProfileService,
    IHttpClientFactory httpClientFactory,
   // SharedLaClient sharedLaClient,
    AppSettingService appSettingsService):IDisposable
{
    private MusicPlayer _player;
    private bool _isPlaying = false;
    private readonly SMTCCreator _smtc = new("Lemon App");
    private readonly UserProfileService _userProfileService = userProfileService;
    private SettingsMgr<PlayingPreference> _playingMgr = appSettingsService.GetConfigMgr<PlayingPreference>();
    private readonly HttpClient hc= httpClientFactory.CreateClient(App.PublicClientFlag);
    //private readonly SharedLaClient _sharedLaClient = sharedLaClient;

    public SMTCCreator SMTC => _smtc;
    public AudioGetter? AudioGetter { get; private set; }
    public MusicPlayer Player { get => _player; }
    public Music? CurrentMusic { get; private set; }
    public MusicQuality CurrentQuality { get; private set; }
    public event Action<Music>? OnLoaded,OnPlay,OnPaused, OnAddToPlayNext, FailedToLoadMusic;
    public event Action<IList<Music>>? OnAddListToPlayNext;
    public event Action? OnEnd, OnPlayNext, OnPlayLast,OnQualityChanged;
    public event Action<IList<Music>>? OnNewPlaylistReceived;
    public Action<long, long>? CacheProgress;
    public Action? CacheFinished,CacheStarted;

    /// <summary>
    /// Initialize when app starts, called by ApplicationService
    /// </summary>
    /// <returns></returns>
    public async Task Init()
    {
        AudioGetter = new(hc, _userProfileService.GetAuth,_userProfileService.GetNeteaseAuth,/*_sharedLaClient,*/_userProfileService.GetSharedLaToken);
        await MusicPlayer.PrepareDll();
        _player = new();
        _smtc.Next += Smtc_Next;
        _smtc.Previous += Smtc_Previous;
        _smtc.PlayOrPause += Smtc_PlayOrPause;
        _smtc.SeekRequested += Smtc_SeekRequested;
        App.Services.GetRequiredService<MyToolBarLyricClient>().Connect();
    }

    private void Smtc_SeekRequested(TimeSpan pos)
    {
        Position = pos;
    }

    private void Smtc_PlayOrPause(object? sender, EventArgs e)
    {
        if (CurrentMusic != null)
        {
            if (_isPlaying)
                Pause();
            else Play();
        }
    }

    private void Smtc_Previous(object? sender, EventArgs e)
    {
        PlayLast();
    }

    private void Smtc_Next(object? sender, EventArgs e)
    {
        PlayNext();
    }

    public void Dispose()
    {
        _smtc.Dispose();
        _player.Free();
    }

    public async Task LoadThenPlay(Music music)
    {
        if (await Load(music))
            Play();
    }
    public void ReplacePlayFile(string path)
    {
        if (File.Exists(path))
        {
            _player.Load(path);
            _player.Play();
        }
    }
    public Task<bool> Load(Music music)
    {
        return LoadMusic(music, _playingMgr.Data.Quality);
    }
    public void RewriteMusicMetadata(Music music)
    {
        CurrentMusic = music;
        _smtc.SetMediaStatus(SMTCMediaStatus.Paused);
        _smtc.Info.SetTitle(music.MusicName)
                        .SetArtist(music.SingerText)
                        .SetAlbumTitle(music.Album?.Name?? "")
                        //.SetThumbnail(await CoverGetter.GetCoverImgUrl(() => hc, _userProfileService.GetAuth(), music))
                        .Update();

        OnLoaded?.Invoke(music);
    }
    private async Task<bool> LoadMusic(Music music, MusicQuality prefer)
    {
        if (AudioGetter == null)
            throw new InvalidOperationException("MediaPlayerService not initialized.");

        Pause();
        //set flag first
        RewriteMusicMetadata(music);

        //如果是本地歌曲，直接检索文件
        if (music.LocalPath!=null)
        {
            if (File.Exists(music.LocalPath))
            {
                _player.Load(music.LocalPath);
                string exName = new FileInfo(music.LocalPath).Extension;
                CurrentQuality= AudioGetter.GetQualityByExtensionName(exName);
                return true;
            }
            else
            {
                //本地文件不存在
                FailedToLoadMusic?.Invoke(music);
                return false;
            }
        }

        bool loadSucceeded= false;

        //先检索本地缓存 按照音质高到低依次检索
        bool playLocalFile = false;
        var matched = AudioGetter.GetFinalQuality(music.Quality, prefer);
        while (true)
        {
            var quality = AudioGetter.QualityMatcher(matched);
            var cacheFile = Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Music), music.MusicID + quality[0]);
            if (File.Exists(cacheFile))
            {
                _player.Load(cacheFile);
                CurrentQuality = matched;
                playLocalFile = true;
                loadSucceeded = true;
                break;
            }
            if (matched == MusicQuality.Std)
            {
                //最低音质也没找到
                break;
            }
            matched--;
        }

        //本地文件未匹配成功则请求加载网络
        if(!playLocalFile){
            var url = await AudioGetter.GetUrlAsync(music, prefer);
            if (url is null || url.Url is null)
            {
                loadSucceeded = false;
                FailedToLoadMusic?.Invoke(music);
            }
            else
            {
                var quality = AudioGetter.QualityMatcher(url.Quality);
                var cacheFile = Path.Combine(CacheManager.GetCachePath(CacheManager.CacheType.Music), music.MusicID + quality[0]);
                _player.LoadUrl(cacheFile, url.Url, CacheProgress, CacheFinished);
                CurrentQuality = url.Quality;
                loadSucceeded = true;
                CacheStarted?.Invoke();
            }
        }
        OnQualityChanged?.Invoke();
        return loadSucceeded;
    }
    /// <summary>
    /// volume: 0~1
    /// </summary>
    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = (float)value;
    }
    public TimeSpan Duration => _player.Duration;
    public TimeSpan Position
    {
        get  {
            int total = (int)_player.Duration.TotalSeconds;
            if (total>0&&(int)_player.Position.TotalSeconds >= total)
            {
                OnEnd?.Invoke();
            }
            _smtc.SetMediaPosition(Duration, _player.Position);
            return _player.Position;
        }
        set => _player.Position = value;
    }
    public void Play()
    {
        _player.Play();
        _isPlaying = true;
        _smtc.SetMediaStatus(SMTCMediaStatus.Playing);
        OnPlay?.Invoke(CurrentMusic!);
    }
    public void Pause()
    {
        if (_isPlaying)
        {
            _player.Pause();
            _isPlaying = false;
            _smtc.SetMediaStatus(SMTCMediaStatus.Paused);
            OnPaused?.Invoke(CurrentMusic!);
        }
    }
    public void PlayNext()
    {
        OnPlayNext?.Invoke();
    }
    public void PlayLast()
    {
        OnPlayLast?.Invoke();
    }

    public bool IsPlaying=> _isPlaying;

    public void ReplacePlayList(IList<Music> list)
    {
        if(list!=null&&list.Any())
        {
            OnNewPlaylistReceived?.Invoke(list);
        }
    }
    public void AddToPlayNext(Music music)
    {
        OnAddToPlayNext?.Invoke(music);
    }
    public void AddToPlayNext(IList<Music> list)
    {
        OnAddListToPlayNext?.Invoke(list);
    }
}
