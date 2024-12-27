using LemonApp.ViewModels;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// MyBoughtPage.xaml 的交互逻辑
    /// </summary>
    public partial class MyBoughtPage : Page
    {
        public MyBoughtPage()
        {
            InitializeComponent();
        }
        public AlbumItemViewModel? MyDissViewModel
        {
            get => viewer.ViewModel;
            set => viewer.ViewModel = value;
        }
    }
}
