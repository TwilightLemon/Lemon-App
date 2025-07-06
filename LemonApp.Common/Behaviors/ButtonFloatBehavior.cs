using EleCho.WpfSuite.Controls;
using Microsoft.Xaml.Behaviors;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Common.Behaviors;

public class ButtonFloatBehavior : Behavior<Button>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
        AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
    }

    private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Button { RenderTransform: TranslateTransform { } transform })
        {
            var animation = new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase()
            };
            transform.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    private void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Button btn)
        {
            //鼠标进入时，按钮向上浮动动画
            var transform = new TranslateTransform();
            btn.RenderTransform = transform;
            var animation = new DoubleAnimation(0, -10, TimeSpan.FromMilliseconds(300));
            animation.EasingFunction = new CubicEase();
            transform.BeginAnimation(TranslateTransform.YProperty, animation);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
    }
}
