using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LemonApp.Views.UserControls;

/// <summary>
/// LyricLineControl.xaml 的交互逻辑
/// </summary>
public partial class LyricLineControl : UserControl
{
    private readonly Dictionary<ISyllableInfo, HighlightTextBlock> mainSyllableLrcs = [];
    private readonly Dictionary<ISyllableInfo, TextBlock> romajiSyllableLrcs = [];
    private const int InActiveLrcBlurRadius = 6;
    private int EmphasisThreshold = 1800; // 高亮抬起分词的阈值ms,在LoadMainLrc时会重新计算
    private int ActiveLrcLiftupHeight => (int)(-FontSize / 4);
    private double AverageWordDuration = 0.0;
    public SyllableLineInfo? RomajiSyllables { get; private set; }
    public ILineInfo? MainLineInfo { get; private set; }
    public Dictionary<ISyllableInfo, HighlightTextBlock> MainSyllableLrcs => mainSyllableLrcs;

    private bool _isPlainLrc = false;

    //reserved for desktop lyric view
    public LyricLineControl()
    {
        InitializeComponent();
        //no blur effect
    }

    public LyricLineControl(SyllableLineInfo info)
    {
        InitializeComponent();
        MainLineInfo = info;
        Effect = new BlurEffect() { Radius = InActiveLrcBlurRadius };
        LoadMainLrc(info.Syllables);
    }

    public LyricLineControl(LineInfo info)
    {
        InitializeComponent();
        MainLineInfo = info;
        Effect = new BlurEffect() { Radius = InActiveLrcBlurRadius };
        _isPlainLrc = true;
        LoadPlainLrc(info.Text);
    }

    public void ClearAll()
    {
        MainLrcContainer.Children.Clear();
        RomajiLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        romajiSyllableLrcs.Clear();
        mainSyllableAnimated.Clear();
        TranslationLrc.Text = string.Empty;
    }
    public void LoadPlainLrc(string lrc, double fontSize = 22)
    {
        MainLrcContainer.Children.Clear();
        var tb = new HighlightTextBlock()
        {
            Text = lrc,
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize,
            HighlightPos = -0.5
        };
        //tb.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
        MainLrcContainer.Children.Add(tb);
    }

    public void LoadPlainRomaji(string? romaji)
    {
        if (romaji == null) return;
        RomajiLrcContainer.Children.Clear();
        RomajiLrcContainer.Children.Add(new TextBlock()
        {
            Text = romaji,
            TextWrapping = TextWrapping.Wrap
        });
    }

    public void LoadMainLrc(List<ISyllableInfo> words, double fontSize = 22)
    {
        MainLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        ClearHighlighter();

        //计算EmphasisThreshold
        var aver = AverageWordDuration = words.Average(w => w.Duration);
        if (aver > 0)
        {
            EmphasisThreshold = (int)(aver * 2);
            if (EmphasisThreshold < 1800) EmphasisThreshold = 1800;
        }
        else
        {
            EmphasisThreshold = 1800; //默认值
        }

        foreach (var word in words)
        {
            bool isSpiltEnabled = word.Duration >= EmphasisThreshold && word.Text.Length > 1;
            var textBlock = new HighlightTextBlock(isSpiltEnabled)
            {
                Text = word.Text,
                FontSize = fontSize,
                HighlightPos = -0.5,
                HighlightWidth = 0.5,
                RenderTransform = isSpiltEnabled ? null : new TranslateTransform()
            };
            textBlock.SetResourceReference(HighlightTextBlock.HighlightColorProperty, "ActiveLrcForegroundColor");

            MainLrcContainer.Children.Add(textBlock);
            mainSyllableLrcs[word] = textBlock;
        }
    }

    public void LoadRomajiLrc(SyllableLineInfo words)
    {
        RomajiLrcContainer.Children.Clear();
        romajiSyllableLrcs.Clear();
        RomajiSyllables = words;
        foreach (var word in words.Syllables)
        {
            var textBlock = new TextBlock
            {
                Text = word.Text,
                TextTrimming = TextTrimming.None
            };
            RomajiLrcContainer.Children.Add(textBlock);
            romajiSyllableLrcs[word] = textBlock;
        }
    }

