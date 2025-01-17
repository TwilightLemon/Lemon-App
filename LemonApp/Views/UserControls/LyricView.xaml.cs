using EleCho.WpfSuite;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Lyric;
using LemonApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Common.Configs;
using CommunityToolkit.Mvvm.Input;

//TODO: 提供效果选择
namespace LemonApp.Views.UserControls
{
    internal class LrcItem(TextBlock container, TextBlock main)
    {
        public double Time { get; set; }
        public string Lyric { get; set; } = string.Empty;
        public TextBlock LrcTb { get; set; }=container;
        public TextBlock LrcMain { get; set; } = main;
        public TextBlock? LrcTrans { get; set; }
        public TextBlock? Romaji { get; set; }
    }

    /// <summary>
    /// LyricView.xaml 的交互逻辑
    /// </summary>
    public partial class LyricView : UserControl
    {
        private readonly HttpClient? _hc;
        private readonly SettingsMgr<LyricOption> _settings;
        private readonly UIResourceService _uiResourceService;
        private readonly List<LrcItem> LrcItems = [];
        private LrcItem? _currentLrc = null;
        public LyricView(IHttpClientFactory httpClientFactory,
            UIResourceService uiResourceService,
            AppSettingsService appSettingsService)
        {
            InitializeComponent();
            UpdateColorMode();

            _settings = appSettingsService.GetConfigMgr<LyricOption>()!;
            _hc = httpClientFactory.CreateClient(App.PublicClientFlag);
            _uiResourceService = uiResourceService;
            _uiResourceService.OnColorModeChanged += UiResourceService_OnColorModeChanged;

            SizeChanged += LyricView_SizeChanged;
            IsVisibleChanged += LyricView_IsVisibleChanged;
            Loaded += LyricView_Loaded;
        }

        #region Self-adaption
        private void LyricView_Loaded(object sender, RoutedEventArgs e)
        {
            IsShowTranslation = _settings?.Data?.ShowTranslation is true;
            IsShowRomaji = _settings?.Data?.ShowRomaji is true;
            SetFontSize(_settings?.Data?.FontSize ?? (int)LyricFontSize);
        }

        private void UpdateColorMode()
        {
            if (Foreground is SolidColorBrush color)
            {
                Hightlighter = new DropShadowEffect() { BlurRadius = 20, Color = color.Color, Opacity = 0.5, ShadowDepth = 0, Direction = 0 };
            }
        }
        private void UiResourceService_OnColorModeChanged() => UpdateColorMode();

