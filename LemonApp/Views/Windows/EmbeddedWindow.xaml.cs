using LemonApp.Common.WinAPI;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.UserControls;
using Lyricify.Lyrics.Models;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// TerminalStyleWindow.xaml 的交互逻辑
    /// </summary>
    public partial class EmbeddedWindow : Window
    {
        private readonly MediaPlayerService _mediaPlayerService;
        private readonly UIResourceService _uiResourceService;
        private readonly Timer _timer = new(100);
        public EmbeddedWindow(MainWindowViewModel mv,
                              MediaPlayerService mediaPlayer,
                              UIResourceService uiResourceService)
        {
            InitializeComponent();
            DataContext = mv;
            visualizer.Player = mediaPlayer.Player;
            _mediaPlayerService = mediaPlayer;
            mv.LyricView.OnLyricLoaded += LyricView_OnLyricLoaded;
            _mediaPlayerService.OnLoaded += MediaPlayerService_OnLoaded;
            _mediaPlayerService.OnPlay += MediaPlayerService_OnPlay;
            _mediaPlayerService.OnPaused += MediaPlayerService_OnPaused;
            _timer.Elapsed += Timer_Elapsed;
            _uiResourceService = uiResourceService;
            uiResourceService.OnColorModeChanged += UiResourceService_OnColorModeChanged;
            Closing += delegate
            {
                mv.LyricView.OnLyricLoaded -= LyricView_OnLyricLoaded;
                _mediaPlayerService.OnLoaded -= MediaPlayerService_OnLoaded;
                _mediaPlayerService.OnPlay -= MediaPlayerService_OnPlay;
                _mediaPlayerService.OnPaused -= MediaPlayerService_OnPaused;
                _uiResourceService.OnColorModeChanged -= UiResourceService_OnColorModeChanged;
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
            };
            Loaded += TerminalStyleWindow_Loaded;
            SourceInitialized += EmbeddedWindow_SourceInitialized;
        }
        private static readonly SolidColorBrush NormalLrcColor_Light = new(Color.FromArgb(0x99,255,255,255));
        private static readonly SolidColorBrush NormalLrcColor_Dark = new(Color.FromArgb(0x99,0,0,0));
        private void UiResourceService_OnColorModeChanged()
        {
            Resources["InActiveLrcForeground"] = _uiResourceService.GetIsDarkMode() ? NormalLrcColor_Light : NormalLrcColor_Dark;
        }

        private void EmbeddedWindow_SourceInitialized(object? sender, System.EventArgs e)
        {
            DesktopWindowHelper.EmbedWindowToDesktop(this);
            //将窗口居中靠下
            var sc = SystemParameters.WorkArea;
            Width = sc.Width * 2 / 3;
            Height = sc.Height * 2 / 3;
            Left = (sc.Right - Width) / 2;
            Top = sc.Height - Height - 80;
            //配置歌词颜色
            UiResourceService_OnColorModeChanged();
        }

        private void MediaPlayerService_OnLoaded(MusicLib.Abstraction.Entities.Music obj)
        {
            Dispatcher.Invoke(lv.Reset);
        }

        private void LyricView_OnLyricLoaded((LyricsData? lrc, LyricsData? trans, LyricsData? romaji, bool isPureLrc) model)
        {
            if(model.lrc == null) return;
            Dispatcher.Invoke(() =>
            {
                lv.Load(model.lrc, model.trans, model.romaji, model.isPureLrc);
                lv.ApplyFontSize(56, 0.6);
                
            });
        }

        private void MediaPlayerService_OnPaused(MusicLib.Abstraction.Entities.Music obj)
        {
            _timer.Stop();
        }

        private void MediaPlayerService_OnPlay(MusicLib.Abstraction.Entities.Music obj)
        {
            _timer.Start();
        }

        private void TerminalStyleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayerService.CurrentMusic is { } m)
                _ = LoadFromMusic(m);
            if (_mediaPlayerService.IsPlaying && !_timer.Enabled)
                _timer.Start();
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

        public async Task LoadFromMusic(MusicLib.Abstraction.Entities.Music m)
        {
            Dispatcher.Invoke(lv.Reset);
            if (await LyricView.LoadLyricForMusic(m) is { } dt )
            {
                var model = LyricView.LoadLrc(dt);
                if (model.lrc == null) return;
                Dispatcher.Invoke(() =>
                {
                    lv.Load(model.lrc, model.trans, model.romaji, model.isPureLrc);
                    lv.ApplyFontSize(56, 0.6);
                });
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
