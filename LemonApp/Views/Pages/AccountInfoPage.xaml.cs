using LemonApp.ViewModels;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// AccountInfoPage.xaml 的交互逻辑
    /// </summary>
    public partial class AccountInfoPage : Page
    {
        public AccountInfoPage(AccountInfoPageViewModel accountInfoPageViewModel)
        {
            InitializeComponent();
            DataContext = accountInfoPageViewModel;
            _ = accountInfoPageViewModel.Load();
        }
    }
}
