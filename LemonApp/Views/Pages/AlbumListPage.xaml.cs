using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// AlbumListPage.xaml 的交互逻辑
    /// </summary>
    public partial class AlbumListPage : Page
    {
        private readonly MainNavigationService nav;
        public AlbumListPage(UserProfileService userProfileService,
            MainNavigationService mainNavigationService)
        {
            InitializeComponent();
            nav = mainNavigationService;
            Loaded += AlbumListPage_Loaded;
        }
        //Title
        public Func<Task<List<AlbumInfo>>>? DataProvider { get; set; }
        public Func<AlbumItemViewModel,int,Task>? NextPage { get; set; }
        private async void AlbumListPage_Loaded(object sender, RoutedEventArgs e)
        {
            nav.BeginLoadingAni();
            if(DataProvider?.Invoke() is { } task)
            {
                var data= await task;
                viewer.ViewModel ??= App.Services.GetRequiredService<AlbumItemViewModel>();
                viewer.ViewModel.SetAlbumItems(data);
            }
            nav.CancelLoadingAni();
        }
        private int _pageIndex = 0;
        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0)
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight)
                {
                    _pageIndex++;
                    nav.BeginLoadingAni();
                    if(viewer.ViewModel is AlbumItemViewModel vm && NextPage?.Invoke(vm,_pageIndex) is { } task)
                    {
                        await task;
                    }
                    nav.CancelLoadingAni();
                }
            }
        }
    }
}
