using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
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
     
    private void _mediaPlayerService_OnLoaded(MusicLib.Abstraction.Music.DataTypes.Music obj)
    {
        LyricText = obj.MusicName + "\r\n";
        TransText = obj.SingerText;
    }

    private void _mediaPlayerService_OnPaused(MusicLib.Abstraction.Music.DataTypes.Music obj)
    {
        IsPlaying = false;
    }

    private void _mediaPlayerService_OnPlay(MusicLib.Abstraction.Music.DataTypes.Music obj)
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
    private bool _showTranslation = true;
    partial void OnShowTranslationChanged(bool value)
    {
        _settingsMgr.Data.ShowTranslation = value;
        if (value)
        {
            TransText = lrcLine?.Trans;
        }
        else
        {
            TransText = null;
        }
    }

    private MusicLib.Abstraction.Lyric.DataTypes.LrcLine? lrcLine;
    public void Update(MusicLib.Abstraction.Lyric.DataTypes.LrcLine lrc)
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
    [RelayCommand]
    private void ShowOrHideTranslation()
    {
        ShowTranslation = !ShowTranslation;
    }
}
