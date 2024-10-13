using LemonApp.Common.UIBases;
using LemonApp.ViewModels;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// UserMenuPopupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserMenuPopupWindow : PopupWindowBase
    {
        public UserMenuPopupWindow(UserMenuViewModel userMenuViewModel)
        {
            InitializeComponent();
            DataContext= userMenuViewModel;
        }
    }
}
