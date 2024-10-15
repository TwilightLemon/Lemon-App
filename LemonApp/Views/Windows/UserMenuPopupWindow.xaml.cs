using LemonApp.Common.UIBases;
using LemonApp.ViewModels;
using System.Windows.Controls;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// UserMenuPopupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserMenuPopupWindow :UserControl
    {
        public UserMenuPopupWindow(UserMenuViewModel userMenuViewModel)
        {
            InitializeComponent();
            DataContext= userMenuViewModel;
        }
    }
}
