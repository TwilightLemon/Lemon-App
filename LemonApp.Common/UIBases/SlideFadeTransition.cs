using EleCho.WpfSuite.Media.Transition;
using System.Windows.Media.Animation;
using System.Windows;
using EleCho.WpfSuite.Controls;

namespace LemonApp.Common.UIBases;
public class SlideFadeTransition : ContentTransition
{
    private Thickness Distance { get; set; } = new Thickness(0, 120, 0, -120);
    private Thickness ReverseDistance { get; set; } = new Thickness(0, -120, 0, 120);

    /// <inheritdoc/>
    protected override Freezable CreateInstanceCore() => new SlideFadeTransition();

    /// <inheritdoc/>
    protected override Storyboard CreateNewContentStoryboard(UIElement container, UIElement newContent, bool forward)
    {
        ThicknessAnimation translateAnimation = new()
        {
            EasingFunction = EasingFunction,
            Duration = Duration,
            From = Distance,
            To = default,
        };

        DoubleAnimation opacityAnimation = new()
        {
            Duration = Duration,
            From = 0,
            To = 1
        };

        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath(nameof(FrameworkElement.Margin)));
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(nameof(FrameworkElement.Opacity)));

        return new Storyboard()
        {
            Duration = Duration,
            Children =
                {
                    translateAnimation,
                    opacityAnimation
                }
        };
    }

    /// <inheritdoc/>
    protected override Storyboard CreateOldContentStoryboard(UIElement container, UIElement oldContent, bool forward)
    {
        DoubleAnimation opacityAnimation = new()
        {
            Duration = TimeSpan.Zero,
            To = 0
        };
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(nameof(FrameworkElement.Opacity)));

        return new Storyboard()
        {
            Duration = Duration,
            Children =
                {
                    opacityAnimation
                }
        };
    }
}
