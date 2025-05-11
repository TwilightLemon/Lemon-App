using LemonApp.Common.Funcs;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonApp.Views.UserControls;
public class AudioVisualizer : Control
{
    static AudioVisualizer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AudioVisualizer), new FrameworkPropertyMetadata(typeof(AudioVisualizer)));
    }
    public AudioVisualizer()
    {
        this.IsVisibleChanged += AudioVisualizer_IsVisibleChanged;
    }

    private void AudioVisualizer_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if(e.NewValue is true)
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
        _isRunning = true;
        _spectrumData = _dataPool.Rent(1024);

        if (_renderLoop != null) return;
        _renderCancel = new CancellationTokenSource();
        _renderLoop = RenderLoopAsync(_renderCancel.Token);
    }
    internal void Stop()
    {
        if (!_isRunning|| _renderCancel==null|| _spectrumData==null) return;
        _isRunning = false;
        _dataPool.Return(_spectrumData);
        _spectrumData = null;
        _renderCancel.Cancel();
        _renderLoop = null;
        InvalidateVisual();
    }

    #region Properties
    public MusicPlayer Player;
    bool _isRunning = false;
    float[]? _spectrumData;
    CancellationTokenSource? _renderCancel;
    Task? _renderLoop;
    private ArrayPool<float> _dataPool = ArrayPool<float>.Shared;
    public int StripCount { get; set; } = 128;
    float StripSpacing = 0.2f;

    public Brush Color
    {
        get { return (Brush)GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color", typeof(Brush), typeof(AudioVisualizer), new PropertyMetadata(null));

    public bool IsPlaying
    {
        get { return (bool)GetValue(IsPlayingProperty); }
        set { SetValue(IsPlayingProperty, value); }
    }

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register("IsPlaying", typeof(bool), typeof(AudioVisualizer), new PropertyMetadata(false,OnIsPlayingChanged));

    static void OnIsPlayingChanged(DependencyObject o,DependencyPropertyChangedEventArgs e)
    {
        if(o is AudioVisualizer visualizer)
        {
            if(e.NewValue is true)
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
            InvalidateVisual();
            await Task.Delay(16);
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Visibility != Visibility.Visible) return;
        if (Player == null|| _spectrumData==null) return;
        DrawStrips(drawingContext, _spectrumData);
    }

    private PathGeometry _pathGeometry = new();
    private void DrawStrips(DrawingContext drawingContext, float[] spectrumData)
    {
        int stripCount = StripCount;
        double thickness = ActualWidth / stripCount * (1 - StripSpacing);

        if (thickness < 0)
            thickness = 1;

        _pathGeometry.Figures.Clear();
        int total = stripCount - 1;
        int index = 0;
       
        for(int i = stripCount - 1; i >= 0; i--,index++)
        {
            double value = spectrumData[i];
            double y = ActualHeight * (1 - value);
            double x = ((double)index / total) * ActualWidth;

            _pathGeometry.Figures.Add(new PathFigure()
            {
                StartPoint = new Point(x, ActualHeight),
                Segments =
                    {
                        new LineSegment()
                        {
                            Point = new Point(x, y)
                        }
                    }
            });
        }
        Pen pen = new(Color, thickness);
        drawingContext.DrawGeometry(null, pen, _pathGeometry);
    }

}