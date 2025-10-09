﻿using EleCho.WpfSuite;
using EleCho.WpfSuite.Media.Animation;
using LemonApp.Services;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Views.UserControls;
public record class LrcLine(ILineInfo Lrc, string? Trans = null, ILineInfo? Romaji = null);
public sealed class SelectiveLyricLine : Border
{
    public  LyricLineControl LyricLine { get; init; }
    public SelectiveLyricLine(LyricLineControl line)
    {
        Background = Brushes.Transparent;
        CornerRadius = new(12);
        Padding = new(8, 4, 8, 4);
        Child = LyricLine = line;
        MouseEnter += SelectiveLyricLine_MouseEnter;
        MouseLeave += SelectiveLyricLine_MouseLeave;
        MouseDown += SelectiveLyricLine_MouseDown;
    }

    private void SelectiveLyricLine_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if( LyricLine.MainLineInfo?.StartTime is int startTime )
        {
            var player = App.Services.GetRequiredService<MediaPlayerService>();
            player.Position = TimeSpan.FromMilliseconds(startTime);
            if(!player.IsPlaying)
                player.Play();
        }
    }

    private void SelectiveLyricLine_MouseLeave(object sender, MouseEventArgs e)
    {
        if (Background is SolidColorBrush)
            Background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(Colors.Transparent, TimeSpan.FromMilliseconds(200)));

        if (!LyricLine.IsCurrent)
            LyricLine.SetActiveState(false,false);
    }

    private void SelectiveLyricLine_MouseEnter(object sender,  MouseEventArgs e)
    {
        var color = ((SolidColorBrush)Application.Current.FindResource("MaskColor")).Color;
        var brush = new SolidColorBrush(Colors.Transparent);
        var da = new ColorAnimation(color, TimeSpan.FromMilliseconds(200));
        Background = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, da);
        if (!LyricLine.IsCurrent)
            LyricLine.SetActiveState(true);
    }
}

public partial class LyricHost : UserControl
{
    public LyricHost()
    {
        InitializeComponent();
        SizeChanged += SimpleLyricView_SizeChanged;
    }

    private readonly Dictionary<ILineInfo, SelectiveLyricLine> lrcs = [];
    private ILineInfo? currentLrc,notifiedLrc;
    private bool _isPureLrc = false;
    private DateTime _interruptedTime;

    public event Action<LrcLine> OnNextLrcReached;

