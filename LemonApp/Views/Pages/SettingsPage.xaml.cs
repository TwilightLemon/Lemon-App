using LemonApp.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage(SettingsPageViewModel settingsPageViewModel)
        {
            InitializeComponent();
            DataContext = vm = settingsPageViewModel;
        }
        private readonly SettingsPageViewModel vm;
        bool IsAboutMoreOpen = false;
        private async void AboutMoreBt_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAboutMoreOpen)
            {
                await vm.LoadPublisherContent();
                ContentPanel.Children.Add(vm.PublisherContent);
                IsAboutMoreOpen = true;
            }
            else
            {
                ContentPanel.Children.Remove(vm.PublisherContent);
                IsAboutMoreOpen = false;
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            vm.LoadData();
        }
    }
}
