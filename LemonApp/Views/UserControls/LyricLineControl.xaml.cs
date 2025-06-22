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
    private int EmphasisThreshold { get; set; }= 1800; // 高亮抬起分词的阈值ms
    private readonly Dictionary<ISyllableInfo, TextBlock> mainSyllableLrcs = [], romajiSyllableLrcs = [];
    private const int InActiveLrcBlurRadius = 6;
    private int ActiveLrcLiftupHeight = -4;
    private double AverageWordDuration = 0.0;
    public SyllableLineInfo? RomajiSyllables { get; private set; }

    public LyricLineControl()
    {
        InitializeComponent();
    }

    public LyricLineControl(List<ISyllableInfo> words)
    {
        InitializeComponent();
        Effect = new BlurEffect() { Radius = InActiveLrcBlurRadius };
        LoadMainLrc(words);
    }

    public void LoadMainLrc(List<ISyllableInfo> words,double fontSize=22)
    {
        MainLrcContainer.Children.Clear();
        mainSyllableLrcs.Clear();
        ClearHighlighter();
        //计算EmphasisThreshold
        var aver = AverageWordDuration = words.Select(w => w.Duration).Average();
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
            var textBlock = new TextBlock
            {
                Text = word.Text,
                TextTrimming=TextTrimming.None,
                FontSize = fontSize
            };

            //高亮抬起词
            if (word.Duration >= EmphasisThreshold && word.Text.Length > 1)
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
                // lift-up animation for all non-highlight lrc
                textBlock.RenderTransform = new TranslateTransform();
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
                //lift-up
                if(textBlock.RenderTransform is TranslateTransform trans)
                {
                    trans.Y = ActiveLrcLiftupHeight;
                }
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
                //clear lift-up
                if (textBlock.RenderTransform is TranslateTransform trans)
                {
                    trans.BeginAnimation(TranslateTransform.YProperty, null);
                    trans.Y = 0;
                }
            }
            else
            {
                // 正在进行，判断是否需要启动动画
                if (!mainSyllableAnimated.TryGetValue(syllable, out var animated) || !animated)
                {
                    mainSyllableAnimated[syllable] = true;

                    var liftupAni = new DoubleAnimation(0, ActiveLrcLiftupHeight,
                                TimeSpan.FromMilliseconds(AverageWordDuration))
                    {
                        EasingFunction = new ExponentialEase()
                    };
                    bool animate = true,liftup=true;
                    if (syllable.Duration >= EmphasisThreshold)
                    {
                        //highlight
                        var fontColor = CustomHighlighterColor?.Color ?? ((SolidColorBrush)FindResource("ForeColor")).Color;
                       var lighter = new DropShadowEffect() { BlurRadius = 20,Color =fontColor, Direction = 0, ShadowDepth = 0 };
                        textBlock.Effect = lighter;
                        lighter.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(syllable.Duration*0.8)));
                        Action hideLighter = delegate {
                            var da = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
                            da.Completed += delegate { textBlock.Effect = null; };
                            lighter.BeginAnimation(DropShadowEffect.OpacityProperty, da);
                        };

                        var easing = new CubicEase();
                        var up = -6;
                        //高亮分词逐字抬起动画
                        if (textBlock.Inlines.Count >1)
                        {
                            //单独设置动画
                            liftup = false;
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
                                    upAni.Completed += (_, _) => hideLighter();
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
                                            new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(begin)),
                                            new EasingDoubleKeyFrame(1, TimeSpan.FromMilliseconds(syllable.Duration * (index + 1) / textBlock.Inlines.Count))
                                        ]
                                    };
                                    var aniDelay = new DoubleAnimationUsingKeyFrames
                                    {
                                        KeyFrames =
                                        [
                                            new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(begin*1.2)),
                                            new EasingDoubleKeyFrame(1, TimeSpan.FromMilliseconds(begin*1.2+ syllable.Duration/textBlock.Inlines.Count))
                                        ]
                                    };
                                    single.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, aniDelay);
                                    single.GradientStops[2].BeginAnimation(GradientStop.OffsetProperty, ani);
                                }
                                index++;
                            }
                        }
                        else if (syllable.Duration > EmphasisThreshold)//高亮分词，但是只有单个字符
                        {
                            liftupAni.Duration = TimeSpan.FromMilliseconds(syllable.Duration);
                            liftupAni.Completed += (_, _) => hideLighter();
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

                        //lift-up animation
                        if (liftup&&textBlock.RenderTransform is TranslateTransform trans)
                        {
                            trans.BeginAnimation(TranslateTransform.YProperty, liftupAni);
                        }
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
                    var fontColor = ((SolidColorBrush)FindResource("InActiveLrcForeground")).Color;
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


    private LinearGradientBrush CreateBrush(double progress)
    {
        var fontColor = ((SolidColorBrush)FindResource("ForeColor")).Color;
        var highlightColor =CustomHighlighterColor?.Color ?? fontColor;
        var normalColor = CustomNormalColor?.Color ?? ((SolidColorBrush)FindResource("InActiveLrcForeground")).Color;
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

    public void ClearHighlighter(bool animated=false)
    {
        mainSyllableBrushes.Clear();
        mainSyllableAnimated.Clear();
        var inactiveColor= CustomNormalColor?.Color ?? ((SolidColorBrush)FindResource("InActiveLrcForeground")).Color;
        var foreColor= ((SolidColorBrush)FindResource("ForeColor")).Color;
        foreach (var lrc in mainSyllableLrcs)
        {
            if (animated)
            {
                var fore=new SolidColorBrush(foreColor);
                var ca = new ColorAnimation(inactiveColor, TimeSpan.FromMilliseconds(300));
                ca.Completed += delegate {
                    lrc.Value.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
                };
                fore.BeginAnimation(SolidColorBrush.ColorProperty,ca);
                lrc.Value.Foreground = fore;
            }
            else lrc.Value.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
            //clear highlight lrc effect
            if (lrc.Value.Inlines.Count > 1)
            {
                foreach(var line in lrc.Value.Inlines)
                {
                    if(line is InlineUIContainer con&&con.Child is TextBlock block)
                    {
                        if (animated)
                        {
                            var fore = new SolidColorBrush(foreColor);
                            var ca = new ColorAnimation(inactiveColor, TimeSpan.FromMilliseconds(300));
                            ca.Completed += delegate {
                               block.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
                            };
                            fore.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                           block.Foreground = fore;
                        }
                        else block.SetResourceReference(ForegroundProperty, "InActiveLrcForeground");
                    }
                }
            }
            //clear lift-up
            if (lrc.Value.RenderTransform is TranslateTransform trans)
            {
                trans.BeginAnimation(TranslateTransform.YProperty, null);
                trans.Y = 0;
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
                var blur = new BlurEffect() { Radius = InActiveLrcBlurRadius };
                control.Effect = blur;
                blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(InActiveLrcBlurRadius, 0, TimeSpan.FromMilliseconds(300)));
                foreach (var lrc in control.mainSyllableLrcs)
                {
                    control.EnsureBrush(lrc.Value, lrc.Key, 0);
                    if (lrc.Key.Duration >= control.EmphasisThreshold&& lrc.Value.Inlines.Count>1)
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
            }
            else
            {
                var blur = new BlurEffect() { Radius = 0 };
                control.Effect = blur;
                blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimation(0, InActiveLrcBlurRadius, TimeSpan.FromMilliseconds(300)));
                control.ClearHighlighter(true);
            }
        }

    }
}