    private readonly Dictionary<ISyllableInfo, bool> mainSyllableAnimated = new();

    public void UpdateTime(int ms)
    {
        if (_isPlainLrc)
        {
            //do nothing 
        }
        else
        {
            foreach (var kvp in mainSyllableLrcs)
            {
                var syllable = kvp.Key;
                var textBlock = kvp.Value;

                if (syllable.EndTime < ms)
                {
                    // 已经过了，直接填满
                    textBlock.HighlightPos = 1;
                    mainSyllableAnimated[syllable] = true;
                    //lift-up: 如果动画正在执行则不强制设置，确保歌词流畅性
                    if (textBlock.RenderTransform is TranslateTransform trans && !trans.HasAnimatedProperties)
                    {
                        trans.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, ActiveLrcLiftupHeight,
                                    TimeSpan.FromMilliseconds(AverageWordDuration * 1.5))
                        {
                            EasingFunction = new ExponentialEase()
                        });
                    }
                }
                else if (syllable.StartTime > ms)
                {
                    // 还没到，保持未填充
                    textBlock.BeginAnimation(HighlightTextBlock.HighlightPosProperty, null);
                    textBlock.HighlightPos = -0.5;
                    mainSyllableAnimated[syllable] = false;

                    //clear lift-up
                    if (textBlock.RenderTransform is TranslateTransform trans)
                    {
                        trans.BeginAnimation(TranslateTransform.YProperty, null);
                        trans.Y = 0;
                    }
                    if (syllable.Duration >= EmphasisThreshold && textBlock.Geometries is { } geo)
                    {
                        foreach (var g in geo)
                        {
                            g.Transform = null;
                        }
                    }
                }
                else
                {
                    // 正在进行，判断是否需要启动动画
                    if (!mainSyllableAnimated.TryGetValue(syllable, out var animated) || !animated)
                    {
                        mainSyllableAnimated[syllable] = true;

                        var liftupAni = new DoubleAnimation(0, ActiveLrcLiftupHeight,
                                    TimeSpan.FromMilliseconds(AverageWordDuration * 1.5))
                        {
                            EasingFunction = new ExponentialEase()
                        };

                        // HighlightPos animation: from -0.5 to 1
                        var duration = TimeSpan.FromMilliseconds(syllable.Duration);
                        var highlightAnim = new DoubleAnimation(-0.5, 1, new Duration(duration));
                        textBlock.BeginAnimation(HighlightTextBlock.HighlightPosProperty, highlightAnim);

                        bool liftup = true;
                        if (syllable.Duration >= EmphasisThreshold && textBlock.Geometries is { } geo)
                        {
                            double delay = 0, unit = syllable.Duration * 1.2 / geo.Length;
                            double p = geo.Length * 0.6, addi = unit - (p - 1) * unit / (geo.Length - 1);
                            foreach (var g in geo)
                            {
                                var transfrom = new TranslateTransform();
                                g.Transform = transfrom;
                                transfrom.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, ActiveLrcLiftupHeight, TimeSpan.FromMilliseconds(unit * p))
                                {
                                    BeginTime = TimeSpan.FromMilliseconds(delay),
                                    EasingFunction = new BackEase()
                                });
                                delay += addi;
                            }
                            liftup = false;
                        }

                        //lift-up animation
                        if (liftup && textBlock.RenderTransform is TranslateTransform trans)
                        {
                            trans.BeginAnimation(TranslateTransform.YProperty, liftupAni);
                        }
                    }
                }
            }

            // Romaji歌词 颜色渐变动画
            foreach (var kvp in romajiSyllableLrcs)
            {
                var syllable = kvp.Key;
                var textBlock = kvp.Value;
                if ((syllable.StartTime) <= ms && (syllable.EndTime + 200) >= ms)
                {
                    //textBlock.SetResourceReference(ForegroundProperty, "HighlightThemeColor");
                    if (textBlock.Tag is not true)
                    {
                        var fontColor = ((SolidColorBrush)FindResource("InActiveLrcForeground")).Color;
                        var highlightColor = ((SolidColorBrush)FindResource("HighlightThemeColor")).Color;
                        var brush = new SolidColorBrush();
                        textBlock.Foreground = brush;
                        var ani = new ColorAnimationUsingKeyFrames
                        {
                            KeyFrames =
                            [
                                new EasingColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))),
                                new EasingColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration))),
                                new EasingColorKeyFrame(fontColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration+1000)))
                            ]
                        };
                        brush.BeginAnimation(SolidColorBrush.ColorProperty, ani);
                        textBlock.Tag = true;
                    }
                }
                else
                {
                    textBlock.Tag = false;
                }
            }
        }
    }

    public void ClearHighlighter(bool animated = false)
    {
        mainSyllableAnimated.Clear();
        foreach (var lrc in mainSyllableLrcs)
        {
            // Reset HighlightPos to initial state
            lrc.Value.BeginAnimation(HighlightTextBlock.HighlightPosProperty, null);

            //fade animation
            if (animated)
            {
                lrc.Value.HighlightPos = 1;
                lrc.Value.BeginAnimation(HighlightTextBlock.HighlightIntensityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));
            }
            else lrc.Value.HighlightPos = -0.5;

            //clear lift-up
            var clearAnimation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
            if (lrc.Value.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.YProperty, clearAnimation);
            }

            if (lrc.Value.Geometries is { } geo)
            {
                foreach (var g in geo)
                {
                    if (g.Transform is TranslateTransform tg)
                    {
                        tg.BeginAnimation(TranslateTransform.YProperty, clearAnimation);
                    }
                }
            }
        }
    }

    public void SetActiveState(bool isActive, bool inactiveAnimated = true)
    {
        var control = this;
        if (isActive)
        {   //Active
            var blur = new BlurEffect() { Radius = InActiveLrcBlurRadius };
            control.Effect = blur;
            var da = new DoubleAnimation(InActiveLrcBlurRadius, 0, TimeSpan.FromMilliseconds(300));
            blur.BeginAnimation(BlurEffect.RadiusProperty, da);

            if (control._isPlainLrc)
            {
                if (control.MainLrcContainer.Children[0] is HighlightTextBlock tb)
                {
                    tb.HighlightPos = inactiveAnimated ? 1 : -0.5;
                }
            }
            else
            {
                // Reset HighlightPos to initial state for all syllables
                foreach (var lrc in control.mainSyllableLrcs)
                {
                    lrc.Value.HighlightPos = -0.5;
                    lrc.Value.BeginAnimation(HighlightTextBlock.HighlightIntensityProperty, null);
                }
            }
        }
        else
        {
            var blur = new BlurEffect() { Radius = 0 };
            control.Effect = blur;
            blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(0, InActiveLrcBlurRadius, TimeSpan.FromMilliseconds(300)));
            if (control._isPlainLrc)
            {
                if (control.MainLrcContainer.Children[0] is HighlightTextBlock tb)
                {
                    tb.HighlightPos = -0.5;
                }
            }
            else control.ClearHighlighter(inactiveAnimated);
        }
    }



    public SolidColorBrush CustomHighlightColorBrush
    {
        get { return (SolidColorBrush)GetValue(CustomHighlightColorBrushProperty); }
        set { SetValue(CustomHighlightColorBrushProperty, value); }
    }

    public static readonly DependencyProperty CustomHighlightColorBrushProperty =
        DependencyProperty.Register(nameof(CustomHighlightColorBrush), typeof(SolidColorBrush), typeof(LyricLineControl), new PropertyMetadata(null, OnCustomHighlightColorBrushChanged));

    private static void OnCustomHighlightColorBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricLineControl control && e.NewValue is SolidColorBrush brush)
        {
            control.MainLrcContainer.Resources["ActiveLrcForegroundColor"] = brush.Color;
        }
    }

    public bool IsCurrent
    {
        get { return (bool)GetValue(IsCurrentProperty); }
        set { SetValue(IsCurrentProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsCurrent.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsCurrentProperty =
        DependencyProperty.Register("IsCurrent", typeof(bool), typeof(LyricLineControl), new PropertyMetadata(false, OnIsCurrentChanged));

    private static void OnIsCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LyricLineControl control)
        {
            control.SetActiveState((bool)e.NewValue);
        }
    }
}