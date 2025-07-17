using System.Windows;
using System.Windows.Input;

namespace LemonApp.Common.UIBases;

public class RightClickCommand
{
    public static ICommand GetCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", 
            typeof(ICommand), typeof(RightClickCommand), new PropertyMetadata(null,OnCommandChanged));

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            if (e.OldValue != null)
            {
                element.PreviewMouseRightButtonDown -= Element_MouseRightButtonDown;
                element.PreviewMouseRightButtonUp -= Element_MouseRightButtonUp;
            }
            if (e.NewValue != null)
            {
                element.PreviewMouseRightButtonDown += Element_MouseRightButtonDown;
                element.PreviewMouseRightButtonUp += Element_MouseRightButtonUp;
            }
        }
    }

    private static void Element_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if(sender is FrameworkElement element&& GetMouseCaptured(element))
        {
            var point = e.GetPosition(element);
            Mouse.Capture(null);
            SetMouseCaptured(element, false);
            if (point.X >= 0 && point.X <= element.ActualWidth && point.Y >= 0 && point.Y <= element.ActualHeight)
            {
                //execlute command
                if (GetCommand(element) is ICommand { } command)
                {
                    var parameter = GetCommandParameter(element);
                    if (command.CanExecute(parameter))
                    {
                        command.Execute(parameter);
                        e.Handled = true;
                    }
                }
            }
        }
    }

    private static void Element_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if(sender is FrameworkElement element)
        {
            Mouse.Capture(element);
            SetMouseCaptured(element,true);
        }
    }

    internal static bool GetMouseCaptured(DependencyObject obj)
    {
        return (bool)obj.GetValue(MouseCapturedProperty);
    }

    internal static void SetMouseCaptured(DependencyObject obj, bool value)
    {
        obj.SetValue(MouseCapturedProperty, value);
    }

    internal static readonly DependencyProperty MouseCapturedProperty =
        DependencyProperty.RegisterAttached("MouseCaptured", typeof(bool), typeof(RightClickCommand),
            new PropertyMetadata(false));



    public static object GetCommandParameter(DependencyObject obj)
    {
        return obj.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(DependencyObject obj, object value)
    {
        obj.SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached("CommandParameter", 
            typeof(object), typeof(RightClickCommand), new PropertyMetadata(null));


}
