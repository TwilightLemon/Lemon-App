using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
    private readonly Dictionary<ISyllableInfo, TextBlock> mainSyllableLrcs = [], romajiSyllableLrcs = [];
    public LyricLineControl(List<ISyllableInfo> words)
    {
        InitializeComponent();
        Opacity = InitialOpacity;
        foreach (var word in words)
        {
            var textBlock = new TextBlock
            {
                Text = word.Text,
                FontSize = 22
            };
            MainLrcContainer.Children.Add(textBlock);
            mainSyllableLrcs[word] = textBlock;
        }
    }

    public void LoadRomajiLrc(List<ISyllableInfo> words)
    {
        foreach (var word in words)
        {
            var textBlock = new TextBlock
            {
                Text = word.Text
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
            }
            else
            {
                // 正在进行，判断是否需要启动动画
                if (!mainSyllableAnimated.TryGetValue(syllable, out var animated) || !animated)
                {
                    var brush = EnsureBrush(textBlock, syllable, 0.0);
                    var duration = TimeSpan.FromMilliseconds(syllable.Duration);
                    var anim = new DoubleAnimation(0.0, 1.0, new Duration(duration));
                    brush.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, anim);
                    brush.GradientStops[2].BeginAnimation(GradientStop.OffsetProperty, anim);
                    mainSyllableAnimated[syllable] = true;

                    if (syllable.Duration > 2000)
                    {
                        //向上抬起又落下的动画
                        var easing = new CubicEase();
                        var position = new Thickness(0, -6, 0, 6);
                        var upAni = new ThicknessAnimationUsingKeyFrames()
                        {
                            KeyFrames = [new EasingThicknessKeyFrame(position, TimeSpan.FromMilliseconds(1500)){
                                EasingFunction=easing
                            },
                              new EasingThicknessKeyFrame(position, TimeSpan.FromMilliseconds(syllable.Duration)),
                                                   new EasingThicknessKeyFrame(default, TimeSpan.FromMilliseconds(syllable.Duration+600)){
                                                       EasingFunction=easing
                                                   }],
                        };
                        textBlock.BeginAnimation(MarginProperty, upAni);
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
            var fontColor = ((SolidColorBrush)FindResource("ForeColor")).Color;
            var highlightColor = fontColor;
            var normalColor = new Color { R = fontColor.R, G = fontColor.G, B = fontColor.B, A = 72 };
            brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops = new GradientStopCollection
            {
                new GradientStop(highlightColor, 0),
                new GradientStop(highlightColor, progress),
                new GradientStop(normalColor, progress),
                new GradientStop(normalColor, 1)
            }
            };
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

    public void ClearHighlighter()
    {
        mainSyllableBrushes.Clear();
        mainSyllableAnimated.Clear();
        foreach(var lrc in mainSyllableLrcs)
        {
            lrc.Value.SetResourceReference(ForegroundProperty, "ForeColor");
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
                foreach(var lrc in control.mainSyllableLrcs)
                {
                    control.EnsureBrush(lrc.Value, lrc.Key, 0);
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
