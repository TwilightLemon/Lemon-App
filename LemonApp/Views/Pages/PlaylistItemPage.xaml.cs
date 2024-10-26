using LemonApp.ViewModels;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// PlaylistItemPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistItemPage : Page
    {
        public PlaylistItemPage()
        {
            InitializeComponent();
        }

        public PlaylistItemPageViewModel? ViewModel
        {
            get => DataContext as PlaylistItemPageViewModel;
            set => DataContext = value;
        }
    }
}
