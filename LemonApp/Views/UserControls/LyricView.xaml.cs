using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Lyric;
using LemonApp.Services;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

//TODO: 提供效果选择
namespace LemonApp.Views.UserControls
{

    /// <summary>
    /// LyricView.xaml 的交互逻辑
    /// </summary>
    public partial class LyricView : UserControl
    {
        private readonly SettingsMgr<LyricOption> _settings;

        public LyricView(AppSettingService appSettingsService)
        {
            InitializeComponent();

            _settings = appSettingsService.GetConfigMgr<LyricOption>();
            _settings.OnDataChanged += Settings_OnDataChanged;
            Loaded += LyricView_Loaded;
        }


        #region Self-adaption
        /// <summary>
        /// respond to LyricOption changed
        /// </summary>
        private void LyricView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

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

        private void RefreshHostSettings()
        {
            LrcHost.SetShowTranslation(_settings.Data.ShowTranslation&&IsTranslationAvailable);
            LrcHost.SetShowRomaji(_settings.Data.ShowRomaji&&IsRomajiAvailable);
            LrcHost.ApplyFontSize(_settings.Data.FontSize, LyricFontSizeScale);
        }

        #endregion

        #region Apperance
        public double LyricFontSize = 24;
        public const double LyricFontSizeScale = 0.6;
        #endregion

        [RelayCommand]
        public void FontSizeUp() => SetFontSize((int)LyricFontSize +2);
        [RelayCommand]
        public void FontSizeDown() => SetFontSize((int)LyricFontSize - 2);
        public void SetFontSize(int size)
        {
            LyricFontSize = size;
            _settings.Data.FontSize = size;
            LrcHost.ApplyFontSize(size,LyricFontSizeScale);
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
        public event Action<(LyricsData? lrc, LyricsData? trans, LyricsData? romaji, bool isPureLrc)> OnLyricLoaded;

        private string? _handlingMusic = null;
        public  async Task LoadFromMusic(Music m)
        {
            LrcHost.Reset();
            _handlingMusic = m.MusicID;
            if(await LoadLyricForMusic(m) is { } dt &&_handlingMusic == m.MusicID)
            {
               var model= LoadLrc(dt);
                if(model.lrc == null) return;
                Dispatcher.Invoke(() => {
                    IsTranslationAvailable = model.trans != null;
                    IsRomajiAvailable = model.romaji != null;
                    LrcHost.Load(model.lrc, model.trans, model.romaji, model.isPureLrc);
                    RefreshHostSettings();
                    OnLyricLoaded?.Invoke(model);
                });
            }
        }
        public static async Task<LyricData?> LoadLyricForMusic(Music m)
        {
            var path = CacheManager.GetCachePath(CacheManager.CacheType.Lyric);
            path = System.IO.Path.Combine(path, m.MusicID + ".lmrc");
            if (await Settings.LoadFromJsonAsync<LyricData>(path, false) is { } local)
            {
                return local;
            }
            else
            {
                if(await GetLyricAsync(m) is { } ly)
                {
                    await Settings.SaveAsJsonAsync(ly, path, false);
                    return ly;
                }
            }
            return null;
        }

        private static async Task<LyricData?> GetLyricAsync(Music m)
        {
            if (m.Source == Platform.qq)
            {
                var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
                var dt = await TencGetLyric.GetLyricsAsync(hc, m.Littleid);
                if (dt == null) return null;
                return new() { Id = m.MusicID, Lyric = dt.Lyrics, Trans = dt.Trans, Romaji = dt.Romaji };
            }else if (m.Source == Platform.wyy)
            {
                var api = new Lyricify.Lyrics.Providers.Web.Netease.Api();
                var data = await api.GetLyricNew(m.MusicID);
                if (data == null) return null;
                var result= new LyricData
                {
                    Type=LyricType.Wyy,
                    Id = m.MusicID,
                    Lyric = data.Yrc?.Lyric ?? data.Lrc.Lyric,
                    Trans = data.Tlyric.Lyric,
                    Romaji = data.Romalrc.Lyric
                };
                if(data.Yrc?.Lyric == null && data.Lrc.Lyric != null)
                {
                    //没有单句分词
                    result.Type = LyricType.PureWyy;
                }
                return result;
            }
            return null;
        }

        public static (LyricsData? lrc, LyricsData? trans, LyricsData? romaji, bool isPureLrc) LoadLrc(LyricData dt)
        {
            if (dt.Lyric == null) return (null,null,null,false);
            var rawType =dt.Type switch
            {
                LyricType.Wyy => LyricsRawTypes.Yrc,
                LyricType.PureWyy => LyricsRawTypes.Lrc,
               LyricType.QQ => LyricsRawTypes.Qrc,
                _ => LyricsRawTypes.Lrc
            };
            var lrc = ParseHelper.ParseLyrics(dt.Lyric,rawType);
            LyricsData? trans = null, romaji = null;
            if (!string.IsNullOrEmpty(dt.Trans))
                trans = ParseHelper.ParseLyrics(dt.Trans, LyricsRawTypes.Lrc);

            if (!string.IsNullOrEmpty(dt.Romaji))
                romaji = ParseHelper.ParseLyrics(dt.Romaji, rawType);

            if (lrc != null)
                return (lrc, trans, romaji, dt.Type == LyricType.PureWyy);
            return (null, null, null, false);
        }
    }
}
