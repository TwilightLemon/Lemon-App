using LemonApp.Common.UIBases;
using LemonApp.MusicLib.Lyric;
using LemonApp.Services;
using LemonApp.ViewModels;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Timers;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// TerminalStyleWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalStyleWindow : FluentWindowBase
    {
        public TerminalStyleWindow(MainWindowViewModel mv,MediaPlayerService mediaPlayer)
        {
            InitializeComponent();
            DataContext = mv;
            visualizer.Player = mediaPlayer.Player;
            _mediaPlayerService = mediaPlayer;
            _mediaPlayerService.OnLoaded += MediaPlayerService_OnLoaded;
            _mediaPlayerService.OnPlay += (m) => _timer.Start();
            _mediaPlayerService.OnPaused += (m) => _timer.Stop();
            _timer.Elapsed += Timer_Elapsed;
            Closing += delegate {
                _mediaPlayerService.OnLoaded -= MediaPlayerService_OnLoaded;
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            };
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_mediaPlayerService.IsPlaying && _mediaPlayerService.CurrentMusic!=null)
            {
                Dispatcher.Invoke(() =>
                {
                    lv.UpdateTime((int)_mediaPlayerService.Player.Position.TotalMilliseconds);
                });
            }
        }

        private readonly MediaPlayerService _mediaPlayerService;
        private readonly Timer _timer = new(100);
        private async void MediaPlayerService_OnLoaded(MusicLib.Abstraction.Entities.Music m)
        {
            if (m.Source == MusicLib.Abstraction.Entities.Platform.qq)
            {
                var hc=App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.PublicClientFlag);
                var dt = await TencGetLyric.GetLyricsAsync(hc,m.Littleid);
                if (dt?.Lyrics == null) return;
                var lrc=ParseHelper.ParseLyrics(dt.Lyrics, LyricsRawTypes.Qrc);

                LyricsData? trans=null,romaji=null;
                if(!string.IsNullOrEmpty(dt.Trans))
                    trans = ParseHelper.ParseLyrics(dt.Trans, LyricsRawTypes.Lrc);
                if(!string.IsNullOrEmpty(dt.Romaji))
                    romaji = ParseHelper.ParseLyrics(dt.Romaji, LyricsRawTypes.Qrc);

                if (lrc != null)
                    Dispatcher.Invoke(()=>lv.Load(lrc,trans,romaji));
            }
        }

    }
}
