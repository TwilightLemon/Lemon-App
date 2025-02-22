using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Common.UIBases;

public static class RippleClickEffect
{
    public static bool GetIsEnable(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnableProperty);
    }

    public static void SetIsEnable(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnableProperty, value);
    }

    public static readonly DependencyProperty IsEnableProperty =
        DependencyProperty.RegisterAttached("IsEnable", typeof(bool), typeof(RippleClickEffect),
            new PropertyMetadata(false,OnIsEnableChanged));

    public static void OnIsEnableChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        if (o is Control element)
        {
            if ((bool)e.NewValue)
            {
                element.PreviewMouseDown += Element_PreviewMouseDown;
            }
            else
            {
                element.PreviewMouseDown -= Element_PreviewMouseDown;
            }
        }
    }

    private static void Element_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Control uiElement)
        {
            var center = e.GetPosition(uiElement);
            var adornerLayer = AdornerLayer.GetAdornerLayer(uiElement);
            if (adornerLayer == null) return;
            var ripple = new RippleEffect(uiElement)
            {
                Center = center,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut },
                InnerBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0))
            };

            ripple.Completed += (s, e) =>
            {
                adornerLayer.Remove(ripple);
            };

            adornerLayer.Add(ripple);
            ripple.Play();
        }
    }
}
public class RippleEffect : Adorner
{
    private readonly UIElement _adornedElement;
    private Geometry? _cachedClip;

    public Point Center
    {
        get { return (Point)GetValue(CenterProperty); }
        set { SetValue(CenterProperty, value); }
    }

    public TimeSpan Duration
    {
        get { return (TimeSpan)GetValue(DurationProperty); }
        set { SetValue(DurationProperty, value); }
    }

    public IEasingFunction EasingFunction
    {
        get { return (IEasingFunction)GetValue(EasingFunctionProperty); }
        set { SetValue(EasingFunctionProperty, value); }
    }

    public Brush InnerBrush
    {
        get { return (Brush)GetValue(InnerBrushProperty); }
        set { SetValue(InnerBrushProperty, value); }
    }

    public double CurrentDiameter
    {
        get { return (double)GetValue(CurrentDiameterProperty); }
        set { SetValue(CurrentDiameterProperty, value); }
    }

    public RippleEffect(UIElement adornedElement) : base(adornedElement) => _adornedElement = adornedElement;

    public void Play()
    {
        var center = Center;
        var renderSize = _adornedElement.RenderSize;

        var d1 = new Vector(center.X, center.Y);
        var d2 = new Vector(renderSize.Width - center.X, center.Y);
        var d3 = new Vector(center.X, renderSize.Height - center.Y);
        var d4 = new Vector(renderSize.Width - center.X, renderSize.Height - center.Y);
        var maxRadiusSquared = Math.Max(
            Math.Max(d1.LengthSquared, d2.LengthSquared),
            Math.Max(d3.LengthSquared, d4.LengthSquared));

        double maxDiameter = Math.Sqrt(maxRadiusSquared) * 2;

        double fromDiameter = CurrentDiameter;
        if (fromDiameter > maxDiameter)
        {
            fromDiameter = 0;
        }

        DoubleAnimation doubleAnimation = new DoubleAnimation()
        {
            From = fromDiameter,
            To = maxDiameter,
            Duration = Duration,
        };

        doubleAnimation.Completed += (s, e) =>
        {
            BeginAnimation(CurrentDiameterProperty, null);
            CurrentDiameter = maxDiameter;

            Completed?.Invoke(this, EventArgs.Empty);
        };

        BeginAnimation(CurrentDiameterProperty, doubleAnimation);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var center = Center;
        var radius = CurrentDiameter / 2;

        var innerBrush = InnerBrush;
        _cachedClip ??= new RectangleGeometry(new Rect(0, 0, _adornedElement.RenderSize.Width, _adornedElement.RenderSize.Height));
        drawingContext.PushClip(_cachedClip);
        drawingContext.DrawEllipse(innerBrush, null, center, radius, radius);
        drawingContext.Pop();
    }

    public static readonly DependencyProperty CenterProperty =
        DependencyProperty.Register("Center", typeof(Point), typeof(RippleEffect), new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(RippleEffect), new PropertyMetadata(TimeSpan.FromMilliseconds(300)));

    public static readonly DependencyProperty EasingFunctionProperty =
        DependencyProperty.Register("EasingFunction", typeof(IEasingFunction), typeof(RippleEffect), new PropertyMetadata(null));

    public static readonly DependencyProperty InnerBrushProperty =
        DependencyProperty.Register("InnerBrush", typeof(Brush), typeof(RippleEffect), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CurrentDiameterProperty =
        DependencyProperty.Register("CurrentDiameter", typeof(double), typeof(RippleEffect), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public event EventHandler? Completed;
}
