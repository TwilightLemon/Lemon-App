using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;

namespace LemonApp.ViewModels;
/// <summary>
/// [Singleton] ViewModel for DesktopLyricWindow, reserved at MainWindowViewModel.cs
/// </summary>
public partial class DesktopLyricWindowViewModel:ObservableObject
{
    public DesktopLyricWindowViewModel(
        MediaPlayerService mediaPlayerService,
        AppSettingsService appSettingsService)
    {
        _mediaPlayerService = mediaPlayerService;
        IsPlaying = _mediaPlayerService.IsPlaying;
        _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>()!;
        ShowTranslation = _settingsMgr.Data.ShowTranslation;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;
        _mediaPlayerService.OnLoaded += _mediaPlayerService_OnLoaded;
    }
     
    private void _mediaPlayerService_OnLoaded(Music obj)
    {
        LyricText = obj.MusicName + "\r\n";
        TransText = obj.SingerText;
    }

    private void _mediaPlayerService_OnPaused(Music obj)
    {
        IsPlaying = false;
    }

    private void _mediaPlayerService_OnPlay(Music obj)
    {
        IsPlaying = true;
    }

    private readonly MediaPlayerService _mediaPlayerService;
    private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;
    [ObservableProperty]
    private bool _isPlaying=false;
    [ObservableProperty]
    private string? _lyricText;
    [ObservableProperty]
    private string? _transText;
    [ObservableProperty]
    private string? _romajiText;
    [ObservableProperty]
    private bool _showTranslation = true;
    partial void OnShowTranslationChanged(bool value)
    {
        _settingsMgr.Data.ShowTranslation = value;
        if (value)
        {
            TransText = lrcLine?.Trans;
            RomajiText = lrcLine?.Romaji;
        }
        else
        {
            TransText = null;
            RomajiText = null;
        }
    }

    private LrcLine? lrcLine;
    public void Update(LrcLine lrc)
    {
        lrcLine = lrc;
        if (!string.IsNullOrEmpty(lrc.Trans)&&ShowTranslation)
        {
            LyricText = lrc.Lyric + "\r\n";
        }
        else
        {
            LyricText = lrc.Lyric;
        }
        TransText = ShowTranslation ? lrc.Trans : null;
        RomajiText = ShowTranslation ? lrc.Romaji+"\r\n" : null;
    }

    [RelayCommand]
    private void PlayOrPause()
    {
        if (IsPlaying)
        {
            _mediaPlayerService.Pause();
        }
        else
        {
            _mediaPlayerService.Play();
        }
    }
    [RelayCommand]
    private void PlayNext()
    {
        _mediaPlayerService.PlayNext();
    }
    [RelayCommand]
    private void PlayLast()
    {
        _mediaPlayerService.PlayLast();
    }
}
