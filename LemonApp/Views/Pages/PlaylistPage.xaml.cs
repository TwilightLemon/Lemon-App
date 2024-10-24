﻿using LemonApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Views.Pages
{
    /// <summary>
    /// PlaylistPage.xaml 的交互逻辑
    /// </summary>
    public partial class PlaylistPage : Page
    {
        public PlaylistPage()
        {
            InitializeComponent();
            _hideInfoViewAni ??= FindResource("HideInfoViewAni") as Storyboard;
            _showInfoViewAni ??= FindResource("ShowInfoViewAni") as Storyboard;
            if (_hideInfoViewAni is { } && _showInfoViewAni is { })
            {
                _hideInfoViewAni.Completed += delegate
                {
                    _isHideInfoView = true;
                };
                _showInfoViewAni.Completed += delegate
                {
                    _isHideInfoView = false;
                };
            }
            DataContextChanged += PlaylistPage_DataContextChanged;
        }
        private Storyboard? _hideInfoViewAni, _showInfoViewAni;
        private bool _isHideInfoView = false;
        private PlaylistPageViewModel? _vm;
        private void PlaylistPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PlaylistPageViewModel { } vm)
            {
                _vm = vm;
                if (!vm.ShowInfoView)
                {
                    if (FindResource("CollapseInfoViewAction") is Storyboard { } sb)
                    {
                        sb.Begin();
                    }
                }
            }
        }

        private void listBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
           /* if (_vm is { } && _vm.ShowInfoView)
            {
                if (e.VerticalOffset > 0)
                {
                    if (_hideInfoViewAni != null && !_isHideInfoView)
                    {
                        _hideInfoViewAni?.Begin();
                    }
                }
                else
                {
                    if (_showInfoViewAni != null && _isHideInfoView)
                    {
                        _showInfoViewAni?.Begin();
                        _isHideInfoView = false;
                    }
                }
            }*/

            if (e.VerticalChange > 0)
            {
                if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight)
                {
                    ViewModel?.LoadMore();
                }
            }
        }

        private void listBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listBox.SelectedItem is Music { } m)
                _vm!.PlayMusicCommand.Execute(m);
        }

        public PlaylistPageViewModel? ViewModel
        {
            get => DataContext as PlaylistPageViewModel;
            set => DataContext = value;
        }
    }
}
