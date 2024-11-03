using System.IO;
using System.Runtime.InteropServices;
using Un4seen.Bass;
/*
使用Bass解码器 还是自带的mediaplayer呢？
 */
namespace LemonApp.Common.Funcs;

/// <summary>
/// 封装bass.dll 的音乐播放器
/// </summary>
public class MusicPlayer
{
    private int stream = -1024;
    public MusicPlayer()
    {
        BassNet.Registration("lemon.app@qq.com", "2X52325160022");
        Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, IntPtr.Zero);
    }
    /// <summary>
    /// 根据平台释放解码器dll
    /// </summary>
    /// <returns></returns>
    public static async Task PrepareDll() 
    {
        if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\bass.dll") || !File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\bass_fx.dll"))
        {
            if (Environment.Is64BitProcess)
                await ReleaseDLLFiles(Properties.Resources.bass);
            else await ReleaseDLLFiles(Properties.Resources.bass_x86);
        }
    }
    private static async Task ReleaseDLLFiles(byte[] maindll) {
        FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "\\bass.dll", FileMode.Create);
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
    /// <summary>
    /// 从URL中加载
    /// </summary>
    /// <param name="path">缓存文件路径全名称</param>
    /// <param name="url"></param>
    /// <param name="proc">下载进度回调 all/now</param>
    /// <param name="finish">下载结束回调</param>
    public void LoadUrl(string path, string url, Action<long, long>? proc, Action? finish)
    {
        try
        {
            Stop();

            var user = new IntPtr(BassdlList.Count);
            var Bassdl = new BASSDL(path);
            BassdlList.Add(Bassdl);
            Bassdl.ProgressChanged = proc;
            Bassdl.DownloadSucceeded = (dl) => {
                BassdlList.Remove(dl);
                finish?.Invoke();
            };
            Bassdl.DownloadFailed = (dl) => {
                BassdlList.Remove(dl);
            };

            stream = Bass.BASS_StreamCreateURL(url + "\r\n"
                                           + "Accept-Encoding: identity;q=1, *;q=0\r\n"
                                           + "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.66 Safari/537.36 Edg/80.0.361.40\r\n"
                                           + "Accept: */*\r\n"
                                           + "Accept-Language: zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"
                           , 0, BASSFlag.BASS_SAMPLE_FLOAT, Bassdl.DownloadDelegate, user);
            Bassdl.stream = stream;

            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, _vol);
        }
        catch { }
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
    /// 获取FFT数据   可以用来做频谱
    /// </summary>
    /// <returns></returns>
    public float[] GetFFTData()
    {
        float[] fft = new float[128];
        Bass.BASS_ChannelGetData(stream, fft, (int)BASSData.BASS_DATA_FFT256);
        return fft;
    }
    public void GetFFTDataRef(ref float[] fft)
    {
        Bass.BASS_ChannelGetData(stream, fft, (int)BASSData.BASS_DATA_FFT1024);
    }
    /// <summary>
    /// 更新设备
    /// </summary>
    public void UpdateDevice()
    {
        var data = Bass.BASS_GetDeviceInfos();
        int index = -1;
        for (int i = 0; i < data.Length; i++)
            if (data[i].IsDefault)
            {
                index = i;
                break;
            }
        if (!data[index].IsInitialized)
            Bass.BASS_Init(index, 44100, BASSInit.BASS_DEVICE_CPSPEAKERS, IntPtr.Zero);
        var a = Bass.BASS_ChannelGetDevice(stream);
        if (a != index)
        {
            Bass.BASS_ChannelSetDevice(stream, index);
            Bass.BASS_SetDevice(index);
        }
    }
    /// <summary>
    /// 释放Bass解码器
    /// </summary>
    public void Free()
    {
        Bass.BASS_ChannelStop(stream);
        Bass.BASS_StreamFree(stream);
        Bass.BASS_Stop();
        Bass.BASS_Free();
    }

    /// <summary>
    /// 停止当前播放
    /// </summary>
    public void Stop()
    {
        if (stream == -1024) return;
        Bass.BASS_ChannelStop(stream);
        Bass.BASS_StreamFree(stream);
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
    private bool _isStopped = false;
    private long _length, _downloaded;
    private string _cacheFileName;

    public DOWNLOADPROC DownloadDelegate;
    public Action<long, long>? ProgressChanged = null;
    public Action<BASSDL>? DownloadSucceeded = null;
    public Action<BASSDL>? DownloadFailed = null;
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
        _isStopped = true;
        ProgressChanged = null;
        DownloadSucceeded = null;
    }
    /// <summary>
    /// 由Bass调用   传来下载数据时 将数据保存到缓存文件中
    /// </summary>
    private void DownloadCallBack(IntPtr buffer, int length, IntPtr user)
    {
        // file length
        _length = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_END);
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
            FileInfo fi = new(_cacheFileName);
            if (_length>_downloaded)
            {
                fi.Delete();
                if (!_isStopped)
                {
                    //没有被停止而是链接下载失败
                    DownloadFailed?.Invoke(this);
                }
            }
            else
            {
                fi.MoveTo(_downloadFile, true);
                DownloadSucceeded?.Invoke(this);
            }
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
