using LemonApp.Common.Funcs;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        this.SizeChanged += AudioVisualizer_SizeChanged;
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

    private void AudioVisualizer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        CreatePen();
    }

    private void AudioVisualizer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true&&IsPlaying)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    internal void Start()
    {
        if (_isRunning || Player == null) return;

        Stop();

        _isRunning = true;
        _spectrumData = _dataPool.Rent(1024);

        //initialize pen and geometry objects
        CreatePen();
        InitializeGeometryObjects();

        if (_renderLoop != null) return;
        _renderCancel = new CancellationTokenSource();
        _renderLoop = RenderLoopAsync(_renderCancel.Token);
    }
    internal void Stop()
    {
        if (!_isRunning || _renderCancel == null || _spectrumData == null) return;
        _isRunning = false;
        _dataPool.Return(_spectrumData);
        _spectrumData = null;
        _renderCancel.Cancel();
        _renderLoop = null;
    }

    #region Properties
    public MusicPlayer Player;

    bool _isRunning = false;
    float[]? _spectrumData;
    CancellationTokenSource? _renderCancel;
    Task? _renderLoop;
    private readonly ArrayPool<float> _dataPool = ArrayPool<float>.Shared;


    public float StripSpacing
    {
        get { return (float)GetValue(StripSpacingProperty); }
        set { SetValue(StripSpacingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for StripSpacing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty StripSpacingProperty =
        DependencyProperty.Register("StripSpacing", typeof(float), typeof(AudioVisualizer), new PropertyMetadata(0.2f));

    public int StripCount
    {
        get { return (int)GetValue(StripCountProperty); }
        set { SetValue(StripCountProperty, value); }
    }

    // Using a DependencyProperty as the backing store for StripCount.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty StripCountProperty =
        DependencyProperty.Register("StripCount", 
            typeof(int), typeof(AudioVisualizer), new PropertyMetadata(128));

    public Brush Color
    {
        get { return (Brush)GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color", typeof(Brush), typeof(AudioVisualizer), new PropertyMetadata(Brushes.White, OnColorChanged));

    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register("IsPlaying", typeof(bool), typeof(AudioVisualizer), new PropertyMetadata(false, OnIsPlayingChanged));

    private static void OnColorChanged(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if (o is AudioVisualizer visualizer)
            visualizer.CreatePen();
    }
    private static void OnIsPlayingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is AudioVisualizer visualizer)
        {
            if (e.NewValue is true&&visualizer.IsVisible)
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

    private async Task RenderLoopAsync(CancellationToken token)
    {
        while (true)
        {
            if (token.IsCancellationRequested)
                break;
            Player.GetFFTDataRef(ref _spectrumData); // FFT 1024 截取前StripCount

            //InvalidateVisual();
            using DrawingContext dc = _visualHost.RenderOpen();
            DrawStrips(dc, _spectrumData);

            await Task.Delay(16,token);//60Hz
        }
    }

/*    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Visibility != Visibility.Visible) return;
        if (Player == null || _spectrumData == null) return;
        DrawStrips(drawingContext, _spectrumData);
    }*/

    private PathFigure[] _pathFigures;
    private LineSegment[] _lineSegments;
    private PathGeometry _pathGeometry = new();
    private PathFigureCollection _figuresToUse;
    private Pen _pen;

    internal void CreatePen()
    {
        double thickness = ActualWidth / StripCount * (1 - StripSpacing);
        if (thickness < 0)
            thickness = 1;
        _pen = new Pen(Color, thickness);
        if (_pen.CanFreeze)
            _pen.Freeze();
    }
    internal void InitializeGeometryObjects()
    {
        // 仅在首次或条形数量改变时初始化
        if (_pathFigures == null || _pathFigures.Length != StripCount)
        {
            _figuresToUse = new();
            _pathFigures = new PathFigure[StripCount];
            _lineSegments = new LineSegment[StripCount];

            for (int i = 0; i < StripCount; i++)
            {
                _lineSegments[i] = new LineSegment();
                _pathFigures[i] = new PathFigure
                {
                    Segments = { _lineSegments[i] }
                };
                _figuresToUse.Add(_pathFigures[i]);
            }
            _pathGeometry = new PathGeometry(_figuresToUse);
        }
    }

    private void DrawStrips(DrawingContext drawingContext, float[] spectrumData)
    {
        int stripCount = StripCount;
        int total = stripCount - 1;
        int index = 0;

        for (int i = stripCount - 1; i >= 0; i--, index++)
        {
            double value = spectrumData[i];
            double y = ActualHeight * (1 - value);
            double x = ((double)index / total) * ActualWidth;

            _pathFigures[index].StartPoint = new Point(x, ActualHeight);
            _lineSegments[index].Point = new Point(x, y);
        }

        drawingContext.DrawGeometry(null, _pen, _pathGeometry);
    }

}