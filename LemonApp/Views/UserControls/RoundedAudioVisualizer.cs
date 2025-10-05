using LemonApp.Common.Funcs;
using System;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.Views.UserControls;

/// <summary>
/// 圆形音频可视化控件，绘制环绕专辑图的波动效果
/// </summary>
public class RoundedAudioVisualizer : FrameworkElement
{
    static RoundedAudioVisualizer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RoundedAudioVisualizer), 
            new FrameworkPropertyMetadata(typeof(RoundedAudioVisualizer)));
    }

    private readonly DrawingVisual _visualHost = new();
    private readonly VisualCollection _children;
    private double _rotationAngle = 0; // 旋转角度

    public RoundedAudioVisualizer()
    {
        this.IsVisibleChanged += RoundedAudioVisualizer_IsVisibleChanged;
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

    private void RoundedAudioVisualizer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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

    /// <summary>
    /// 波形采样点数量
    /// </summary>
    public int SampleCount
    {
        get { return (int)GetValue(SampleCountProperty); }
        set { SetValue(SampleCountProperty, value); }
    }

    public static readonly DependencyProperty SampleCountProperty =
        DependencyProperty.Register("SampleCount",
            typeof(int), typeof(RoundedAudioVisualizer), new PropertyMetadata(128));

    /// <summary>
    /// 波形最大振幅（相对于内圆半径的增量）
    /// </summary>
    public double WaveAmplitude
    {
        get { return (double)GetValue(WaveAmplitudeProperty); }
        set { SetValue(WaveAmplitudeProperty, value); }
    }

    public static readonly DependencyProperty WaveAmplitudeProperty =
        DependencyProperty.Register("WaveAmplitude",
            typeof(double), typeof(RoundedAudioVisualizer), new PropertyMetadata(30.0));

    /// <summary>
    /// 旋转速度（度/帧）
    /// </summary>
    public double RotationSpeed
    {
        get { return (double)GetValue(RotationSpeedProperty); }
        set { SetValue(RotationSpeedProperty, value); }
    }

    public static readonly DependencyProperty RotationSpeedProperty =
        DependencyProperty.Register("RotationSpeed",
            typeof(double), typeof(RoundedAudioVisualizer), new PropertyMetadata(0.5));

    /// <summary>
    /// 填充画刷
    /// </summary>
    public Brush Fill
    {
        get { return (Brush)GetValue(FillProperty); }
        set { SetValue(FillProperty, value); }
    }

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register("Fill", typeof(Brush), 
            typeof(RoundedAudioVisualizer), new PropertyMetadata(Brushes.LightBlue));

    /// <summary>
    /// 描边画刷
    /// </summary>
    public Brush Stroke
    {
        get { return (Brush)GetValue(StrokeProperty); }
        set { SetValue(StrokeProperty, value); }
    }

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register("Stroke", typeof(Brush), 
            typeof(RoundedAudioVisualizer), new PropertyMetadata(null));

    /// <summary>
    /// 描边粗细
    /// </summary>
    public double StrokeThickness
    {
        get { return (double)GetValue(StrokeThicknessProperty); }
        set { SetValue(StrokeThicknessProperty, value); }
    }

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register("StrokeThickness",
            typeof(double), typeof(RoundedAudioVisualizer), new PropertyMetadata(0.0));

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register("IsPlaying", typeof(bool), 
            typeof(RoundedAudioVisualizer), new PropertyMetadata(false, OnIsPlayingChanged));

    private static void OnIsPlayingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is RoundedAudioVisualizer visualizer)
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

        // 更新旋转角度
        _rotationAngle += RotationSpeed;
        if (_rotationAngle >= 360)
            _rotationAngle -= 360;

        using DrawingContext dc = _visualHost.RenderOpen();
        DrawCircularWave(dc, _spectrumData);
    }

    private void DrawCircularWave(DrawingContext drawingContext, float[] spectrumData)
    {
        int sampleCount = SampleCount;
        const float easing = 0.25f;

        // 更新缓动值
        for (int i = 0; i < sampleCount; i++)
        {
            double target = spectrumData[i];
            _displayValues[i] += (float)((target - _displayValues[i]) * easing);
        }

        double centerX = ActualWidth / 2;
        double centerY = ActualHeight / 2;

        double innerRadius = Math.Min(ActualWidth, ActualHeight) / 2;

        // 构建外圈波形点集
        Point[] outerPoints = new Point[sampleCount];

        double rotationRadians = _rotationAngle * Math.PI / 180;

        for (int i = 0; i < sampleCount; i++)
        {
            // 计算角度（从0到2π），加上旋转偏移
            double angle = (double)i / sampleCount * 2 * Math.PI + rotationRadians;
            
            // 根据频谱数据计算当前点的半径
            double amplitude = _displayValues[i] * WaveAmplitude;
            double outerRadius = innerRadius + amplitude;

            // 计算外圈点坐标
            outerPoints[i] = new Point(
                centerX + outerRadius * Math.Cos(angle),
                centerY + outerRadius * Math.Sin(angle)
            );
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(outerPoints[0], true, true);

            for (int i = 0; i < sampleCount; i++)
            {
                int nextIndex = (i + 1) % sampleCount;
                
                var controlPoint = outerPoints[i];
                var endPoint = new Point(
                    (outerPoints[i].X + outerPoints[nextIndex].X) / 2,
                    (outerPoints[i].Y + outerPoints[nextIndex].Y) / 2
                );
                
                ctx.QuadraticBezierTo(controlPoint, endPoint, true, false);
            }
            
            ctx.QuadraticBezierTo(outerPoints[0], outerPoints[0], true, true);
        }

        geometry.Freeze();

        Pen pen = null;
        if (Stroke != null && StrokeThickness > 0)
        {
            pen = new Pen(Stroke, StrokeThickness);
            pen.Freeze();
        }

        drawingContext.DrawGeometry(Fill, pen, geometry);
    }
}
