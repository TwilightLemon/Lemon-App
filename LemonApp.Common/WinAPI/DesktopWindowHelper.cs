using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
namespace LemonApp.Common.WinAPI;
public static class DesktopWindowHelper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const uint WM_SPAWN_WORKERW = 0x052C;
    private const uint SMTO_NORMAL = 0x0000;

    private static IntPtr _desktopHandle = IntPtr.Zero;

    public static IntPtr EmbedWindowToDesktop(Window window)
    {
        IntPtr progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);

        //查找Progman下面的WorkerW窗口
        IntPtr workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
        //查找SHELLDLL_DefView窗口
        IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if(workerW != IntPtr.Zero&&shellView!=IntPtr.Zero)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            IntPtr we = FindWindowEx(workerW, IntPtr.Zero, "WPEVideoWallpaper", null);
            if(we==IntPtr.Zero)we=FindWindowEx(workerW, IntPtr.Zero, "WPELiveWallpaper", null);
            if (we != IntPtr.Zero)
            {
                SetParent(hwnd, we);
            }
            else SetParent(hwnd, shellView);
        }

        return _desktopHandle;
    }
}
