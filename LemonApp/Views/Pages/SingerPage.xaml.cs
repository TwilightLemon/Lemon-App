using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
                //Recent Albums
                var recentVm = App.Services.GetRequiredService<AlbumItemViewModel>();
                if (vm.SingerPageData.RecentAlbums is { Count: >0 } recent)
                {
                    recentVm.SetAlbumItems(recent);
                    RecentViewer.DataContext = recentVm;
                }
                else RecentCard.Visibility = Visibility.Collapsed;

                //Related Singers
                var singerVm = App.Services.GetRequiredService<SingerItemViewModel>();
                if (vm.SingerPageData.SimilarSingers is { Count: > 0 } singers)
                {
                    singerVm.SetList(singers);
                    RelatedSingerView.DataContext = singerVm;
                }
                else RelatedSingerCard.Visibility = Visibility.Collapsed;

                //Albums
                var albumVm = App.Services.GetRequiredService<AlbumItemViewModel>();
                if (vm.SingerPageData.Albums is { Count: > 0 } albums)
                {
                    albumVm.SetAlbumItems(albums);
                    AlbumListView.DataContext = albumVm;
                }
                else AlbumListCard.Visibility = Visibility.Collapsed;


                if (vm.BigBackground != null)
                {
                    SingerNamePanel.Margin = new(30,160 ,0,0);
                    CoverImg.Visibility = Visibility.Collapsed;
                    BigBackground.Height = 350;
                    TextBlock.SetForeground(SingerNamePanel, Brushes.WhiteSmoke);
                }
            }
        }
    }
}
