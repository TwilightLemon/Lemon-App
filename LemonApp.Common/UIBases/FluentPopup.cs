using LemonApp.Common.WinAPI;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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
            FluentPopupFunc.SetPopupWindowMaterial(hwnd, cb.Color,MaterialApis.WindowCorner.RoundSmall);
        }
    }
}

public class FluentPopup:Popup
{
    public enum ExPopupAnimation
    {
        None,
        SlideUp,
        SlideDown
    }
    private DoubleAnimation? _slideAni;
    public FluentPopup()
    {
        Opened += FluentPopup_Opened;
        Closed += FluentPopup_Closed;
    }

    #region 启动动画控制
    private void FluentPopup_Closed(object? sender, EventArgs e)
    {
        ResetAnimation();
    }

    public new bool IsOpen { get => base.IsOpen;
        set 
        {
            if (value)
            {
                BuildAnimation();
                base.IsOpen = value;
                // Run Animation in Opened Event
            }
            else
            {
                base.IsOpen = value;
                ResetAnimation();
            }
        }
    }
    public uint SlideAnimationOffset { get; set; } = 50;
    private void ResetAnimation()
    {
        if (ExtPopupAnimation is ExPopupAnimation.SlideUp or ExPopupAnimation.SlideDown)
        {
            BeginAnimation(VerticalOffsetProperty, null);
            VerticalOffset -= ExtPopupAnimation == ExPopupAnimation.SlideUp ? SlideAnimationOffset : -SlideAnimationOffset;
        }
    }
    public void BuildAnimation()
    {
        if (ExtPopupAnimation is ExPopupAnimation.SlideUp or ExPopupAnimation.SlideDown)
        {
            _slideAni = new DoubleAnimation(VerticalOffset, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
            VerticalOffset += ExtPopupAnimation ==ExPopupAnimation.SlideUp ? SlideAnimationOffset : -SlideAnimationOffset;
        }
    }
    public void RunPopupAnimation()
    {
        if (_slideAni != null)
        {
            BeginAnimation(VerticalOffsetProperty, _slideAni);
        }
    }

    #endregion
    #region Fluent Style
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

    public ExPopupAnimation ExtPopupAnimation
    {
        get { return (ExPopupAnimation)GetValue(ExtPopupAnimationProperty); }
        set { SetValue(ExtPopupAnimationProperty, value); }
    }

    public static readonly DependencyProperty ExtPopupAnimationProperty =
        DependencyProperty.Register("ExtPopupAnimation", typeof(ExPopupAnimation), typeof(FluentPopup),
            new PropertyMetadata(ExPopupAnimation.None));

    private IntPtr _windowHandle= IntPtr.Zero;
    private void FluentPopup_Opened(object? sender, EventArgs e)
    {
        _windowHandle = this.GetNativeWindowHwnd();
        ApplyFluentHwnd();
        Dispatcher.Invoke(RunPopupAnimation);
    }
    public void ApplyFluentHwnd()
    {
        FluentPopupFunc.SetPopupWindowMaterial(_windowHandle, Background.Color);
    }
    #endregion
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
    public static void SetPopupWindowMaterial(IntPtr hwnd,Color compositionColor,
        MaterialApis.WindowCorner corner=MaterialApis.WindowCorner.Round)
    {
        if (hwnd != IntPtr.Zero)
        {
            int hexColor = compositionColor.ToHexColor();
            var hwndSource = HwndSource.FromHwnd(hwnd);
            MaterialApis.SetWindowProperties(hwndSource, 0);
            MaterialApis.SetWindowComposition(hwnd, true, hexColor);
            MaterialApis.SetWindowCorner(hwnd, corner);
        }
    }
}
