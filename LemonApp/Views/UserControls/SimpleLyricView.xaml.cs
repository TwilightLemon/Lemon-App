using EleCho.WpfSuite;
using Lyricify.Lyrics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Views.UserControls;
public record class LrcLine(SyllableLineInfo Lrc, string? Trans = null, SyllableLineInfo? Romaji = null);
/// <summary>
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
    private SyllableLineInfo? currentLrc,notifiedLrc;

    public event Action<LrcLine> OnNextLrcReached;

    public void Reset()
    {
        lrcs.Clear();
        currentLrc = null;
        LrcContainer.Children.Clear();
        scrollviewer.BeginAnimation(ScrollViewerUtils.VerticalOffsetProperty, null);
    }
    public void ApplyFontSize(double size, double scale)
    {
        foreach(var control in lrcs.Values)
        {
            control.FontSize = size * scale;
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

    private Thickness lyricSpacing= new(0, 0, 0, 40);
    public void Load(LyricsData lyricsData,LyricsData? trans=null,LyricsData? romaji=null)
    {
        LrcContainer.Children.Clear();
        lrcs.Clear();
        LrcContainer.Children.Add(new TextBlock() { Height = 300 ,Background=Brushes.Transparent});
        foreach (var line in lyricsData.Lines)
        {
            if(line is SyllableLineInfo{ } syllable)
            {
                var lrc=new LyricLineControl(syllable.Syllables) {Margin= lyricSpacing };
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

        //从上一条结束到本条结束都是当前歌词时间，目的是本条歌词结束就跳转到下一个
        KeyValuePair<SyllableLineInfo, LyricLineControl>? lastItem=null,target = null;
        foreach (var cur in lrcs)
        {
            if((lastItem?.Key.EndTime ?? cur.Key.StartTime) <= ms && cur.Key.EndTime >= ms)
            {
                target = cur;
                break;
            }
            lastItem = cur;
        }

        //next found. 对于LyricPage希望准确使用当前时间来定位歌词，对于OnNextLrcReached事件则希望使用上一次结束时跳转
        if (target != null&&target.HasValue)
        {
            var line = target.Value.Key;
            var control = target.Value.Value;

            //Notify the change in current line
            if (line != notifiedLrc)
            {
                OnNextLrcReached?.Invoke(new(line, control.TranslationLrc.Text, control.RomajiSyllables));
                notifiedLrc = line;
            }

            if (line.StartTime > ms || line.EndTime < ms) return;//skip if not in range.

            if (line == currentLrc) return;//skip if already being the current lrc.
            if (currentLrc != null && lrcs.TryGetValue(currentLrc, out var lrc))
            {
                lrc.IsCurrent = false;// set last-played lrc inactive.
            }
            currentLrc = line;
            var currentControl = control;
            currentControl.IsCurrent = true;
            ScrollToCurrent();
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

