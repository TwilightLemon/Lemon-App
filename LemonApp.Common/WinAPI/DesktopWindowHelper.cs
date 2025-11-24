using System.Runtime.InteropServices;
using System.Windows;
using FluentWpfCore.Helpers;
namespace LemonApp.Common.WinAPI;
public static class DesktopWindowHelper
{
    public static bool EmbedWindowToDesktop(Window window)
    {
        WindowFlagsHelper.SetToolWindow(window);
        return true;
    }
}