using EleCho.WpfSuite;
using EleCho.WpfSuite.Controls;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Lyric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using static LemonApp.MusicLib.Abstraction.Lyric.DataTypes;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Views.UserControls
{
    internal class LrcItem
    {
        public double Time { get; set; }
        public string Lyric { get; set; } = string.Empty;
        public TextBlock? LrcTb { get; set; }
        public TextBlock? LrcMain { get; set; }
        public TextBlock? LrcTrans { get; set; }
        public TextBlock? Romaji { get; set; }
    }

    /// <summary>
    /// LyricView.xaml 的交互逻辑
    /// </summary>
    public partial class LyricView : UserControl
    {
        public LyricView()
        {
            InitializeComponent();
        }
        #region Apperance
        /// <summary>
        /// 非高亮歌词的透明度
        /// </summary>
        private const double LyricOpacity = 0.8;
        /// <summary>
        /// 高亮歌词效果
        /// </summary>
        public Effect Hightlighter = new DropShadowEffect() { BlurRadius = 20, Color = Colors.White, Opacity = 0.5, ShadowDepth = 0, Direction = 0 };
        public Effect? NomalTextEffect = null;
        /// <summary>
        /// 非高亮歌词的颜色
        /// </summary>
        public SolidColorBrush NormalLrcColor= Brushes.White;
        public SolidColorBrush HighlightLrcColor = Brushes.White;
        /// <summary>
        /// 歌词的文本对齐方式
        /// </summary>
        public TextAlignment TextAlignment = TextAlignment.Left;

        public double LyricFontSize = 20;
        #endregion
        public void Init(HttpClient hc,TencUserAuth auth)
        {
            _hc  ??= hc;
            _auth ??= auth;
        }
        private HttpClient? _hc;
        private TencUserAuth? _auth;
        private List<LrcItem> LrcItems = [];
        private LrcItem? _currentLrc = null;
        public async Task LoadFromMusic(Music m)
        {
            var path = CacheManager.GetCachePath(CacheManager.CacheType.Lyric);
            path = System.IO.Path.Combine(path, m.MusicID + ".json");
            if (await Settings.LoadFromJsonAsync<LocalLyricData>(path) is { } local)
            {
                LoadLrc(local);
            }
            else
            {
                if (_hc == null || _auth == null) return;
                if (m.Source == Platform.qq)
                {
                    var data=await TencGetLyric.GetLyricDataAsync(_hc,_auth, m.MusicID);
                    if(data != null&&data.Lyric!=null)
                    {
                        LocalLyricData ly=new();
                        ly.Id= m.MusicID;
                        var lyrics = LyricHelper.Format(data.Lyric);
                        Dictionary<double, string>? trans = null;
                        List<string>? romaji = null;
                        if (data.Trans != null)
                        {
                             trans = LyricHelper.Format(data.Trans);
                            if (LyricHelper.IsJapanese(data.Lyric))
                            {
                                StringBuilder sb = new();
                                foreach(var line in lyrics)
                                    sb.AppendLine(line.Value);
                                 romaji =await RomajiLyric.Trans(_hc, sb.ToString());
                            }
                        }
                        int i = 0;
                        foreach(var line in lyrics)
                        {
                            LrcLine lrcLine = new() {
                                Time = line.Key,
                                Lyric = line.Value,
                                Trans=trans?.First(p=>p.Key<=line.Key).Value,
                                Romaji=romaji?[i]
                            };
                            i++;
                            ly.LyricData.Add(lrcLine);
                        }
                        await Settings.SaveAsJsonAsync(ly, path);
                        LoadLrc(ly);
                    }
                }
            }
        }
        private void LoadLrc(LocalLyricData data)
        {
            foreach(var line in data.LyricData)
            {
                LrcItem item = new();
                item.Time = line.Time;
                TextBlock tb = new()
                {
                    FontSize = LyricFontSize,
                    Foreground = NormalLrcColor,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment,
                    Opacity = LyricOpacity,
                    TextTrimming = TextTrimming.None,
                };
                item.LrcTb = tb;
                if (line.Romaji != null)
                {
                    TextBlock romaji = new()
                    {
                        Text = line.Romaji+"\r\n",
                        FontSize = LyricFontSize-5,
                        FontWeight = FontWeights.Regular,
                        Foreground = NormalLrcColor
                    };
                    item.Romaji = romaji;
                    tb.Inlines.Add(romaji);
                }
                TextBlock lyric = new()
                {
                    Text = line.Lyric
                };
                item.LrcMain = lyric;
                item.Lyric = line.Lyric;
                tb.Inlines.Add(lyric);
                if (line.Trans != null)
                {
                    TextBlock trans = new() {
                        FontWeight = FontWeights.Regular,
                        Text="\r\n"+line.Trans,
                        FontSize = LyricFontSize - 6,
                        Foreground = NormalLrcColor
                    };
                    item.LrcTrans = trans;
                    tb.Inlines.Add(trans);
                }
                LrcItems.Add(item);
                LyricPanel.Children.Add(tb);
            }
        }
        private static double pixelsPerDip = VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
        public string InsertLineBreaks(TextBlock tb, double fontSize, double maxWidth)
        {
            string result = string.Empty;
            string line = string.Empty;
            string text = tb.Text;
            bool hasBlank = text.Contains(' ');
            var list = hasBlank ? text.Split(' ') : text.Split();
            foreach (var word in list)
            {
                var typeface = new Typeface(tb.FontFamily, tb.FontStyle, FontWeights.Bold, tb.FontStretch);
                var formattedLine = new FormattedText(line + " " + word,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    Brushes.Black,
                    pixelsPerDip);

                if (formattedLine.WidthIncludingTrailingWhitespace > maxWidth)
                {
                    result += line + "\n";
                    line = hasBlank ? word + " " : word;
                }
                else
                {
                    line += hasBlank ? word + " " : word;
                }
            }

            result += line;
            //去除result的第一个换行符
            if (result.StartsWith("\n"))
            {
                result = result.Substring(1);
            }
            return result;
        }

        public void UpdateTime(double ms)
        {
            void reset(LrcItem? item)
            {
                if(item==null|| item.LrcTb == null) return;
                item.LrcTb.Foreground = NormalLrcColor;
                item.LrcTb.FontWeight = FontWeights.Regular;
                item.LrcTb.BeginAnimation(FontSizeProperty, null);
                item.LrcTb.Opacity = LyricOpacity;
                item.LrcTb.Effect = NomalTextEffect;

                item.LrcMain!.Text=item.Lyric;
                item.LrcMain!.TextWrapping= TextWrapping.Wrap;
            }
            reset(_currentLrc);
            _currentLrc = LrcItems.FirstOrDefault(p => p.Time <= ms);
            if (_currentLrc == null) return;

            var container = _currentLrc.LrcTb!;
            container.Foreground = HighlightLrcColor;
            container.FontWeight = FontWeights.Bold;
            container.Opacity = 1;
            container.Effect = Hightlighter;

            double targetFontsize = LyricFontSize + 8;
            var mainLine = _currentLrc.LrcMain!;
            mainLine.TextWrapping = TextWrapping.NoWrap;
            mainLine.Text = InsertLineBreaks(mainLine, targetFontsize, mainLine.ActualWidth);
            var da = new DoubleAnimation(targetFontsize, TimeSpan.FromSeconds(0.3))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            Timeline.SetDesiredFrameRate(da, 60);
            ResetLrcviewScroll();
            container.BeginAnimation(FontSizeProperty, da);
        }

        private void ResetLrcviewScroll()
        {
            GeneralTransform gf = _currentLrc!.LrcTb!.TransformToVisual(LyricPanel);
            Point p = gf.Transform(new Point(0, 0));
            double os = p.Y - (scrollviewer.ActualHeight / 2) + 120;
            var da = new DoubleAnimation(os, TimeSpan.FromMilliseconds(500));
            da.EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut };
            Timeline.SetDesiredFrameRate(da, 60);
            scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, da);
        }

    }
}
