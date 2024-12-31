using LemonApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// MyDissPage.xaml 的交互逻辑
    /// </summary>
    public partial class MyDissPage : Page
    {
        public MyDissPage()
        {
            InitializeComponent();
        }
        public PlaylistItemViewModel? MyDissViewModel
        {
            get => MyDissList.ViewModel;
            set => MyDissList.ViewModel = value;
        }
        public PlaylistItemViewModel? MyFavViewModel
        {
            get => MyFarvoriteDissList.ViewModel;
            set => MyFarvoriteDissList.ViewModel = value;
        }
    }
}
