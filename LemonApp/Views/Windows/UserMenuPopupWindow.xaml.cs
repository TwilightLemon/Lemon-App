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
        }
        private readonly UserMenuViewModel _userMenuViewModel;
        public Action? RequestClose { set => _userMenuViewModel.RequestClose = value; }
    }
}
