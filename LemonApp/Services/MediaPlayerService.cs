using LemonApp.MusicLib.Media;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using MusicDT = LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Services;

public class MediaPlayerService(UserProfileService userProfileService,IHttpClientFactory httpClientFactory)
{
    private MediaPlayer _mediaPlayer = new MediaPlayer();
    private AudioGetter? audioGetter = null;
    private readonly UserProfileService _userProfileService = userProfileService!;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient? hc;
    public MusicDT.Music? CurrentMusic { get; private set; }
    public event Action<MusicDT.Music>? OnLoaded,OnPlay,OnPaused,OnPlayNext,OnPlayLast;
    public void Init()
    {
        hc = _httpClientFactory.CreateClient(App.PublicClientFlag);
        audioGetter = new(hc, _userProfileService.GetAuth(), null);
        _mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
        _mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
    }
    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        OnPlayLast?.Invoke(CurrentMusic);
    }
    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        OnPlayNext?.Invoke(CurrentMusic);
    }

    private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        OnPaused?.Invoke(CurrentMusic);
    }

    private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
    {
        OnPlay?.Invoke(CurrentMusic);
    }

    public async Task Load(MusicDT.Music music)
    {
        if(audioGetter == null)
            throw new InvalidOperationException("MediaPlayerService not initialized.");

        Pause();
        CurrentMusic = music;
        var url = await audioGetter.GetUrlAsync(music, MusicDT.MusicQuality.SQ);
        if (url is null||url.Url is null)
            throw new InvalidOperationException("Failed to get music url.");
        var source = MediaSource.CreateFromUri(new Uri(url.Url));
        var item = new MediaPlaybackItem(source);
        var info = item.GetDisplayProperties();
        info.Type = MediaPlaybackType.Music;
        info.MusicProperties.Title = music.MusicName;
        info.MusicProperties.Artist = music.SingerText;
        info.MusicProperties.AlbumArtist = music.Album?.Name ?? "";
        info.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(await CoverGetter.GetCoverImgUrl(hc!, music)));
        item.ApplyDisplayProperties(info);
        _mediaPlayer.Source = item;
        //_mediaPlayer.SetUriSource(new Uri(url.Url));


        OnLoaded?.Invoke(music);
    }
    public double Volume
    {
        get => _mediaPlayer.Volume;
        set => _mediaPlayer.Volume = value;
    }
    public void Play()
    {
        _mediaPlayer.Play();
        OnPlay?.Invoke(CurrentMusic!);
    }
    public void Pause()
    {
        if (_mediaPlayer.CanPause)
        {
            _mediaPlayer.Pause();
            OnPaused?.Invoke(CurrentMusic!);
        }
    }
    public void PlayNext()
    {
        OnPlayNext?.Invoke(CurrentMusic!);
    }
    public void PlayLast()
    {
        OnPlayLast?.Invoke(CurrentMusic!);
    }
}
