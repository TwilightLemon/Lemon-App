using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
    public partial class SingerPage : Page
    {
        public SingerPage()
        {
            InitializeComponent();
            DataContextChanged += SingerPage_DataContextChanged;
        }

        private void SingerPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(DataContext is SingerPageViewModel{SingerPageData:not null} vm)
            {
                var albumVm = App.Services.GetRequiredService<AlbumItemViewModel>();
                _=albumVm.SetAlbumItems(vm.SingerPageData.RecentAlbums);
                AlbumViewer.DataContext = albumVm;
            }
        }

        private void HotSongList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is SingerPageViewModel { SingerPageData: not null } vm)
            {
                _ = vm.PlayHotSongs();
            }
        }
    }
}
