using LemonApp.Common.WinAPI;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media;
using LemonApp.ViewModels;
using System;
using System.Windows.Input;
using LemonApp.Services;

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
            Deactivated += DesktopLyricWindow_Deactivated;
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

        private void DesktopLyricWindow_Deactivated(object? sender, EventArgs e)
        {
            LrcPanel.Effect = null;
        }

        private void DesktopLyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            LrcPanel.Effect = null;
        }

        private void DesktopLyricWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            LrcPanel.Effect = new BlurEffect() { Radius = 12 };
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

        private void GoCenterBtn_Click(object sender, RoutedEventArgs e)
        {
            var sc = SystemParameters.WorkArea;
            Left = (sc.Right - Width) / 2;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
