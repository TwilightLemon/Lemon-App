using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.Views.UserControls;
using Lyricify.Lyrics.Models;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        CustomLyricControlStyle();
    }

    private void CustomLyricControlStyle()
    {
#pragma warning disable MVVMTK0034
        _lyricControl.FontSize = 18;
        _lyricControl.TranslationLrc.TextAlignment = TextAlignment.Center;
        _lyricControl.MainLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;
        _lyricControl.RomajiLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;

        _lyricControl.CustomNormalColor = new SolidColorBrush(Color.FromRgb( 0xEF, 0xEF, 0xEF));
        _lyricControl.SetResourceReference(LyricLineControl.CustomHighlighterColorProperty, "HighlightThemeColor");
#pragma warning restore MVVMTK0034
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

    public Action? UpdateAnimation;
    public async void Update(LrcLine lrc)
    {
        UpdateAnimation?.Invoke();
        await Task.Delay(200);
        LyricControl.LoadMainLrc(lrc.Lrc.Syllables,fontSize: 32);
        LyricControl.LoadRomajiLrc(lrc.Romaji ?? new SyllableLineInfo([]));
        LyricControl.TranslationLrc.Text = lrc.Trans;
        if (ShowTranslation)
            LyricControl.TranslationLrc.Visibility = string.IsNullOrEmpty(lrc.Trans) ? Visibility.Collapsed : Visibility.Visible;
        else 
        {
            LyricControl.TranslationLrc.Visibility = Visibility.Collapsed;
            LyricControl.RomajiLrcContainer.Visibility = Visibility.Collapsed;
        }
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
