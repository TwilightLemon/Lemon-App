namespace LemonApp.Common.Funcs;

public sealed class AudioVisualizerProcessor : IDisposable
{
    private readonly MusicPlayer _player;

    private readonly float[] _fft;
    private readonly float[] _bufferA;
    private readonly float[] _bufferB;

    private float[] _front;
    private float[] _back;

    private readonly Thread _thread;
    private volatile bool _running;

    public AudioVisualizerProcessor(MusicPlayer player, int stripCount)
    {
        _player = player;

        _fft = new float[1024];
        _bufferA = new float[stripCount];
        _bufferB = new float[stripCount];

        _front = _bufferA;
        _back = _bufferB;

        _thread = new Thread(Worker)
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
    }

    public ReadOnlySpan<float> Current => _front;
    public int LogScaleFactor { get; set; } = 40;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread.Join();
    }

    private void Worker()
    {
        var fft = _fft.AsSpan(0, _fft.Length / 2);

        while (_running)
        {
            var bars = _back.AsSpan();
            var lastFrame = _front.AsSpan();

            _player.GetFFTData(_fft);

            DownsampleLog(fft, bars,LogScaleFactor);
            Smooth(bars, lastFrame);

            _back = Interlocked.Exchange(ref _front, _back);

            //~60hz
            Thread.Sleep(16);
        }
    }

    private static void DownsampleLog(ReadOnlySpan<float> fft, Span<float> bars,int scale)
    {
        int bins = fft.Length;
        int count = bars.Length;

        double logMin = Math.Log(1);
        double logMax = Math.Log(bins);
        double step = (logMax - logMin) / count;

        for (int i = 0; i < count; i++)
        {
            int idx = (int)Math.Exp(logMin + step * i);
            if (idx >= bins) idx = bins - 1;

            float value = (float)Math.Log10(1 + scale * fft[idx]);
            bars[i] = value;
        }
    }

    private static void Smooth(Span<float> data, ReadOnlySpan<float> lastFrame)
    {
        const float attack = 0.25f;
        const float decay = 0.08f;

        for (int i = 0; i < data.Length; i++)
        {
            float current = data[i];
            float last = lastFrame[i];
            float smoothed = current > last
                ? last + (current - last) * attack
                : last + (current - last) * decay;
            data[i] = smoothed;
        }
    }

    public void Dispose()
    {
        if (_running)
            Stop();
    }
}