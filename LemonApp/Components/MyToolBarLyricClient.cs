using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Views.UserControls;
using Microsoft.Extensions.Hosting;

namespace LemonApp.Components;

/// <summary>
/// Lyric interaction with MyToolBar
/// </summary>
/// <param name="lyricView"></param>
/// <param name="wbc"></param>
public class MyToolBarLyricClient(LyricView lyricView, WindowBasicComponent wbc) : IHostedService
{
    private TcpClient? client;
    private readonly CancellationTokenSource cts = new();
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lyricView.OnNextLrcReached += LyricView_OnNextLrcReached;
        wbc.OnCopyDataReceived += Wbc_OnCopyDataReceived;
        return Task.CompletedTask;
    }
    //connect after startup or called by MyToolBar
    public void Connect()
    {
        Task.Run(() =>
        {
            try
            {
                client = new("127.0.0.1", 12587);
            }
            catch { }
        });
    }
    public const string INTERACT_MTB_SYNC = "INTERACT_MTB_SYNC";
    private void Wbc_OnCopyDataReceived(string? msg)
    {
        if (msg == INTERACT_MTB_SYNC)
        {
            //reconnect called by MyToolBar
            client?.Dispose();
            client = null;
            Connect();
        }
    }

    private async void LyricView_OnNextLrcReached(LrcLine lrc,LrcLine? next)
    {
        if (client != null && client.Connected)
        {
            try
            {
                var msg = lrc.Lyric + "\r\n";
                var data = Encoding.UTF8.GetBytes(msg);
                var stream = client.GetStream();
                await stream.WriteAsync(data, cts.Token);
                await stream.FlushAsync(cts.Token);
            }
            catch { }
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        cts.Cancel();
        lyricView.OnNextLrcReached -= LyricView_OnNextLrcReached;
        wbc.OnCopyDataReceived -= Wbc_OnCopyDataReceived;
        client?.Dispose();
        return Task.CompletedTask;
    }
}