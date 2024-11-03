using LemonApp.Common.WinAPI;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// DesktopLyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyricWindow : Window
    {
        public DesktopLyricWindow()
        {
            InitializeComponent();
            var sc = SystemParameters.WorkArea;
            Top = sc.Bottom - Height;
            Left = (sc.Right - Width) / 2;
            Loaded += DesktopLyricWindow_Loaded;
        }
        public void UpdateColorMode()
        {
            if (Foreground is SolidColorBrush color)
            {
                LrcTb.Effect = new DropShadowEffect() { BlurRadius = 20, Color = color.Color, Opacity = 0.5, ShadowDepth = 0, Direction = 0 };
            }
        }

        private void DesktopLyricWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLongAPI.SetToolWindow(this);
            UpdateColorMode();
        }

        public void Update(MusicLib.Abstraction.Lyric.DataTypes.LrcLine lrc)
        {
            if (!string.IsNullOrEmpty(lrc.Trans))
            {
                LyricTb.Text = lrc.Lyric + "\r\n";
            }
            else
            {
                LyricTb.Text = lrc.Lyric;
            }
            TransTb.Text = lrc.Trans;
        }

        private void GoCenterBtn_Click(object sender, RoutedEventArgs e)
        {
            var sc = SystemParameters.WorkArea;
            Left = (sc.Right - Width) / 2;
        }
    }
}
