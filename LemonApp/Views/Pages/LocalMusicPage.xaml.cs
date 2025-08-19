using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// LocalMusicPage.xaml 的交互逻辑
    /// </summary>
    public partial class LocalMusicPage : Page
    {
        private readonly LocalDissService localDissService;
        private readonly MainNavigationService mainNavigationService;

        public ObservableCollection<LocalDirMeta> LocalDirs => localDissService.LocalDirs;
        public LocalMusicPage(LocalDissService localDissService,MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            DataContext = this;
            this.localDissService = localDissService;
            this.mainNavigationService = mainNavigationService;
        }
        [RelayCommand]
        private void GotoPlaylist(LocalDirMeta meta)
        {
            mainNavigationService.RequstNavigation(PageType.LocalPlaylistPage, meta);
        }
        [RelayCommand]
        private void OpenAddDirDialog()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                localDissService.AddDir(dialog.SelectedPath);
            }
        }
    }
}