        private async void LyricView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                await Task.Delay(300);
                RefreshCurrentLrcStyle();
            }
        }

        private void LyricView_SizeChanged(object sender, SizeChangedEventArgs e) => RefreshCurrentLrcStyle();
        #endregion
        #region Apperance

        public Thickness LyricMargin = new Thickness(20, 12, 0, 12);
        /// <summary>
        /// 非高亮歌词的透明度
        /// </summary>
        public double LyricOpacity = 0.5;
        /// <summary>
        /// 高亮歌词效果
        /// </summary>
        public Effect? Hightlighter;
        public Effect? NomalTextEffect = new BlurEffect() { Radius = 8 };
        public Effect? AroundTextEffect = new BlurEffect() { Radius = 5 };
        /// <summary>
        /// 歌词的文本对齐方式
        /// </summary>
        public TextAlignment TextAlignment = TextAlignment.Left;

        public double LyricFontSize = 24;
        public const double LyricFontSizeDelta = 6;
        public FontWeight NormalTextFontWeight = FontWeights.Normal;
        #endregion


        public event Action<LrcLine>? OnNextLrcReached;
        public LrcLine GetCurrentLrc() => new()
        {
            Lyric = _currentLrc?.Lyric ?? "",
            Trans = _currentLrc?.LrcTrans?.Text ?? "",
            Romaji = _currentLrc?.Romaji?.Text ?? "",
            Time = double.NaN
        };

        [RelayCommand]
        public void FontSizeUp() => SetFontSize((int)LyricFontSize +2);
        [RelayCommand]
        public void FontSizeDown() => SetFontSize((int)LyricFontSize - 2);
        public void SetFontSize(int size)
        {
            LyricFontSize = size;
            _settings.Data.FontSize = size;
            foreach (var item in LrcItems)
            {
                item.LrcTb!.FontSize = size;
                if (item.Romaji != null)
                    item.Romaji.FontSize = size - LyricFontSizeDelta;
                if (item.LrcTrans != null)
                    item.LrcTrans.FontSize = size - LyricFontSizeDelta;
            }
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
            get { return (bool)GetValue(IsShowTranslationProperty); }
            set
            {
                SetValue(IsShowTranslationProperty, value);
                _settings.Data.ShowTranslation = value;
            }
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
            get { return (bool)GetValue(IsShowRomajiProperty); }
            set
            {
                SetValue(IsShowRomajiProperty, value);
                _settings.Data.ShowRomaji = value;
            }
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
            foreach (var item in LrcItems)
            {
                if (item.LrcTrans != null)
                {
                    item.LrcTrans.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
        public void SetShowRomaji(bool show)
        {
            foreach (var item in LrcItems)
            {
                if (item.Romaji != null)
                {
                    item.Romaji.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private string? _handlingMusic = null;
        public async Task LoadFromMusic(Music m)
        {
            Reset();
            _handlingMusic = m.MusicID;
            var path = CacheManager.GetCachePath(CacheManager.CacheType.Lyric);
            path = System.IO.Path.Combine(path, m.MusicID + ".json");
            if (await Settings.LoadFromJsonAsync<LocalLyricData>(path, false) is { } local)
            {
                LoadLrc(local);
            }
            else
            {
                if (_hc == null) return;
                if (m.Source == Platform.qq)
                {
                    var ly = await GatitoGetLyric.GetTencLyricAsync(_hc, m.MusicID);
                    await Settings.SaveAsJsonAsync(ly, path, false);
                    if(_handlingMusic == m.MusicID)//防止异步加载时已经切换歌曲
                        LoadLrc(ly);
                    /*
                    var data=await TencGetLyric.GetLyricDataAsync(_hc,_auth, m.MusicID);
                    if(data != null&&data.Lyric!=null)
                    {
                        LocalLyricData ly=new();
                        ly.Id= m.MusicID;
                        var lyrics = LyricHelper.Format(data.Lyric);
                        List<(double, string?)>? trans = null;
                        List<string>? romaji = null;
                        if (data.Trans != null)
                        {
                             trans = LyricHelper.Format(data.Trans);
                        }
                        int i = 0;
                        foreach(var line in lyrics)
                        {
                            var transText = trans?.FirstOrDefault(m => m.Item1 >= line.Item1 - 2).Item2;
                            LrcLine lrcLine = new() {
                                Time = line.Item1,
                                Lyric = line.Item2!,
                                Trans=transText,
                                Romaji=romaji?[i]
                            };
                            i++;
                            ly.LyricData.Add(lrcLine);
                        }
                        await Settings.SaveAsJsonAsync(ly, path,false);
                        LoadLrc(ly);
                    }
                    */
                }
            }
        }
        private void Reset()
        {
            scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
            scrollviewer.ScrollToTop();
            LyricPanel.Children.Clear();
            LrcItems.Clear();
            GC.Collect();
        }
        private void LoadLrc(LocalLyricData data)
        {
            IsTranslationAvailable = data.HasTrans;
            IsRomajiAvailable = data.HasRomaji;

            //占位
            LyricPanel.Children.Add(new Border() { Height = 200, Background = Brushes.Transparent });
            foreach (var line in data.LyricData)
            {
                //Container
                TextBlock tb = new()
                {
                    FontSize = LyricFontSize,
                    //Foreground = NormalLrcColor,
                    TextAlignment = TextAlignment,
                    Opacity = LyricOpacity,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.None,
                    Margin = LyricMargin,
                    Effect = NomalTextEffect,
                    FontWeight = NormalTextFontWeight
                };
                //Romaji
                TextBlock? romaji = null;
                if (line.Romaji != null)
                {
                    romaji = new()
                    {
                        Text = line.Romaji,
                        Opacity = 0.8,
                        FontSize = LyricFontSize - LyricFontSizeDelta,
                        FontWeight = FontWeights.Regular,
                        //Foreground = NormalLrcColor,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None
                    };
                    if (!IsShowRomaji) romaji.Visibility = Visibility.Collapsed;
                    tb.Inlines.Add(romaji);
                    tb.Inlines.Add(new LineBreak());
                }
                //Main Lyric
                TextBlock lyric = new()
                {
                    Text = line.Lyric,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.None
                };
                LrcItem item = new(tb,lyric);
                item.Romaji = romaji;
                item.Time = line.Time;
                item.LrcMain = lyric;
                item.Lyric = line.Lyric;
                tb.Inlines.Add(lyric);
                //Translation
                if (line.Trans != null)
                {
                    TextBlock trans = new()
                    {
                        FontWeight = FontWeights.Regular,
                        Text = line.Trans,
                        Opacity = 0.8,
                        FontSize = LyricFontSize - LyricFontSizeDelta,
                        //Foreground = NormalLrcColor,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None
                    };
                    if (!IsShowTranslation) trans.Visibility = Visibility.Collapsed;
                    item.LrcTrans = trans;
                    tb.Inlines.Add(new LineBreak());
                    tb.Inlines.Add(trans);
                }
                LrcItems.Add(item);
                LyricPanel.Children.Add(tb);
            }
            LyricPanel.Children.Add(new Border() { Height = 200, Background = Brushes.Transparent });
        }


        private static double? pixelsPerDip;
        private static string InsertLineBreaks(TextBlock tb, string preText, double fontSize, double maxWidth)
        {
            var typeface = new Typeface(tb.FontFamily, tb.FontStyle, FontWeights.Bold, tb.FontStretch);
            pixelsPerDip ??= VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            double GetWidth(string text)
            {
                var formattedLine = new FormattedText(text, CultureInfo.CurrentCulture,
                                                                                    FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black, pixelsPerDip!.Value);
                return formattedLine.WidthIncludingTrailingWhitespace;
            }
            StringBuilder temp = new();
            string[] blocks;
            bool spaceSplit = preText.Contains(' ');
            if (spaceSplit)
                blocks = preText.Split(' ');
            else blocks = preText.Select(c => c.ToString()).ToArray();


            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;
                temp.Append(block);
                if (spaceSplit) temp.Append(' ');
                if (GetWidth(temp.ToString()) > maxWidth)
                {
                    int undoLength = block.Length + (spaceSplit ? 1 : 0);
                    temp.Remove(temp.Length - undoLength, undoLength);
                    temp.AppendLine();
                    temp.Append(block);
                    if (spaceSplit) temp.Append(' ');
                }
            }
            string result = temp.ToString();
            return result;
        }

        public void UpdateTime(double ms)
        {
            void reset(LrcItem? item)
            {
                if (item == null || item.LrcTb == null) return;
                item.LrcTb.FontWeight = NormalTextFontWeight;
                var da = new DoubleAnimation(LyricFontSize, TimeSpan.FromSeconds(0.3));
                da.EasingFunction = new CubicEase();
                item.LrcTb.BeginAnimation(FontSizeProperty, da);
                item.LrcTb.BeginAnimation(OpacityProperty, new DoubleAnimation(LyricOpacity, TimeSpan.FromSeconds(0.5)));
                item.LrcTb.Effect = NomalTextEffect;
                //item.LrcMain.Text = item.Lyric;
            }
            var temp = LrcItems.LastOrDefault(p => p.Time <= ms);
            if (temp == null || temp == _currentLrc) return;

            reset(_currentLrc);
            _currentLrc = temp;
            OnNextLrcReached?.Invoke(GetCurrentLrc());

            //next lyric 
            if (IsVisible)
                RefreshCurrentLrcStyle();
        }

        private void RefreshCurrentLrcStyle()
        {
            if (_currentLrc == null) return;
            var container = _currentLrc.LrcTb!;
            //container.Foreground = HighlightLrcColor;
            container.FontWeight = FontWeights.Bold;
            container.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.3)));
            container.Effect = Hightlighter;

            double targetFontsize = LyricFontSize + 10;
            var mainLine = _currentLrc.LrcMain!;
            mainLine.TextWrapping = TextWrapping.NoWrap;
            mainLine.Text = InsertLineBreaks(mainLine, _currentLrc.Lyric, targetFontsize, ActualWidth - LyricMargin.Left - LyricMargin.Right - 1);
            var da = new DoubleAnimation(targetFontsize, TimeSpan.FromSeconds(0.4))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }
            };
            Timeline.SetDesiredFrameRate(da, 60);
            ResetLrcviewScroll();
            container.BeginAnimation(FontSizeProperty, da);

            int index = LrcItems.IndexOf(_currentLrc);
            if (index < LrcItems.Count - 1)
            {
                var next = LrcItems[index + 1];
                next.LrcTb!.Effect = AroundTextEffect;
            }
        }

        private void ResetLrcviewScroll()
        {
            try
            {
                if (_currentLrc == null || _currentLrc.LrcTb == null) return;
                GeneralTransform gf = _currentLrc.LrcTb.TransformToVisual(LyricPanel);
                Point p = gf.Transform(new Point(0, 0));
                double os = p.Y - (scrollviewer.ActualHeight / 2) + 120;
                var da = new DoubleAnimation(os, TimeSpan.FromMilliseconds(500));
                da.EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut };
                Timeline.SetDesiredFrameRate(da, 60);
                scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, da);
            }
            catch { }
        }

    }
}
