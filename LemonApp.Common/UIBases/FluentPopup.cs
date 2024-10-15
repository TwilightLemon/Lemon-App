using LemonApp.Common.WinAPI;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace LemonApp.Common.UIBases;

public static class FluentTooltip
{
    public static bool GetUseFluentStyle(DependencyObject obj)
    {
        return (bool)obj.GetValue(UseFluentStyleProperty);
    }

    public static void SetUseFluentStyle(DependencyObject obj, bool value)
    {
        obj.SetValue(UseFluentStyleProperty, value);
    }

    public static readonly DependencyProperty UseFluentStyleProperty =
        DependencyProperty.RegisterAttached("UseFluentStyle",
            typeof(bool), typeof(FluentTooltip),
            new PropertyMetadata(false,OnUseFluentStyleChanged));
    public static void OnUseFluentStyleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != e.OldValue)
        {
            if(obj is ToolTip tip)
            {
                if ((bool)e.NewValue)
                {
                    tip.Opened += Tip_Opened;
                }
                else
                {
                    tip.Opened -= Tip_Opened;
                }
            }
        }
    }

    private static void Tip_Opened(object sender, RoutedEventArgs e)
    {
        if(sender is ToolTip tip&& tip.Background is SolidColorBrush cb)
        {
            var hwnd = tip.GetNativeWindowHwnd();
            FluentPopupFunc.SetPopupWindowMaterial(hwnd, cb.Color);
        }
    }
}

public class FluentPopup:Popup
{
    public FluentPopup()
    {
        Opened += FluentPopup_Opened;
    }
    public SolidColorBrush Background
    {
        get { return (SolidColorBrush)GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register("Background",
            typeof(SolidColorBrush), typeof(FluentPopup),
            new PropertyMetadata(Brushes.Transparent,OnBackgroundChanged));

    public static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FluentPopup popup)
        {
            popup.ApplyFluentHwnd();
        }
    }

    private IntPtr _windowHandle= IntPtr.Zero;
    private void FluentPopup_Opened(object? sender, EventArgs e)
    {
        _windowHandle = this.GetNativeWindowHwnd();
        ApplyFluentHwnd();
    }
    public void ApplyFluentHwnd()
    {
        FluentPopupFunc.SetPopupWindowMaterial(_windowHandle, Background.Color);
    }
}

internal static class FluentPopupFunc
{
    public const BindingFlags privateInstanceFlag = BindingFlags.NonPublic | BindingFlags.Instance;
    public static IntPtr GetNativeWindowHwnd(this ToolTip tip)
    {
        var field=tip.GetType().GetField("_parentPopup", privateInstanceFlag);
        if (field != null)
        {
            if(field.GetValue(tip) is Popup{ } popup)
            {
                return popup.GetNativeWindowHwnd();
            }
        }
        return IntPtr.Zero;
    }
    public static IntPtr GetNativeWindowHwnd(this Popup popup)
    {
        var field = typeof(Popup).GetField("_secHelper", privateInstanceFlag);
        if (field != null)
        {
            if (field.GetValue(popup) is { } _secHelper)
            {
                if (_secHelper.GetType().GetProperty("Handle", privateInstanceFlag) is { } prop)
                {
                    if (prop.GetValue(_secHelper) is IntPtr handle)
                    {
                        return handle;
                    }
                }
            }
        }
        return IntPtr.Zero;
    }
    public static void SetPopupWindowMaterial(IntPtr hwnd,Color compositionColor)
    {
        if (hwnd != IntPtr.Zero)
        {
            int hexColor = compositionColor.ToHexColor();
            var hwndSource = HwndSource.FromHwnd(hwnd);
            MaterialApis.SetWindowProperties(hwndSource, 0);
            MaterialApis.SetWindowComposition(hwnd, true, hexColor);
            MaterialApis.SetWindowCorner(hwnd, MaterialApis.WindowCorner.Round);
        }
    }
}
