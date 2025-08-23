using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// PlaylistPage.xaml 的交互逻辑
    /// </summary>
    public partial class LocalPlaylistPage : Page
    {
        public LocalPlaylistPage()
        {
            InitializeComponent();
        }
        private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listBox.SelectedItem is Music { } m)
                ViewModel.PlayMusicCommand.Execute(m);
        }

        private void GotoPlayingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Playing is Music { } m)
            {
                listBox.ScrollIntoView(m);
            }
        }

        private void ListSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.SearchItem(ListSearchBox.Text);
        }

        public PlaylistPageViewModel ViewModel
        {
            get => DataContext as PlaylistPageViewModel;
            set => DataContext = value;
        }

        private void OpenDirectoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.Description is { }dir && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true
                });
            }
        }
    }
}