    public void Reset()
    {
        lrcs.Clear();
        currentLrc = null;
        LrcContainer.Children.Clear();
        scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
    }
    private async Task WaitToScroll()
    {
        await Task.Delay(100);
        Dispatcher.Invoke(ScrollToCurrent);
    }
    public void ApplyFontSize(double size, double scale)
    {
        foreach(var control in lrcs.Values)
        {
            control.LyricLine.FontSize = size * scale;
            foreach( TextBlock tb in control.LyricLine.MainLrcContainer.Children)
            {
                tb.FontSize = size;
            }
        }
        _ = WaitToScroll();
    }
    public void SetShowTranslation(bool show)
    {
        foreach (var lrc in lrcs.Values)
        {
            lrc.LyricLine.TranslationLrc.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
        _ = WaitToScroll();
    }
    public void SetShowRomaji(bool show)
    {
        foreach (var lrc in lrcs.Values)
        {
            lrc.LyricLine.RomajiLrcContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
        _ = WaitToScroll();
    }

    private Thickness lyricSpacing= new(0, 0, 0, 30);
    public void Load(LyricsData lyricsData,LyricsData? trans=null,LyricsData? romaji=null,bool isPureLrc=false)
    {
        _isPureLrc= isPureLrc;
        LrcContainer.Children.Clear();
        lrcs.Clear();
        LrcContainer.Children.Add(new TextBlock() { Height = 300 ,Background=Brushes.Transparent});
        foreach (var line in lyricsData.Lines)
        {
            if(line is SyllableLineInfo{ } syllable)
            {
                var lrc = new LyricLineControl(syllable);
                var container =new SelectiveLyricLine(lrc) { Margin = lyricSpacing };
                LrcContainer.Children.Add(container);
                lrcs[syllable] = container;
            }else if(line is LineInfo { } pure)
            {
                var lrc = new LyricLineControl(pure);
                var container = new SelectiveLyricLine(lrc) { Margin = lyricSpacing };
                LrcContainer.Children.Add(container);
                lrcs[pure] = container;
            }
        }
        LrcContainer.Children.Add(new TextBlock() { Height = 300, Background = Brushes.Transparent });
        if (trans is { Lines:not null})
        {
            foreach(var lrc in lrcs)
            {
                var transLrc = trans.Lines.FirstOrDefault(a=>a.StartTime>=lrc.Key.StartTime-10);
                if (transLrc != null&&transLrc.Text!="//")
                    lrc.Value.LyricLine.TranslationLrc.Text = transLrc.Text;
            }
        }
        if (romaji  is { Lines: not null})
        {
            foreach(var lrc in lrcs)
            {
                var romajiLrc = romaji.Lines.FirstOrDefault(a => a.StartTime >= lrc.Key.StartTime - 10);
                if (romajiLrc is SyllableLineInfo { } roma)
                    lrc.Value.LyricLine.LoadRomajiLrc(roma);
                else if (romajiLrc is LineInfo { } pure)
                    lrc.Value.LyricLine.LoadPlainRomaji(pure.Text);
            }
        }
    }

    public void UpdateTime(int ms)
    {
        {
            if (currentLrc != null && lrcs.TryGetValue(currentLrc, out var lrc))
                lrc.LyricLine.UpdateTime(ms);
        }

        //从上一条结束到本条结束都是当前歌词时间，目的是本条歌词结束就跳转到下一个
        KeyValuePair<ILineInfo, SelectiveLyricLine>? lastItem=null,target = null;
        if (!_isPureLrc)
        {
            foreach (var cur in lrcs)
            {
                if ((lastItem?.Key.EndTime ?? cur.Key.StartTime) <= ms && cur.Key.EndTime >= ms)
                {
                    target = cur;
                    break;
                }
                lastItem = cur;
            }
        }
        else
        {
            target = lrcs.LastOrDefault(a => a.Key.StartTime <= ms);
        }

        //next found. 对于LyricPage希望准确使用当前时间来定位歌词，对于OnNextLrcReached事件则希望使用上一次结束时跳转
        if (target != null && target.HasValue)
        {
            var line = target.Value.Key;
            var control = target.Value.Value;
            if (line == null || control == null) return;

            //Notify the change in current line
            if (line != notifiedLrc)
            {
                OnNextLrcReached?.Invoke(new(line, control.LyricLine.TranslationLrc.Text, control.LyricLine.RomajiSyllables));
                notifiedLrc = line;
            }

            if (line.StartTime > ms || (line.EndTime??int.MaxValue) < ms) return;//skip if not in range.

            if (line == currentLrc) return;//skip if already being the current lrc.
            if (currentLrc != null && lrcs.TryGetValue(currentLrc, out var lrc))
            {
                lrc.LyricLine.IsCurrent = false;// set last-played lrc inactive.
            }
            currentLrc = line;
            var currentControl = control;
            currentControl.LyricLine.IsCurrent = true;
            ScrollToCurrent();
        }
    }

    public void ScrollToCurrent()
    {
        //被打断的xs内不再滚动
        if ((DateTime.Now - _interruptedTime).TotalSeconds < 5) return;
        try
        {
            if (currentLrc == null) return;
            GeneralTransform gf = lrcs[currentLrc].TransformToVisual(LrcContainer);
            Point p = gf.Transform(new Point(0, 0));
            double os = p.Y - (scrollviewer.ActualHeight / 2) + 120;
            var da = new DoubleAnimation(scrollviewer.VerticalOffset,os, TimeSpan.FromMilliseconds(500));
            da.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, da);
        }
        catch { }
    }

    private void scrollviewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _interruptedTime = DateTime.Now;
    }
    private void SimpleLyricView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScrollToCurrent();
    }
}

