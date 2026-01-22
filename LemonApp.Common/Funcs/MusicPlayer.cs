using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Un4seen.Bass;
namespace LemonApp.Common.Funcs;

/// <summary>
/// 封装bass.dll 的音乐播放器
/// </summary>
public class MusicPlayer
{
    private int stream = -1024;
    private readonly int bassflacHandle = -1;
    public MusicPlayer()
    {
        BassNet.Registration("lemon.app@qq.com", "2X52325160022");
        Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, IntPtr.Zero);
        bassflacHandle = Bass.BASS_PluginLoad("bassflac.dll");
    }
    /// <summary>
    /// 根据平台释放解码器dll
    /// </summary>
    /// <returns></returns>
    public static async Task PrepareDll() 
    {
        if (!File.Exists("bass.dll"))
        {
            if (Environment.Is64BitProcess)
                await ReleaseDLLFiles(Properties.Resources.bass,"bass.dll");
            else await ReleaseDLLFiles(Properties.Resources.bass_x86, "bass.dll");
        }
        if (!File.Exists("bassflac.dll"))
        {
            if (Environment.Is64BitProcess)
                await ReleaseDLLFiles(Properties.Resources.bassflac, "bassflac.dll");
            else await ReleaseDLLFiles(Properties.Resources.bassflac_x86, "bassflac.dll");
        }
    }
    private static async Task ReleaseDLLFiles(byte[] maindll,string name) {
        FileStream fs = new FileStream(name, FileMode.Create);
        byte[] datas = maindll;
        await fs.WriteAsync(datas);
        await fs.FlushAsync();
        fs.Close();
    }

    /// <summary>
    /// 音量 value:0~1
    /// </summary>
    public float Volume
    {
        get {
            float value = 0;
            Bass.BASS_ChannelGetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, ref value);
            return value;
        }
        set {
            if (stream != -1024)
            {
                Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, value);
                _vol = value;
            }
        }
    }
    private float _vol = 1;
    /// <summary>
    /// 从文件中加载
    /// </summary>
    /// <param name="file"></param>
    public void Load(string file)
    {
        Stop();

        stream = Bass.BASS_StreamCreateFile(file, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
        //设置音量
        Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, _vol);
    }
    public List<BASSDL> BassdlList = [];
    public Action? finish;
    /// <summary>
    /// 从URL中加载
    /// </summary>
    /// <param name="path">缓存文件路径全名称</param>
    /// <param name="url"></param>
    /// <param name="proc">下载进度回调 all/now</param>
    /// <param name="finish">下载结束回调</param>
    public void LoadUrl(string path, string url, Action<long, long>? proc , Action? finish)
    {
        this.finish = finish;
        try
        {
            Stop();

            var user = new IntPtr(BassdlList.Count);
            var Bassdl = new BASSDL(path);
            BassdlList.Add(Bassdl);
            Bassdl.ProgressChanged = proc;
            Bassdl.DownloadFinished += Bassdl_DownloadFinished; 

            stream = Bass.BASS_StreamCreateURL(url + "\r\n"
                                           + "Accept-Encoding: identity;q=1, *;q=0\r\n"
                                           + "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.66 Safari/537.36 Edg/80.0.361.40\r\n"
                                           + "Accept: */*\r\n"
                                           + "Accept-Language: zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"
                           , 0, BASSFlag.BASS_SAMPLE_FLOAT, Bassdl.DownloadDelegate, user);
            Bassdl.stream = stream;
            Debug.WriteLine("NEW: "+stream);
            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, _vol);
        }
        catch { }
    }

    private void Bassdl_DownloadFinished(BASSDL dl)
    {
        BassdlList.Remove(dl);
        if (dl.CanStop && dl.stream != stream)
        {
            Bass.BASS_ChannelStop(dl.stream);
            Bass.BASS_StreamFree(dl.stream);
        }
        if (dl.stream == stream)
            finish?.Invoke();
    }

    public void Play()
    {
        Bass.BASS_ChannelPlay(stream, false);
    }
    public void Pause()
    {
        if (stream != -1024)
            Bass.BASS_ChannelPause(stream);
    }
    public TimeSpan Duration
    {
        get
        {
            double seconds = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetLength(stream));
            return TimeSpan.FromSeconds(seconds);
        }
    }
    public TimeSpan Position
    {
        get
        {
            double seconds = Bass.BASS_ChannelBytes2Seconds(stream, Bass.BASS_ChannelGetPosition(stream));
            return TimeSpan.FromSeconds(seconds);
        }
        set => Bass.BASS_ChannelSetPosition(stream, value.TotalSeconds);
    }
    /// <summary>
    /// FFT 512
    /// </summary>
    /// <param name="fft"></param>
    public void GetFFTData(float[] fft)
    {
        Bass.BASS_ChannelGetData(stream, fft, (int)(BASSData.BASS_DATA_FFT1024 | BASSData.BASS_DATA_FFT_REMOVEDC));
    }

    /// <summary>
    /// 释放Bass解码器
    /// </summary>
    public void Free()
    {
        Bass.BASS_ChannelStop(stream);
        Bass.BASS_StreamFree(stream);
        Bass.BASS_PluginFree(bassflacHandle);
        Bass.BASS_Stop();
        Bass.BASS_Free();
    }

    /// <summary>
    /// 停止当前播放
    /// </summary>
    public void Stop()
    {
        if (stream == -1024) return;
        if (BassdlList.LastOrDefault() is { } last)
        {
            last.SetClose();
            if (last.CanStop)
            {
                Bass.BASS_ChannelStop(last.stream);
                Bass.BASS_StreamFree(last.stream);
            }
        }
    }
}

