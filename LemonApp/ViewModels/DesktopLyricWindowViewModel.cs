using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.Views.UserControls;
using Lyricify.Lyrics.Models;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.ViewModels;
/// <summary>
/// [Singleton] ViewModel for DesktopLyricWindow, reserved at MainWindowViewModel.cs
/// </summary>
public partial class DesktopLyricWindowViewModel:ObservableObject
{
    public DesktopLyricWindowViewModel(
        MediaPlayerService mediaPlayerService,
        AppSettingService appSettingsService)
    {
        _mediaPlayerService = mediaPlayerService;
        IsPlaying = _mediaPlayerService.IsPlaying;
        _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>()!;
        ShowTranslation = _settingsMgr.Data.ShowTranslation;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;

        _lyricControl.FontSize = 16;
    }

    private void _mediaPlayerService_OnPaused(Music obj)
    {
        IsPlaying = false;
    }

    private void _mediaPlayerService_OnPlay(Music obj)
    {
        IsPlaying = true;
    }

    public void SetTimerTick(Timer timer)
    {
        _timer = timer;
        timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        LyricControl.Dispatcher.Invoke(() => {
            LyricControl.UpdateTime((int)_mediaPlayerService.Player.Position.TotalMilliseconds);
        });
    }

    private Timer? _timer;
    private readonly MediaPlayerService _mediaPlayerService;
    private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;
    [ObservableProperty]
    private bool _isPlaying=false;
    [ObservableProperty]
    private bool _showTranslation = true;
    [ObservableProperty]
    private LyricLineControl _lyricControl = new() { IsCurrent=true};
    partial void OnShowTranslationChanged(bool value)
    {
        _settingsMgr.Data.ShowTranslation = value;
        if (LyricControl != null)
        {
            var visible= value ? Visibility.Visible : Visibility.Collapsed;
            LyricControl.TranslationLrc.Visibility = visible;
            LyricControl.RomajiLrcContainer.Visibility = visible;
        }
    }

    public void Update(LrcLine lrc)
    {
        LyricControl.LoadMainLrc(lrc.Lrc.Syllables,fontSize: 26);
        LyricControl.LoadRomajiLrc(lrc.Romaji ?? new SyllableLineInfo([]));
        LyricControl.TranslationLrc.Text = lrc.Trans;
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
    private void ShowMainWindow()=> App.Current.MainWindow.ShowWindow();
}
