using LemonApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// PlaylistPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistPage : Page
    {
        public PlaylistPage()
        {
            InitializeComponent();
            _hideInfoViewAni??= FindResource("HideInfoViewAni") as Storyboard;
            _showInfoViewAni ??= FindResource("ShowInfoViewAni") as Storyboard;
            DataContextChanged += PlaylistPage_DataContextChanged;
        }
        private Storyboard? _hideInfoViewAni, _showInfoViewAni;
        private void PlaylistPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is PlaylistPageViewModel { } vm)
            {
                if (!vm.ShowInfoView)
                {
                    if(FindResource("CollapseInfoViewAction") is Storyboard{ } sb)
                    {
                        sb.Begin();
                    }
                }
            }
        }

        public PlaylistPageViewModel? ViewModel
        {
            get => DataContext as PlaylistPageViewModel;
            set => DataContext = value;
        }
    }
}
