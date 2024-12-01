using LemonApp.ViewModels;
using System.Windows.Controls;

namespace LemonApp.Views.UserControls
{
    /// <summary>
    /// PlaylistItemView.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistItemView : UserControl
    {
        public PlaylistItemView()
        {
            InitializeComponent();
        }
        public PlaylistItemViewModel? ViewModel
        {
            get => DataContext as PlaylistItemViewModel;
            set => DataContext = value;
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List.SelectedItem = null;
        }
    }
}
