using LemonApp.ViewModels;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {
        public DownloadPage(DownloadPageViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
