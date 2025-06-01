using Lyricify.Lyrics.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EleCho.WpfSuite;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LemonApp.Views.UserControls;
public record class LrcLine(SyllableLineInfo Lrc, string? Trans = null, SyllableLineInfo? Romaji = null);
/// <summary>
/// [Simplified]
/// SimpleLyricView and LyricLineControl are used to display lyrics with syllables
/// </summary>
public partial class SimpleLyricView : UserControl
{
    public SimpleLyricView()
    {
        InitializeComponent();
        SizeChanged += SimpleLyricView_SizeChanged;
    }

    private readonly Dictionary<SyllableLineInfo, LyricLineControl> lrcs = [];
    private SyllableLineInfo? currentLrc;

    public event Action<LrcLine> OnNextLrcReached;

    public void Reset()
    {
        lrcs.Clear();
        currentLrc = null;
        LrcContainer.Children.Clear();
        scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
    }
    public void ApplyFontSize(double size,double delta)
    {
        foreach(var control in lrcs.Values)
        {
            control.FontSize = size - delta;
            foreach( TextBlock tb in control.MainLrcContainer.Children)
            {
                tb.FontSize = size;
            }
        }
    }
    public void SetShowTranslation(bool show)
    {
        foreach (var lrc in lrcs.Values)
        {
            lrc.TranslationLrc.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    public void SetShowRomaji(bool show)
    {
        foreach (var lrc in lrcs.Values)
        {
            lrc.RomajiLrcContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public void Load(LyricsData lyricsData,LyricsData? trans=null,LyricsData? romaji=null)
    {
        LrcContainer.Children.Clear();
        lrcs.Clear();
        LrcContainer.Children.Add(new TextBlock() { Height = 300 ,Background=Brushes.Transparent});
        foreach (var line in lyricsData.Lines)
        {
            if(line is SyllableLineInfo{ } syllable)
            {
                var lrc=new LyricLineControl(syllable.Syllables);
                LrcContainer.Children.Add(lrc);
                lrcs[syllable] = lrc;
            }
        }
        LrcContainer.Children.Add(new TextBlock() { Height = 300, Background = Brushes.Transparent });
        if (trans is { Lines:not null})
        {
            foreach(var lrc in lrcs)
            {
                var transLrc = trans.Lines.FirstOrDefault(a=>a.StartTime>=lrc.Key.StartTime-10);
                if (transLrc != null&&transLrc.Text!="//")
                    lrc.Value.TranslationLrc.Text = transLrc.Text;
            }
        }
        if (romaji  is { Lines: not null})
        {
            foreach(var lrc in lrcs)
            {
                var romajiLrc = romaji.Lines.FirstOrDefault(a => a.StartTime >= lrc.Key.StartTime - 10);
                if (romajiLrc is SyllableLineInfo { } roma)
                    lrc.Value.LoadRomajiLrc(roma);
            }
        }
    }

    public void UpdateTime(int ms)
    {
        {
            if (currentLrc != null && lrcs.TryGetValue(currentLrc, out var lrc))
                lrc.UpdateTime(ms);
        }

        //find the line that matches the current time
        foreach (var kvp in lrcs)
        {
            var line = kvp.Key;
            var control = kvp.Value;
            if (line.StartTime <= ms && line.EndTime >= ms)
            {
                if (line == currentLrc) return;

                if (currentLrc != null && lrcs.TryGetValue(currentLrc, out var lrc))
                {
                    lrc.IsCurrent = false;
                }
                currentLrc = line;
                var currentControl = lrcs[currentLrc];
                currentControl.IsCurrent = true;
                //Notify the change in current line
                OnNextLrcReached?.Invoke(new(line, currentControl.TranslationLrc.Text,currentControl.RomajiSyllables));
                ScrollToCurrent();
            }
        }
    }

    public void ScrollToCurrent()
    {
        try
        {
            if (currentLrc == null) return;
            GeneralTransform gf = lrcs[currentLrc].TransformToVisual(LrcContainer);
            Point p = gf.Transform(new Point(0, 0));
            double os = p.Y - (scrollviewer.ActualHeight / 2) + 120;
            var da = new DoubleAnimation(os, TimeSpan.FromMilliseconds(500));
            da.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, da);
        }
        catch { }
    }

    private void scrollviewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        e.Handled = true;
    }
    private void SimpleLyricView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScrollToCurrent();
    }
}

