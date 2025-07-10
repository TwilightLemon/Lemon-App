using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LemonApp.Common.Behaviors;
public class FadeBorderBehavior:Behavior<Border>
{
    public Brush Brush
    {
        get { return (Brush)GetValue(BrushProperty); }
        set { SetValue(BrushProperty, value); }
    }

    public static readonly DependencyProperty BrushProperty =
        DependencyProperty.Register("Brush", typeof(Brush), typeof(FadeBorderBehavior),
            new PropertyMetadata(null, OnBrushChanged));

    private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FadeBorderBehavior behavior && e.NewValue is Brush brush)
        {
            behavior.TransitionToNewBrush(brush);
        }
    }



    public ImageSource Source
    {
        get { return (ImageSource)GetValue(SourceProperty); }
        set { SetValue(SourceProperty, value); }
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(ImageSource), typeof(FadeBorderBehavior), 
            new PropertyMetadata(null, OnSourceChanged));

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FadeBorderBehavior behavior && e.OldValue is ImageSource source)
        {
            behavior.TransitionWithImageSource(source);
        }
    }



    public Duration Duration
    {
        get { return (Duration)GetValue(DurationProperty); }
        set { SetValue(DurationProperty, value); }
    }

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register("Duration", typeof(Duration), typeof(FadeBorderBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(300))));

    private void TransitionToNewBrush(Brush newBrush)
    {
        if (newBrush == null || AssociatedObject == null) return;

        var oldBrush = AssociatedObject.Background;
        if (oldBrush == null)
        {
            AssociatedObject.Background = newBrush;
            return;
        }

        var cover = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background=newBrush,
            CornerRadius=AssociatedObject.CornerRadius
        };
        AssociatedObject.Child = cover;
        var ani = new DoubleAnimation
        {
            From = 0,
            To=1,
            Duration = Duration
        };
        ani.Completed += delegate {
            AssociatedObject.Child = null;
            AssociatedObject.Background = newBrush;
        };
        cover.BeginAnimation(UIElement.OpacityProperty, ani);
    }

    private void TransitionWithImageSource(ImageSource oldSource)
    {
        if(oldSource ==null|| AssociatedObject==null) return;
        if (AssociatedObject.Background is not ImageBrush image) return;
        var oldBrush = new ImageBrush()
        {
            ImageSource = oldSource,
            Stretch = image.Stretch,
            RelativeTransform = image.RelativeTransform.CloneCurrentValue()
        };
        var cover = new Border
        {
            Background =oldBrush,
            CornerRadius = AssociatedObject.CornerRadius
        };
        AssociatedObject.Child = cover;
        var ani = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = Duration
        };
        ani.Completed += delegate {
            AssociatedObject.Child = null;
        };
        cover.BeginAnimation(UIElement.OpacityProperty, ani);
    }
}
