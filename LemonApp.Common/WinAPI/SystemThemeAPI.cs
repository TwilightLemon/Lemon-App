using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace LemonApp.Common.WinAPI
{
    /// <summary>
    /// 注册系统主题颜色改变事件和获取主题颜色的方法集
    /// </summary>
    public static class SystemThemeAPI
    {
        public static bool GetIsLightTheme()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int { } v)
                {
                    return v > 0;
                }
            }

            return true; // 默认为浅色模式
        }
        static Windows.UI.ViewManagement.UISettings? _uiSettings = null;
        public static Color GetSystemAccentColor()
        {
            _uiSettings ??= new ();
            var color = _uiSettings.GetColorValue(GetIsLightTheme()?Windows.UI.ViewManagement.UIColorType.AccentDark1: Windows.UI.ViewManagement.UIColorType.AccentLight2);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        static Action? _onThemeChanged = null;
        static Action? _onSystemColorChanged = null;
        public static void RegesterOnThemeChanged(Window window,Action onThemeChanged,Action onSystemColorChanged)
        {
            _onThemeChanged = onThemeChanged;
            _onSystemColorChanged = onSystemColorChanged;
            var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source?.AddHook(WndProc);
        }

        public static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SETTINGCHANGE)
            {
                _onThemeChanged?.Invoke();
                handled = true;
            }else if(msg== WM_DWMCOLORIZATIONCOLORCHANGED)
            {
                _onSystemColorChanged?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
