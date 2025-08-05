using EleCho.WpfSuite.Controls;
using Microsoft.Xaml.Behaviors;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Common.Behaviors;

public class ButtonFloatBehavior : Behavior<Button>
{
    private TranslateTransform? _transform;
    private const double FloatDistance = 10;
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.RenderTransform = _transform = new();
        AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
        AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
    }

    private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
    {
        if(_transform!= null)
        {
            var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
            _transform.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    private void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
    {
        if (_transform!=null)
        {
            //判断鼠标进入至少距离下边框FloatDistance距离
            var mousePosition = e.GetPosition(AssociatedObject);
            if (mousePosition.Y >AssociatedObject.ActualHeight-FloatDistance)
                return;
            //鼠标进入时，按钮向上浮动动画
            var animation = new DoubleAnimation(0, -FloatDistance, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
            _transform.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
        AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
    }
}
