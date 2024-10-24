using EleCho.WpfSuite;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
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
        public LyricView(IHttpClientFactory httpClientFactory,UserProfileService userProfileService)
        {
            InitializeComponent();
            UpdateColorMode();

            _hc = httpClientFactory.CreateClient(App.PublicClientFlag);
            _auth=userProfileService.GetAuth();

            SizeChanged += LyricView_SizeChanged;
            IsVisibleChanged += LyricView_IsVisibleChanged;
        }

        private void LyricView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is true)
            {
                RefreshCurrentLrcStyle();
            }
        }

        private void LyricView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshCurrentLrcStyle();
        }
        #region Apperance
        public void UpdateColorMode()
        {
            if(Foreground is SolidColorBrush color)
            {
                Hightlighter = new DropShadowEffect() { BlurRadius = 20, Color = color.Color, Opacity = 0.5, ShadowDepth = 0, Direction = 0 };
            }
        }

        private  Thickness LyricMargin = new Thickness(20,12,0,12);
        /// <summary>
        /// 非高亮歌词的透明度
        /// </summary>
        private const double LyricOpacity = 0.8;
        /// <summary>
        /// 高亮歌词效果
        /// </summary>
        public Effect? Hightlighter;
        public Effect? NomalTextEffect = new BlurEffect() { Radius = 5 };
        /// <summary>
        /// 歌词的文本对齐方式
        /// </summary>
        public TextAlignment TextAlignment = TextAlignment.Left;

        public double LyricFontSize = 22;
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
                        Dictionary<double, string?>? trans = null;
                        List<string>? romaji = null;
                        if (data.Trans != null)
                        {
                             trans = LyricHelper.Format(data.Trans);
                            //TODO:重新考虑romaji翻译的加载和储存方式，不要让其影响主要部件加载
                           /* if (LyricHelper.IsJapanese(data.Lyric))
                            {
                                StringBuilder sb = new();
                                foreach(var line in lyrics)
                                    sb.AppendLine(line.Value.Contains('：')?"":line.Value);
                                var temp = sb.ToString();
                                romaji =await RomajiLyric.Trans(_hc,temp);
                            }*/
                        }
                        int i = 0;
                        foreach(var line in lyrics)
                        {
                            var transText = trans?.FirstOrDefault(m => m.Key >= line.Key - 2).Value;
                            LrcLine lrcLine = new() {
                                Time = line.Key,
                                Lyric = line.Value!,
                                Trans=transText,
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
            scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
            scrollviewer.ScrollToTop();
            LyricPanel.Children.Clear();
            LrcItems.Clear();
            //占位
            LyricPanel.Children.Add(new Border() { Height = 200,Background=Brushes.Transparent });
            foreach (var line in data.LyricData)
            {
                LrcItem item = new();
                item.Time = line.Time;
                TextBlock tb = new()
                {
                    FontSize = LyricFontSize,
                    //Foreground = NormalLrcColor,
                    TextAlignment = TextAlignment,
                    Opacity = LyricOpacity,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.None,
                    Margin= LyricMargin,
                    Effect= NomalTextEffect
                };
                item.LrcTb = tb;
                if (line.Romaji != null)
                {
                    TextBlock romaji = new()
                    {
                        Text = line.Romaji,
                        FontSize = LyricFontSize-5,
                        FontWeight = FontWeights.Regular,
                        //Foreground = NormalLrcColor,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None
                    };
                    item.Romaji = romaji;
                    tb.Inlines.Add(romaji);
                    tb.Inlines.Add(new LineBreak());
                }
                TextBlock lyric = new()
                {
                    Text = line.Lyric,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.None
                };
                item.LrcMain = lyric;
                item.Lyric = line.Lyric;
                tb.Inlines.Add(lyric);
                if (line.Trans != null)
                {
                    TextBlock trans = new() {
                        FontWeight = FontWeights.Regular,
                        Text=line.Trans,
                        Opacity=0.5,
                        FontSize = LyricFontSize - 6,
                        //Foreground = NormalLrcColor,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.None
                    };
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
        private static string InsertLineBreaks(TextBlock tb,string preText, double fontSize, double maxWidth)
        {
            var typeface = new Typeface(tb.FontFamily, tb.FontStyle, FontWeights.Bold, tb.FontStretch);
            pixelsPerDip ??= VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip;
            double GetWidth(string text)
            {
                var formattedLine = new FormattedText(text, CultureInfo.CurrentCulture,
                                                                                    FlowDirection.LeftToRight,typeface,fontSize,Brushes.Black,pixelsPerDip!.Value);
                return formattedLine.WidthIncludingTrailingWhitespace;
            }
            StringBuilder temp = new();
            string[] blocks;
            bool spaceSplit = preText.Contains(' ');
            if (spaceSplit)
                blocks = preText.Split(' ');
            else blocks = preText.Select(c=>c.ToString()).ToArray();


            foreach(var block in blocks)
            {
                if(string.IsNullOrWhiteSpace(block)) continue;
                temp.Append(block);
                if(spaceSplit)temp.Append(' ');
                if (GetWidth(temp.ToString()) > maxWidth)
                {
                    int undoLength=block.Length+(spaceSplit? 1:0);
                    temp.Remove(temp.Length-undoLength,undoLength);
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
                if(item==null|| item.LrcTb == null) return;
                //item.LrcTb.Foreground = NormalLrcColor;
                item.LrcTb.FontWeight = FontWeights.Regular;
                item.LrcTb.BeginAnimation(FontSizeProperty, null);
                item.LrcTb.Opacity = LyricOpacity;
                item.LrcTb.Effect = NomalTextEffect;

                item.LrcMain!.Text=item.Lyric;
                item.LrcMain!.TextWrapping= TextWrapping.Wrap;
            }
            var temp = LrcItems.LastOrDefault(p => p.Time <= ms);
            if (temp == null || temp == _currentLrc) return;

            reset(_currentLrc);
            _currentLrc = temp;
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
            container.Opacity = 1;
            container.Effect = Hightlighter;

            double targetFontsize = LyricFontSize + 8;
            var mainLine = _currentLrc.LrcMain!;
            mainLine.TextWrapping = TextWrapping.NoWrap;
            mainLine.Text = InsertLineBreaks(mainLine,_currentLrc.Lyric, targetFontsize, ActualWidth - LyricMargin.Left - LyricMargin.Right - 1);
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
