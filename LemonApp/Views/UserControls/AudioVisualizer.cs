using LemonApp.Common.Funcs;
using System;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.Views.UserControls;

public sealed class AudioVisualizer : FrameworkElement
{
    static AudioVisualizer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(AudioVisualizer),
            new FrameworkPropertyMetadata(typeof(AudioVisualizer)));
    }

    #region Visuals

    private readonly DrawingVisual _lowVisual = new();
    private readonly DrawingVisual _highVisual = new();
    private readonly VisualCollection _children;

    protected override int VisualChildrenCount => 2;

    protected override Visual GetVisualChild(int index) => index switch
    {
        0 => _highVisual,
        1 => _lowVisual,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    #endregion

    public AudioVisualizer()
    {
        _children = new VisualCollection(this)
        {
            _highVisual,
            _lowVisual
        };

        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && IsPlaying) Start();
            else Stop();
        };
    }

    #region Public API

    public MusicPlayer? Player;

    public int StripCount
    {
        get => (int)GetValue(StripCountProperty);
        set => SetValue(StripCountProperty, value);
    }

    public static readonly DependencyProperty StripCountProperty =
        DependencyProperty.Register(
            nameof(StripCount),
            typeof(int),
            typeof(AudioVisualizer),
            new PropertyMetadata(128, OnStripCountChanged));

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(Brushes.LightBlue, OnFillChanged));

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(AudioVisualizer),
            new PropertyMetadata(false, OnIsPlayingChanged));

    #endregion

    #region Internal State

    private bool _running;
    private AudioVisualizerProcessor? _processor;


    // Bezier 点缓存
    private Point[] _points = Array.Empty<Point>();

    private Brush? _highFill;

    #endregion

    #region Lifecycle

    private void Start()
    {
        if (_running || Player is null) return;

        _processor ??= new AudioVisualizerProcessor(Player, StripCount);
        _processor.Start();

        CompositionTarget.Rendering += OnRender;
        _running = true;
    }

    private void Stop()
    {
        if (!_running) return;

        CompositionTarget.Rendering -= OnRender;
        _processor?.Dispose();
        _processor = null;

        _running = false;
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (AudioVisualizer)d;
        if ((bool)e.NewValue && v.IsVisible) v.Start();
        else v.Stop();
    }

    private static void OnStripCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      ((AudioVisualizer)d).ResizeBuffers();
    }
    private void ResizeBuffers()
    {
        int n = Math.Max(1, StripCount);
        _points = new Point[n];
    }


    private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (AudioVisualizer)d;
        if (e.NewValue is not Brush b) return;

        var hf = b.Clone();
        hf.Opacity = 0.6;
        if (hf.CanFreeze) hf.Freeze();
        v._highFill = hf;
    }

    #endregion

    #region Rendering Pipeline

    private void OnRender(object? sender, EventArgs e)
    {
        if (_processor is null)
            return;

        var data = _processor.Current;

        if (_highFill != null)
            Draw(_highVisual, _highFill, data, true);
        Draw(_lowVisual, Fill, data, false);
    }

    private void Draw(
        DrawingVisual visual,
        Brush fill,
        ReadOnlySpan<float> data,
        bool reversed)
    {
        int n = data.Length;
        if (n < 2) return;

        double w = ActualWidth;
        double h = ActualHeight;
        int last = n - 1;

        var pts = _points.AsSpan();

        for (int i = 0; i < n; i++)
        {
            double x = reversed
                ? i * w / last
                : (last - i) * w / last;

            double y = h * (1 - data[i]);
            pts[i] = new Point(x, y);
        }

        using var dc = visual.RenderOpen();
        var geo = new StreamGeometry();

        using (var g = geo.Open())
        {
            g.BeginFigure(pts[0], true, true);

            for (int i = 1; i < n - 1; i++)
            {
                var mid = new Point(
                    (pts[i].X + pts[i + 1].X) * 0.5,
                    (pts[i].Y + pts[i + 1].Y) * 0.5);

                g.QuadraticBezierTo(pts[i], mid, true, false);
            }

            g.LineTo(new Point(pts[last].X, h), true, false);
            g.LineTo(new Point(pts[0].X, h), true, false);
        }

        geo.Freeze();
        dc.DrawGeometry(fill, null, geo);
    }

    #endregion
}
