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
    private readonly DrawingVisual _lowFreqVisual = new();
    private readonly DrawingVisual _highFreqVisual = new();
    private readonly VisualCollection _children;
    public AudioVisualizer()
    {
        this.IsVisibleChanged += AudioVisualizer_IsVisibleChanged;
        _children = new(this)
        {
            _highFreqVisual,
            _lowFreqVisual
        };
    }

    // 必须重写这两个方法以支持视觉子元素
    protected override int VisualChildrenCount => 2;
    protected override Visual GetVisualChild(int index)
    {
        return index switch
        {
            0 => _highFreqVisual,
            1 => _lowFreqVisual,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
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
    private readonly float[] _spectrumData = new float[512];
    private readonly float[] _displayValues = new float[512];
    private Brush _highFreqFill;

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
        DependencyProperty.Register("Fill", typeof(Brush), typeof(AudioVisualizer), new PropertyMetadata(Brushes.LightBlue, OnFillChanged));

    private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer && e.NewValue is Brush newBrush)
        {
            var highFreqBrush = newBrush.Clone();
            highFreqBrush.Opacity = 0.6;
            if (highFreqBrush.CanFreeze)
                highFreqBrush.Freeze();
            visualizer._highFreqFill = highFreqBrush;
        }
    }

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
        if (Player == null) return;
        Player.GetFFTData(_spectrumData);

        int stripCount = StripCount;
        int actualHeight = (int)ActualHeight;
        if (stripCount <= 0) return;

        float[] processedData = new float[stripCount];
        int dataLen = _spectrumData.Length / 2; // We only use the first half of the spectrum data (real part)

        // Logarithmic scaling
        double logMin = Math.Log(1);
        double logMax = Math.Log(dataLen);
        double logRange = logMax - logMin;

        for (int i = 0; i < stripCount; i++)
        {
            double logI = logMin + (logRange / stripCount) * i;
            double linearI = Math.Exp(logI);
            int index = (int)Math.Round(linearI);
            if (index >= dataLen) index = dataLen - 1;

            float value = (float)Math.Log10(1 + actualHeight * _spectrumData[index]);
            processedData[i] = value;
        }

        // Improved smoothing
        const float attack = 0.25f; // Faster rise
        const float decay = 0.08f;  // Slower fall

        for (int i = 0; i < stripCount; i++)
        {
            float newValue = processedData[i];
            float oldValue = _displayValues[i];

            if (newValue > oldValue)
            {
                _displayValues[i] += (newValue - oldValue) * attack;
            }
            else
            {
                _displayValues[i] += (newValue - oldValue) * decay;
            }
        }

        using (DrawingContext dcHigh = _highFreqVisual.RenderOpen())
        {
            DrawStrips(dcHigh,  _highFreqFill, true);
        }
        using (DrawingContext dcLow = _lowFreqVisual.RenderOpen())
        {
            DrawStrips(dcLow,  Fill, false);
        }
    }

    private void DrawStrips(DrawingContext drawingContext,   Brush fill, bool reversed)
    {
        int stripCount = StripCount;
        if (stripCount <= 0) return;
        int actualHeight = (int)ActualHeight;

        // 构建点集
        Point[] points = new Point[stripCount];
        int total = stripCount > 1 ? stripCount - 1 : 1;
        for (int i = 0; i < stripCount; i++)
        {
            int displayIndex = i;
            double x;
            if (reversed)
            {
                x = (double)i / total * ActualWidth;
            }
            else
            {
                x = (1.0d - (double)i / total) * ActualWidth;
            }
            double y = actualHeight * (1 - _displayValues[displayIndex]);
            points[i] = new Point(x, y);
        }

        // 绘制几何图形
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true, true);

            for (int i = 1; i < stripCount - 2; i++)
            {
                var mid = new Point(
                    (points[i].X + points[i + 1].X) / 2,
                    (points[i].Y + points[i + 1].Y) / 2
                );
                ctx.QuadraticBezierTo(points[i], mid, true, false);
            }
            if (stripCount > 1)
            {
                ctx.LineTo(points[stripCount - 1], true, false);
            }
            ctx.LineTo(new Point(points[stripCount - 1].X, actualHeight), true, false);
            ctx.LineTo(new Point(points[0].X, actualHeight), true, false);
            ctx.LineTo(points[0], true, false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(fill, null, geometry);
    }
}