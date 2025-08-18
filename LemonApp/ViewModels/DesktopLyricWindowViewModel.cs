using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.Views.UserControls;
using Lyricify.Lyrics.Models;
using System;
using System.Linq;
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
        _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>();
        _settingsMgr.OnDataChanged += _settingsMgr_OnDataChanged;
        ShowTranslation = _settingsMgr.Data.ShowTranslation;
        _mediaPlayerService.OnPlay += _mediaPlayerService_OnPlay;
        _mediaPlayerService.OnPaused += _mediaPlayerService_OnPaused;
        _mediaPlayerService.OnLoaded += _mediaPlayerService_OnLoaded;
        CustomLyricControlStyle();
    }

    private async void _settingsMgr_OnDataChanged()
    {
        await _settingsMgr.LoadAsync();
        LyricControl.Dispatcher.Invoke(ApplySettings);
    }

    public Action? UpdateAnimation { get;set; }
    public Action<FrameworkElement>? ScrollLrc { get; set; }
    private void ApplySettings()
    {
#pragma warning disable MVVMTK0034 
        if (_lyricControl != null)
        {
            ShowTranslation = _settingsMgr.Data.ShowTranslation;
            _lyricControl.FontFamily = new FontFamily(_settingsMgr.Data.FontFamily);
            _lyricControl.FontSize = _settingsMgr.Data.LrcFontSize*0.6;
            foreach (var block in _lyricControl.MainSyllableLrcs)
            {
                block.Value.FontSize = _settingsMgr.Data.LrcFontSize;
            }
        }
#pragma warning restore MVVMTK0034
    }
    private void _mediaPlayerService_OnLoaded(Music obj)
    {
        //有新的歌曲加载时，先清除当前歌词
        LyricControl.ClearAll();
    }
    private static readonly SolidColorBrush NormalLrcColor = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xEF));
    private void CustomLyricControlStyle()
    {
#pragma warning disable MVVMTK0034
        ApplySettings();
        _lyricControl.TranslationLrc.TextAlignment = TextAlignment.Center;
        _lyricControl.MainLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;
        _lyricControl.RomajiLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;

        _lyricControl.CustomNormalColor = NormalLrcColor;
        _lyricControl.TranslationLrc.Foreground = NormalLrcColor;
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
        if (LyricControl.IsVisible)
        {
            int ms = (int)_mediaPlayerService.Player.Position.TotalMilliseconds;
            LyricControl.Dispatcher.Invoke(() =>
            { 
                LyricControl.UpdateTime(ms);
                var block = LyricControl.MainSyllableLrcs.FirstOrDefault(x => x.Key.StartTime > ms).Value;
                ScrollLrc?.Invoke(block);
            });
        }
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
    public async void Update(LrcLine lrc)
    {
        UpdateAnimation?.Invoke();
        double fontSize = _settingsMgr.Data.LrcFontSize;
        await Task.Delay(200);
        
        if (lrc.Lrc is LineInfo pure)
            LyricControl.LoadPlainLrc(pure.Text, fontSize);
        else if (lrc.Lrc is SyllableLineInfo line)
            LyricControl.LoadMainLrc(line.Syllables, fontSize);

        if (lrc.Romaji is LineInfo pureRomaji)
            LyricControl.LoadPlainRomaji(pureRomaji.Text);
        else if (lrc.Romaji is SyllableLineInfo romaji)
            LyricControl.LoadRomajiLrc(romaji ?? new SyllableLineInfo([]));
        else LyricControl.LoadRomajiLrc(new([]));

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

    [RelayCommand]
    private void FontSizeUp()
    {
        _settingsMgr.Data.LrcFontSize += 2;
        ApplySettings();
    }
    [RelayCommand]
    private void FontSizeDown()
    {
        _settingsMgr.Data.LrcFontSize -= 2;
        ApplySettings();
    }
}
