using EleCho.WpfSuite;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Common.WinAPI;
using LemonApp.Services;
using LemonApp.ViewModels;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// DesktopLyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DesktopLyricWindow : Window
    {
        public DesktopLyricWindow(DesktopLyricWindowViewModel vm, AppSettingService appSettingsService)
        {
            InitializeComponent();
            DataContext = vm;
            vm.UpdateAnimation = ShowLyricAnimation;
            vm.ScrollLrc = ScrollLrc;
            _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>();

            var sc = SystemParameters.WorkArea;
            Top = sc.Bottom - Height;
            Left = (sc.Right - Width) / 2;
            Closing += delegate {
                vm.UpdateAnimation = null; 
                _settingsMgr.Data.WindowSize = new Size(Width, Height);
            };
            Loaded += DesktopLyricWindow_Loaded;
            MouseEnter += DesktopLyricWindow_MouseEnter;
            MouseLeave += DesktopLyricWindow_MouseLeave;
            MouseDoubleClick += DesktopLyricWindow_MouseDoubleClick;
        }
        private FrameworkElement? currentBlock = null;
        private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;
        private void ScrollLrc(FrameworkElement block)
        {
            if (block == currentBlock ||block ==null)
            {
                return;
            }
            var position=block.TransformToVisual(LrcHost);
            Point p = position.Transform(new Point(0, 0));
            Debug.WriteLine(p.X);
            LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty,
                    new DoubleAnimation(p.X  - LrcScrollViewer.ViewportWidth *0.4, TimeSpan.FromMilliseconds(500)));
            currentBlock = block;
        }

        private void ShowLyricAnimation()
        {
            var blur=new BlurEffect() { Radius = 0 };
            LrcHost.Effect = blur;
            LrcHost.BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames()
            {
                KeyFrames = [new LinearDoubleKeyFrame(0,TimeSpan.FromMilliseconds(200)),
                                      new LinearDoubleKeyFrame(0,TimeSpan.FromMilliseconds(300)),
                                      new LinearDoubleKeyFrame(1,TimeSpan.FromMilliseconds(500))]
            });
            blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimationUsingKeyFrames()
            {
                KeyFrames = [new LinearDoubleKeyFrame(20,TimeSpan.FromMilliseconds(200)),
                                      new LinearDoubleKeyFrame(20,TimeSpan.FromMilliseconds(300)),
                                      new LinearDoubleKeyFrame(0,TimeSpan.FromMilliseconds(500))]
            });
            new Action(async() => {
                await Task.Delay(200);
                LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);
                ScrollViewerUtils.SetHorizontalOffset(LrcScrollViewer, 0);
            })();
        }

        private void DesktopLyricWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sc = SystemParameters.WorkArea;
            Left = (sc.Right - Width) / 2;
        }
        private readonly DropShadowEffect shadowEffect = new() { BlurRadius = 5, Direction = 0, ShadowDepth = 0 };
        private void DesktopLyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            cancelShowFunc?.Cancel();
            cancelShowFunc = null;
            preShowFunc = false;
            LrcPanel.Effect = shadowEffect;
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


        private void DesktopLyricWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLongAPI.SetToolWindow(this);
            LrcPanel.Effect = shadowEffect;

            if(_settingsMgr.Data.WindowSize is { Width:>0,Height:>0} size)
            {
                Width = size.Width;
                Height = size.Height;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