/// <summary>
/// Bass 从URL中加载并缓存的辅助类
/// </summary>
public class BASSDL
{
    private FileStream? _fs = null;
    private byte[]?  _data; // local data buffer
    private readonly string _downloadFile;
    private long _length, _downloaded;
    private string _cacheFileName;

    public DOWNLOADPROC DownloadDelegate;
    public Action<long, long>? ProgressChanged = null;
    public event Action<BASSDL>? DownloadFinished = null;
    public bool CanStop { get; private set; } = false;
    public int stream;
    public BASSDL(string file)
    {
        DownloadDelegate = new DOWNLOADPROC(DownloadCallBack);
        _downloadFile = file;
        _cacheFileName = file + ".temp";
    }
    /// <summary>
    /// 指示关闭此下载任务
    /// bass官网申明  不可停止下载   故切断下载回调 (若强制关闭则抛异常)
    /// </summary>
    public void SetClose()
    {
        ProgressChanged = null;
    }
    /// <summary>
    /// 由Bass调用   传来下载数据时 将数据保存到缓存文件中
    /// </summary>
    private void DownloadCallBack(IntPtr buffer, int length, IntPtr user)
    {
        // file length
        _length =Math.Max(_length, Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_END));
        // download progress
        _downloaded = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
        ProgressChanged?.Invoke(_length, _downloaded);

        _fs ??= File.OpenWrite(_cacheFileName);

        if (buffer == IntPtr.Zero)
        {
            // finished downloading
            _fs.Flush();
            _fs.Close();
            _fs = null;
            CanStop = true;
            FileInfo fi = new(_cacheFileName);
            if (_length > _downloaded)
            {
                //下载不完整
                fi.Delete();
            }
            else
            {
                fi.MoveTo(_downloadFile, true);
            }
            DownloadFinished?.Invoke(this);
        }
        else
        {
            // increase the data buffer as needed
            if (_data == null || _data.Length < length)
                _data = new byte[length];
            // copy from managed to unmanaged memory
            Marshal.Copy(buffer, _data, 0, length);
            // write to file
            _fs.Write(_data, 0, length);
        }
    }
}
