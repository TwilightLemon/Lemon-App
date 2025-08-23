using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Controls;

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
        [RelayCommand]
        private void RemoveDir(LocalDirMeta meta)
        {
            localDissService.RemoveDir(meta);
        }
    }
}
