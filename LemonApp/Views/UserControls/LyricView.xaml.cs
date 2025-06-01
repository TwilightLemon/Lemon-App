using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Lyric;
using LemonApp.Services;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Helpers.Types;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

//TODO: 提供效果选择
namespace LemonApp.Views.UserControls
{

    /// <summary>
    /// LyricView.xaml 的交互逻辑
    /// </summary>
    public partial class LyricView : UserControl
    {
        private readonly HttpClient? _hc;
        private readonly SettingsMgr<LyricOption> _settings;

        public LyricView(IHttpClientFactory httpClientFactory,
            AppSettingService appSettingsService)
        {
            InitializeComponent();

            _settings = appSettingsService.GetConfigMgr<LyricOption>();
            _settings.OnDataChanged += Settings_OnDataChanged;
            _hc = httpClientFactory.CreateClient(App.PublicClientFlag);
        }

        #region Self-adaption
        /// <summary>
        /// respond to LyricOption changed
        /// </summary>
        private async void Settings_OnDataChanged()
        {
            await _settings.LoadAsync();
            ApplySettings();
        }
        private void ApplySettings()
        {
            this.Dispatcher.Invoke(() =>
            {
                IsShowTranslation = _settings?.Data?.ShowTranslation is true;
                IsShowRomaji = _settings?.Data?.ShowRomaji is true;
                SetFontSize(_settings?.Data?.FontSize ?? (int)LyricFontSize);
                this.FontFamily = new FontFamily(_settings?.Data?.FontFamily ?? "Segou UI");
            });
        }

        #endregion

        #region Apperance
        public double LyricFontSize = 24;
        public const double LyricFontSizeDelta = 6;
        #endregion

        [RelayCommand]
        public void FontSizeUp() => SetFontSize((int)LyricFontSize +2);
        [RelayCommand]
        public void FontSizeDown() => SetFontSize((int)LyricFontSize - 2);
        public void SetFontSize(int size)
        {
            LyricFontSize = size;
            _settings.Data.FontSize = size;
            LrcHost.ApplyFontSize(size,LyricFontSizeDelta);
            LrcHost.ScrollToCurrent();
        }


        public bool IsRomajiAvailable
        {
            get { return (bool)GetValue(IsRomajiAvailableProperty); }
            private set { SetValue(IsRomajiAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsRomajiAvailableProperty =
            DependencyProperty.Register("IsRomajiAvailable", typeof(bool), typeof(LyricView), new PropertyMetadata(false));


        public bool IsTranslationAvailable
        {
            get { return (bool)GetValue(IsTranslationAvailableProperty); }
            private set { SetValue(IsTranslationAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsTranslationAvailableProperty =
            DependencyProperty.Register("IsTranslationAvailable", typeof(bool), typeof(LyricView), new PropertyMetadata(false));

        public bool IsShowTranslation
        {
            get => (bool)GetValue(IsShowTranslationProperty);
            set => SetValue(IsShowTranslationProperty, value);
        }

        public static readonly DependencyProperty IsShowTranslationProperty =
            DependencyProperty.Register("IsShowTranslation", typeof(bool), typeof(LyricView), new PropertyMetadata(true, OnIsShowTranslationChanged));

        private static void OnIsShowTranslationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricView view)
            {
                view.SetShowTranslation(e.NewValue is true);
            }
        }

        public bool IsShowRomaji
        {
            get => (bool)GetValue(IsShowRomajiProperty);
            set => SetValue(IsShowRomajiProperty, value);
        }

        public static readonly DependencyProperty IsShowRomajiProperty =
            DependencyProperty.Register("IsShowRomaji", typeof(bool), typeof(LyricView), new PropertyMetadata(true, OnIsShowRomajiChanged));

        private static void OnIsShowRomajiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricView view)
            {
                view.SetShowRomaji(e.NewValue is true);
            }
        }

        public void SetShowTranslation(bool show)
        {
            _settings.Data.ShowTranslation = show;
            LrcHost.SetShowTranslation(show);
        }
        public void SetShowRomaji(bool show)
        {
            _settings.Data.ShowRomaji = show;
            LrcHost.SetShowRomaji(show);
        }

        private string? _handlingMusic = null;
        public async Task LoadFromMusic(Music m)
        {
            LrcHost.Reset();
            _handlingMusic = m.MusicID;
            var path = CacheManager.GetCachePath(CacheManager.CacheType.Lyric);
            path = System.IO.Path.Combine(path, m.MusicID + ".lmrc");
            if (await Settings.LoadFromJsonAsync<LyricData>(path, false) is { } local)
            {
                LoadLrc(local);
            }
            else
            {
                if (_hc == null) return;
                if(await GetLyricAsync(m) is { } ly)
                {
                    await Settings.SaveAsJsonAsync(ly, path, false);
                    if (_handlingMusic == m.MusicID)//防止异步加载时已经切换歌曲
                        LoadLrc(ly);
                }
            }
        }

        public static async Task<LyricData?> GetLyricAsync(Music m)
        {
            if (m.Source == Platform.qq)
            {
                var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
                var dt = await TencGetLyric.GetLyricsAsync(hc, m.Littleid);
                if (dt == null) return null;
                return new() { Id = m.MusicID, Lyric = dt.Lyrics, Trans = dt.Trans, Romaji = dt.Romaji };
            }
            return null;
        }

        private void LoadLrc(LyricData dt)
        {
            if (dt.Lyric == null) return;
            var lrc = ParseHelper.ParseLyrics(dt.Lyric, LyricsRawTypes.Qrc);
            LyricsData? trans = null, romaji = null;
            if (!string.IsNullOrEmpty(dt.Trans))
            {
                trans = ParseHelper.ParseLyrics(dt.Trans, LyricsRawTypes.Lrc);
                IsTranslationAvailable = true;
            }else IsTranslationAvailable = false;

            if (!string.IsNullOrEmpty(dt.Romaji))
            {
                romaji = ParseHelper.ParseLyrics(dt.Romaji, LyricsRawTypes.Qrc);
                IsRomajiAvailable = true;
            }else IsRomajiAvailable = false;

            if (lrc != null)
                Dispatcher.Invoke(() => {
                    LrcHost.Load(lrc, trans, romaji);
                    ApplySettings();
                });
        }
    }
}
