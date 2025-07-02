using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static LemonApp.Common.WinAPI.WindowLongAPI;
namespace LemonApp.Common.WinAPI;
public static class DesktopWindowHelper
{
    // 设置窗口位置、Z顺序、大小、显示状态
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    public static readonly IntPtr HWND_BOTTOM = new(1);
    public const uint SWP_NOACTIVATE = 0x0010; // 不激活窗口（避免抢焦点）
    public const uint SWP_SHOWWINDOW = 0x0040; // 显示窗口（如果尚未可见）

    public static bool EmbedWindowToDesktop(Window window)
    {
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
       int exStyle= (int)WS.WS_POPUP|(int)WS.WS_VISIBLE;
        SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_STYLE, (IntPtr)exStyle);
        WindowLongAPI.SetToolWindow(window);
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, (int)window.ActualWidth, (int)window.ActualHeight,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
        return true;
    }
}