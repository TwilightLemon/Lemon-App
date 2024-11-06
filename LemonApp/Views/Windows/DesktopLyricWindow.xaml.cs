using LemonApp.Common.WinAPI;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media;
using LemonApp.ViewModels;
using System;
using System.Windows.Input;
using LemonApp.Services;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Threading;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// DesktopLyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyricWindow : Window
    {
        public DesktopLyricWindow(DesktopLyricWindowViewModel vm,UIResourceService uiResourceService)
        {
            InitializeComponent();
            DataContext = vm;
            _uiResourceService = uiResourceService;
            _uiResourceService.OnColorModeChanged += _uiResourceService_OnColorModeChanged;

            var sc = SystemParameters.WorkArea;
            Top = sc.Bottom - Height;
            Left = (sc.Right - Width) / 2;
            Loaded += DesktopLyricWindow_Loaded;
            Closed += DesktopLyricWindow_Closed;
            MouseEnter += DesktopLyricWindow_MouseEnter;
            MouseLeave += DesktopLyricWindow_MouseLeave;
            MouseDoubleClick += DesktopLyricWindow_MouseDoubleClick;
        }

        private void DesktopLyricWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sc = SystemParameters.WorkArea;
            Left = (sc.Right - Width) / 2;
        }

        private void _uiResourceService_OnColorModeChanged()
        {
            UpdateColorMode();
        }

        private readonly UIResourceService _uiResourceService;

        private void DesktopLyricWindow_Closed(object? sender, EventArgs e)
        {
            _uiResourceService.OnColorModeChanged -= _uiResourceService_OnColorModeChanged;
        }

        private void DesktopLyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            cancelShowFunc?.Cancel();
            cancelShowFunc = null;
            preShowFunc = false;
            LrcPanel.Effect = null;
            LrcPanel.BeginAnimation(OpacityProperty, null);
            FuncPanel.Visibility = Visibility.Collapsed;
            FuncPanel.BeginAnimation(OpacityProperty, null);
        }
        bool preShowFunc = false;
        CancellationTokenSource? cancelShowFunc = null;
        private async void DesktopLyricWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            preShowFunc = true;
            cancelShowFunc ??= new();
            try
            {
                await Task.Delay(300, cancelShowFunc.Token);
                if (preShowFunc)
                {
                    LrcPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.6, TimeSpan.FromMilliseconds(100)));
                    LrcPanel.Effect = new BlurEffect() { Radius = 12 };
                    FuncPanel.Visibility = Visibility.Visible;
                    FuncPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                }
            }
            catch { }
        }

        private void UpdateColorMode()
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

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
