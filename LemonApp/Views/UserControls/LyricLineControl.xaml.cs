using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
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
    private static double InitialOpacity = 0.4, ActiveOpacity = 1;
    private const int EmphasisThreshold = 1800;//TODO: [优化] 使用句中词的平均时长来计算
    private readonly Dictionary<ISyllableInfo, TextBlock> mainSyllableLrcs = [], romajiSyllableLrcs = [];
    private Effect? _normalLrcEffect = new BlurEffect() { Radius = 4 };
    public SyllableLineInfo? RomajiSyllables { get; private set; }

    public LyricLineControl()
    {
        InitializeComponent();
    }

    public LyricLineControl(List<ISyllableInfo> words)
    {
        InitializeComponent();
        Opacity = InitialOpacity;
        Effect = _normalLrcEffect;
        LoadMainLrc(words);
    }

    public void LoadMainLrc(List<ISyllableInfo> words,double fontSize=22)
    {
        MainLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        ClearHighlighter();
        foreach (var word in words)
        {
            var textBlock = new TextBlock
            {
                Text = word.Text,
                TextTrimming=TextTrimming.None,
                FontSize = fontSize
            };
            //高亮抬起词
            if (word.Duration >= EmphasisThreshold)
            {
                if (word.Text.Length > 1)
                {
                    textBlock.Text = null;
                    //拆分每个字符
                    foreach (char c in word.Text)
                    {
                        textBlock.Inlines.Add(new TextBlock()
                        {
                            Text = c.ToString(),
                            RenderTransform = new TranslateTransform()
                        });
                    }
                }
                else
                {
                    textBlock.RenderTransform = new TranslateTransform();
                }
            }
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

    private readonly Dictionary<ISyllableInfo, LinearGradientBrush> mainSyllableBrushes = new();
    private readonly Dictionary<ISyllableInfo, bool> mainSyllableAnimated = new();

    public void UpdateTime(int ms)
    {
        foreach (var kvp in mainSyllableLrcs)
        {
            var syllable = kvp.Key;
            var textBlock = kvp.Value;

            if (syllable.EndTime < ms)
            {
                // 已经过了，直接填满
                EnsureBrush(textBlock, syllable, 1.0);
                mainSyllableAnimated[syllable] = true;
            }
            else if (syllable.StartTime > ms)
            {
                // 还没到，保持未填充
                EnsureBrush(textBlock, syllable, 0.0);
                mainSyllableAnimated[syllable] = false;

                //如果是高亮抬起分词，则可能需要先清除效果
                if (syllable.Duration >= EmphasisThreshold&& textBlock.Inlines.Count>1)
                {
                    var empty= CreateBrush(0.0);
                    foreach (var line in textBlock.Inlines)
                    {
                        if(line is InlineUIContainer con &&con.Child is TextBlock block)
                        {
                            block.Foreground = empty;
                        }
                    }
                }
            }
            else
            {
                // 正在进行，判断是否需要启动动画
                if (!mainSyllableAnimated.TryGetValue(syllable, out var animated) || !animated)
                {
                    mainSyllableAnimated[syllable] = true;

                    bool animate = true;
                    if (syllable.Duration >= EmphasisThreshold)
                    {
                        //highlight
                       var lighter = new DropShadowEffect() { BlurRadius = 20,Color = Colors.White, Direction = 0, ShadowDepth = 0 };
                        textBlock.Effect = lighter;
                        lighter.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(syllable.Duration*0.8)));

                       var easing = new CubicEase();
                        var up = -6;
                        //向上抬起又落下的动画(整体)
                        if (textBlock.Inlines.Count <=1)
                        {
                            var upAni = new DoubleAnimationUsingKeyFrames()
                            {
                                KeyFrames = [
                                    new EasingDoubleKeyFrame(up, TimeSpan.FromMilliseconds(1500)){
                                        EasingFunction=easing
                                    },
                                    new EasingDoubleKeyFrame(up, TimeSpan.FromMilliseconds(syllable.Duration)),
                                    new EasingDoubleKeyFrame(default, TimeSpan.FromMilliseconds(syllable.Duration+400)){
                                        EasingFunction=easing
                                    }],
                            };
                            upAni.Completed += delegate {
                                var da = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
                                da.Completed += delegate { textBlock.Effect = null; };
                                lighter.BeginAnimation(DropShadowEffect.OpacityProperty, da);
                            };
                            textBlock.RenderTransform.BeginAnimation(TranslateTransform.YProperty, upAni);
                        }
                        else
                        {
                            //单独设置动画
                            animate = false;
                            int index = 0;
                            foreach (InlineUIContainer line in textBlock.Inlines)
                            {
                                if (line.Child.RenderTransform is TranslateTransform ts)
                                {
                                    double begin = 100 * index;
                                    var upAni = new DoubleAnimationUsingKeyFrames()
                                    {
                                        KeyFrames = [
                                            new EasingDoubleKeyFrame(default, TimeSpan.FromMilliseconds(begin)),
                                            new EasingDoubleKeyFrame(up, TimeSpan.FromMilliseconds((double)syllable.Duration*(double)(index+1)/(double)textBlock.Inlines.Count)){
                                                EasingFunction=easing
                                            },
                                            new EasingDoubleKeyFrame(up, TimeSpan.FromMilliseconds(syllable.Duration)),
                                            new EasingDoubleKeyFrame(default, TimeSpan.FromMilliseconds(syllable.Duration+400)){
                                                EasingFunction=easing
                                            }],
                                    };
                                    upAni.Completed += delegate {
                                        var da = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
                                        da.Completed += delegate { textBlock.Effect = null; };
                                        lighter.BeginAnimation(DropShadowEffect.OpacityProperty, da);
                                    };
                                    ts.BeginAnimation(TranslateTransform.YProperty, upAni);
                                }
                                if(line.Child is TextBlock block)
                                {
                                    var single = CreateBrush(0);
                                    block.Foreground = single;
                                    double begin = syllable.Duration/ textBlock.Inlines.Count * index;
                                    var ani = new DoubleAnimationUsingKeyFrames
                                    {
                                        KeyFrames =
                                        [
                                            new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(begin*0.8)),
                                            new EasingDoubleKeyFrame(1, TimeSpan.FromMilliseconds(syllable.Duration*0.8 * (index + 1) / textBlock.Inlines.Count))
                                        ]
                                    };
                                    var aniDelay = new DoubleAnimationUsingKeyFrames
                                    {
                                        KeyFrames =
                                        [
                                            new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(begin)),
                                            new EasingDoubleKeyFrame(1, TimeSpan.FromMilliseconds(begin+ syllable.Duration/textBlock.Inlines.Count))
                                        ]
                                    };
                                    single.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, aniDelay);
                                    single.GradientStops[2].BeginAnimation(GradientStop.OffsetProperty, ani);
                                }
                                index++;
                            }
                        }
                    }

                    if (animate)
                    {
                        var brush = EnsureBrush(textBlock, syllable, 0.0);
                        var duration = TimeSpan.FromMilliseconds(syllable.Duration);
                        var anim = new DoubleAnimation(0.0, 1.0, new Duration(duration*0.8));
                        var animDelay = new DoubleAnimation(0.0, 1.0, new Duration(duration));
                        brush.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, animDelay);
                        brush.GradientStops[2].BeginAnimation(GradientStop.OffsetProperty, anim);
                    }
                }
            }
        }

        // Romaji歌词 颜色渐变动画
        foreach (var kvp in romajiSyllableLrcs)
        {
            var syllable = kvp.Key;
            var textBlock = kvp.Value;
            if (syllable.StartTime <= ms && syllable.EndTime >= ms)
            {
                //textBlock.SetResourceReference(ForegroundProperty, "HighlightThemeColor");
                if(textBlock.Tag is not true)
                {
                    var fontColor = ((SolidColorBrush)FindResource("ForeColor")).Color;
                    var highlightColor = ((SolidColorBrush)FindResource("HighlightThemeColor")).Color;
                    var brush = new SolidColorBrush();
                    textBlock.Foreground = brush;
                    var ani=new ColorAnimationUsingKeyFrames
                    {
                        KeyFrames =
                        [
                            new EasingColorKeyFrame(fontColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))),
                            new EasingColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration/2))),
                            new EasingColorKeyFrame(fontColor, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(syllable.Duration+2000)))
                        ]
                    };
                    brush.BeginAnimation(SolidColorBrush.ColorProperty,ani);
                    textBlock.Tag = true;
                }
            }
            else
            {
                textBlock.Tag = false;
            }
        }
    }

    // 创建或获取渐变画刷，并设置初始进度
    private LinearGradientBrush EnsureBrush(TextBlock textBlock, ISyllableInfo syllable, double progress)
    {
        if (!mainSyllableBrushes.TryGetValue(syllable, out var brush))
        {
            brush = CreateBrush(progress);
            mainSyllableBrushes[syllable] = brush;
            textBlock.Foreground = brush;
        }
        else
        {
            brush.GradientStops[1].Offset = progress;
            brush.GradientStops[2].Offset = progress;
            textBlock.Foreground = brush;
        }
        return brush;
    }


    public SolidColorBrush CustomHighlighterColor
    {
        get { return (SolidColorBrush)GetValue(CustomHighlighterColorProperty); }
        set { SetValue(CustomHighlighterColorProperty, value); }
    }

    public static readonly DependencyProperty CustomHighlighterColorProperty =
        DependencyProperty.Register("CustomHighlighterColor", 
            typeof(SolidColorBrush), typeof(LyricLineControl), 
            new PropertyMetadata(null));



    public SolidColorBrush CustomNormalColor
    {
        get { return (SolidColorBrush)GetValue(CustomNormalColorProperty); }
        set { SetValue(CustomNormalColorProperty, value); }
    }

    public static readonly DependencyProperty CustomNormalColorProperty =
        DependencyProperty.Register("CustomNormalColor",
            typeof(SolidColorBrush), typeof(LyricLineControl),
            new PropertyMetadata(null));



    public double TranslationLrcOpacity
    {
        get => TranslationLrc.Opacity;
        set => TranslationLrc.Opacity = value;
    }


    private LinearGradientBrush CreateBrush(double progress)
    {
        var fontColor = ((SolidColorBrush)FindResource("ForeColor")).Color;
        var highlightColor =CustomHighlighterColor?.Color ?? fontColor;
        var normalColor = CustomNormalColor?.Color ?? new Color { R = fontColor.R, G = fontColor.G, B = fontColor.B, A = 72 };
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5),
            EndPoint = new Point(1, 0.5),
            GradientStops =
            [
                    new GradientStop(highlightColor, 0),
                    new GradientStop(highlightColor, progress),
                    new GradientStop(normalColor, progress),
                    new GradientStop(normalColor, 1)
            ]
        };
    }

    public void ClearHighlighter()
    {
        mainSyllableBrushes.Clear();
        mainSyllableAnimated.Clear();
        foreach (var lrc in mainSyllableLrcs)
        {
            lrc.Value.SetResourceReference(ForegroundProperty, "ForeColor");
            if (lrc.Value.Inlines.Count > 1)
            {
                foreach(var line in lrc.Value.Inlines)
                {
                    if(line is InlineUIContainer con&&con.Child is TextBlock block)
                    {
                        block.Foreground = null;
                        block.SetResourceReference(ForegroundProperty, "ForeColor");
                    }
                }
            }
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
            if ((bool)e.NewValue)
            {
                control.Effect = null;
                foreach(var lrc in control.mainSyllableLrcs)
                {
                    control.EnsureBrush(lrc.Value, lrc.Key, 0);
                    if (lrc.Key.Duration >= EmphasisThreshold&& lrc.Value.Inlines.Count>1)
                    {
                        var empty = control.CreateBrush(0.0);
                        foreach (var line in lrc.Value.Inlines)
                        {
                            if (line is InlineUIContainer con && con.Child is TextBlock block)
                            {
                                block.Foreground = empty;
                            }
                        }
                    }
                }
                control.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    From = InitialOpacity,
                    To = ActiveOpacity,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            }
            else
            {
                control.Effect= control._normalLrcEffect;
                control.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To = InitialOpacity,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
                control.ClearHighlighter();
            }
        }

    }
}
