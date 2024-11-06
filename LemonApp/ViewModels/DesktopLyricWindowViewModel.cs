using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Services;

namespace LemonApp.ViewModels;
/// <summary>
/// [Singleton] ViewModel for DesktopLyricWindow, reserved at MainWindowViewModel.cs
/// </summary>
public partial class DesktopLyricWindowViewModel:ObservableObject
{
    public DesktopLyricWindowViewModel(MediaPlayerService mediaPlayerService)
    {
        _mediaPlayerService = mediaPlayerService;
        IsPlaying = _mediaPlayerService.IsPlaying;
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
    [ObservableProperty]
    private bool _isPlaying=false;
    [ObservableProperty]
    private string? _lyricText;
    [ObservableProperty]
    private string? _transText;

    public void Update(MusicLib.Abstraction.Lyric.DataTypes.LrcLine lrc)
    {
        if (!string.IsNullOrEmpty(lrc.Trans))
        {
            LyricText = lrc.Lyric + "\r\n";
        }
        else
        {
            LyricText = lrc.Lyric;
        }
        TransText = lrc.Trans;
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
