using LemonApp.Common.UIBases;
using LemonApp.ViewModels;
using System;
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
            DataContext = _userMenuViewModel = userMenuViewModel;
            Initialized += UserMenuPopupWindow_Initialized;
        }

        private void UserMenuPopupWindow_Initialized(object? sender, EventArgs e)
        {
            this.Height = Body.ActualHeight;
        }

        private readonly UserMenuViewModel _userMenuViewModel;
        public Action? RequestCloseMenu { set => _userMenuViewModel.RequestCloseMenu = value; }
    }
}
