using LemonApp.Common.WinAPI;
using LemonApp.Services;
using LemonApp.Views.UserControls;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace LemonApp.Components;

/// <summary>
/// Basic Components for MainWindow, including TaskBar, NotifyIcon, Singleton Wakeup, Hot Keys(TODO), etc.
/// </summary>
public class WindowBasicComponent(IServiceProvider serviceProvider,
    MediaPlayerService mediaPlayerService):IDisposable
{
    public event Action<string?>? OnCopyDataReceived;
    public void Dispose()
    {
        TaskBarImg?.Dispose();
        TaskBarBtn_Last?.Dispose();
        TaskBarBtn_Play?.Dispose();
        TaskBarBtn_Next?.Dispose();
        NotifyIcon?.Dispose();
    }
    /// <summary>
    /// 由ApplicationService在MainWindow创建完成后调用
    /// </summary>
    public void Init()
    {
//#if !DEBUG
        InitTaskBarThumb();
//#endif
        FixPopup();
        InitNotifyIcon();
        RegisterWakeup();
        SaveHwnd();
    }

    private static void FixPopup()
    {
        var ifLeft = SystemParameters.MenuDropAlignment;
        if (ifLeft)
        {
            var t = typeof(SystemParameters);
            var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, false);
        }
    }

    #region Task Bar Thumb
    TabbedThumbnail? TaskBarImg;
    ThumbnailToolBarButton? TaskBarBtn_Last;
    ThumbnailToolBarButton? TaskBarBtn_Play;
    ThumbnailToolBarButton? TaskBarBtn_Next;

    static readonly System.Drawing.Icon? icon_play= Properties.Resources.play,
        icon_pause= Properties.Resources.pause,
        icon_last = Properties.Resources.left,
        icon_next = Properties.Resources.right, 
        icon_app= Properties.Resources.icon;

    private void InitTaskBarThumb()
    {
        var _window = App.Current.MainWindow;
        TaskBarImg = new(_window, _window, new Vector());
        TaskBarImg.Title = "Lemon App";
        TaskBarImg.SetWindowIcon(Properties.Resources.icon);
        TaskBarImg.TabbedThumbnailActivated += delegate
        {
            _window.WindowState = WindowState.Normal;
            _window.Activate();
        };
        TaskBarBtn_Last = new(icon_last, "上一曲");
        TaskBarBtn_Last.Enabled = true;
        TaskBarBtn_Last.Click += delegate {
            mediaPlayerService.PlayNext();
        };

        TaskBarBtn_Play = new(icon_play, "播放|暂停");
        TaskBarBtn_Play.Enabled = true;
        TaskBarBtn_Play.Click += delegate {
            if (mediaPlayerService.IsPlaying) 
                mediaPlayerService.Pause();
            else mediaPlayerService.Play();
        };

        TaskBarBtn_Next = new(icon_next, "下一曲");
        TaskBarBtn_Next.Enabled = true;
        TaskBarBtn_Next.Click += delegate {
            mediaPlayerService.PlayNext();
        };

        TaskbarManager.Instance.TabbedThumbnail.AddThumbnailPreview(TaskBarImg);
        TaskbarManager.Instance.ThumbnailToolBars.AddButtons(_window, TaskBarBtn_Last, TaskBarBtn_Play, TaskBarBtn_Next);
    }

    /// <summary>
    /// 更新任务栏缩略图按钮状态, 由MainWindowViewModel调用
    /// </summary>
    public void UpdateThumbButtonState()
    {
        if (TaskBarBtn_Play != null)
            TaskBarBtn_Play.Icon = mediaPlayerService.IsPlaying ? icon_pause : icon_play;
    }

    /// <summary>
    /// 更新任务栏缩略图信息, 由MainWindowViewModel调用
    /// </summary>
    /// <param name="cover"></param>
    public void UpdateThumbInfo(System.Drawing.Bitmap cover)
    {
        if (cover == null || TaskBarImg == null) return;
        TaskBarImg.SetImage(cover);
        TaskBarImg.Tooltip = mediaPlayerService.CurrentMusic?.MusicName + " - " + mediaPlayerService.CurrentMusic?.SingerText;
    }
    #endregion
    #region NotifyIcon
    System.Windows.Forms.NotifyIcon? NotifyIcon;
    private void InitNotifyIcon()
    {
        NotifyIcon = new()
        {
            Icon = icon_app,
            Text = "Lemon App",
            Visible = true
        };
        NotifyIcon.MouseClick += NotifyIcon_MouseClick;
        NotifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
    }

    private void NotifyIcon_MouseDoubleClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        var _window = App.Current.MainWindow;
        _window.ShowWindow();
    }
    private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Right)
        {
            if (serviceProvider.GetRequiredService<NotifyIconMenuWindow>() is { } menu)
            {
                //计算窗口弹出的坐标
                var point = System.Windows.Forms.Cursor.Position;
                var dpi = VisualTreeHelper.GetDpi(App.Current.MainWindow);
                menu.Left = point.X / dpi.DpiScaleX;
                menu.Top = point.Y / dpi.DpiScaleY - menu.Height;

                menu.Show();
                menu.Activate();
            }
        }
    }
    #endregion
    #region Wakeup
    private static async void SaveHwnd()
    {
        //save window handle for MsgInteraction
        if (EntryPoint._procMgr is { } procMgr)
        {
            var hwnd = new WindowInteropHelper(App.Current.MainWindow).Handle;
            procMgr.Data = new() { MainWindowHandle = hwnd.ToInt32() };
            await procMgr.SaveAsync();
        }
    }
    private void RegisterWakeup()
    {
        HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(App.Current.MainWindow).Handle);
        source.AddHook(WndProc);
    }
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == MsgInteraction.WM_COPYDATA)
        {
            var msgStr = MsgInteraction.HandleMsg(lParam);
            if (msgStr == MsgInteraction.SEND_SHOW)
            {
                App.Current.MainWindow.ShowWindow();
            }
            OnCopyDataReceived?.Invoke(msgStr);
        }
        return IntPtr.Zero;
    }
    #endregion
}
