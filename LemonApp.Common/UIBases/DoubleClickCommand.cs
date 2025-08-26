using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LemonApp.Common.UIBases;

public class DoubleClickCommand
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
            typeof(ICommand), typeof(DoubleClickCommand), new PropertyMetadata(null, OnCommandChanged));

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
            typeof(object), typeof(DoubleClickCommand), new PropertyMetadata(null));
    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            if (e.OldValue != null)
            {
                element.MouseLeftButtonDown -= Element_MouseLeftButtonDown;
            }
            if (e.NewValue != null)
            {
                element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            }
        }
    }

    private static void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (sender is FrameworkElement element)
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
}
