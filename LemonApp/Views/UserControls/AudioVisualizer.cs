using LemonApp.Common.Funcs;
using System;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.Views.UserControls;
public class AudioVisualizer : FrameworkElement
{
    static AudioVisualizer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AudioVisualizer), new FrameworkPropertyMetadata(typeof(AudioVisualizer)));
    }
    private readonly DrawingVisual _visualHost = new();
    private readonly VisualCollection _children;
    public AudioVisualizer()
    {
        this.IsVisibleChanged += AudioVisualizer_IsVisibleChanged;
        _children = new(this)
        {
            _visualHost
        };
    }

    // 必须重写这两个方法以支持视觉子元素
    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0);
        return _visualHost;
    }

    private void AudioVisualizer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && IsPlaying)
            Start();
        else Stop();
    }

    internal void Start()
    {
        if (_isRunning || Player == null) return;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
        _isRunning = true;
    }

    internal void Stop()
    {
        if (!_isRunning) return;
        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        _isRunning = false;
    }

    #region Properties
    public MusicPlayer Player;

    private bool _isRunning = false;
    private readonly float[] _spectrumData = new float[1024];
    private readonly float[] _displayValues = new float[1024];

    public int StripCount
    {
        get { return (int)GetValue(StripCountProperty); }
        set { SetValue(StripCountProperty, value); }
    }

    public static readonly DependencyProperty StripCountProperty =
        DependencyProperty.Register("StripCount",
            typeof(int), typeof(AudioVisualizer), new PropertyMetadata(128));

    public Brush Fill
    {
        get { return (Brush)GetValue(FillProperty); }
        set { SetValue(FillProperty, value); }
    }
    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register("Fill", typeof(Brush), typeof(AudioVisualizer), new PropertyMetadata(Brushes.LightBlue));

    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register("IsPlaying", typeof(bool), typeof(AudioVisualizer), new PropertyMetadata(false, OnIsPlayingChanged));

    private static void OnIsPlayingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is AudioVisualizer visualizer)
        {
            if (e.NewValue is true && visualizer.IsVisible)
            {
                visualizer.Start();
            }
            else
            {
                visualizer.Stop();
            }
        }
    }

    #endregion

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        Player.GetFFTData(_spectrumData);

        using DrawingContext dc = _visualHost.RenderOpen();
        DrawStrips(dc, _spectrumData);
    }

    private void DrawStrips(DrawingContext drawingContext, float[] spectrumData)
    {
        int stripCount = StripCount;
        int total = stripCount - 1;
        const float easing = 0.25f;

        // 更新缓动值
        for (int i = 0; i < stripCount; i++)
        {
            double target = spectrumData[i];
            _displayValues[i] += (float)((target - _displayValues[i]) * easing);
        }

        // 构建点集
        Point[] points = new Point[stripCount];
        for (int i = 0; i < stripCount; i++)
        {
            double x = (1.0d - (double)i / total) * ActualWidth;
            double y = ActualHeight * (1 - _displayValues[i]);
            points[i] = new Point(x, y);
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true, true);

            for (int i = 1; i < stripCount - 2; i++)
            {
                // 取中点做平滑
                var mid = new Point(
                    (points[i].X + points[i + 1].X) / 2,
                    (points[i].Y + points[i + 1].Y) / 2
                );
                ctx.QuadraticBezierTo(points[i], mid, true, false);
            }
            // 最后两个点直接连接
            ctx.LineTo(points[stripCount - 1], true, false);
            // 闭合到底部
            ctx.LineTo(new Point(points[stripCount - 1].X, ActualHeight), true, false);
            ctx.LineTo(new Point(points[0].X, ActualHeight), true, false);
            ctx.LineTo(points[0], true, false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(Fill, null, geometry);
    }

}