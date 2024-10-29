using LemonApp.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// NotifyIconMenuWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NotifyIconMenuWindow : Window
    {
        public NotifyIconMenuWindow(NotifyIconMenuViewModel notifyIconMenuViewModel)
        {
            InitializeComponent();
            notifyIconMenuViewModel.RequestCloseMenu = Close;
            DataContext = notifyIconMenuViewModel;
            Deactivated += NotifyIconMenuWindow_Deactivated;
            Closing += NotifyIconMenuWindow_Closing;
        }

        private void NotifyIconMenuWindow_Deactivated(object? sender, EventArgs e)
        {
            Close();
        }

        private void NotifyIconMenuWindow_Closing(object? sender, CancelEventArgs e)
        {
            Deactivated -= NotifyIconMenuWindow_Deactivated;
        }
    }
}
