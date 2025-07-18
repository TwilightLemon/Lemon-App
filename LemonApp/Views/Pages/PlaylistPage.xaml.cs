﻿using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

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
        private PlaylistPageViewModel _vm= null;
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
            if (_vm is { } && _vm.ShowInfoView)
            {
                if (e.VerticalOffset > 120)
                {
                    if (_hideInfoViewAni != null && !_isHideInfoView)
                    {
                        _hideInfoViewAni?.Begin();
                    }
                }
                if(e.VerticalOffset<=6)
                {
                    if (_showInfoViewAni != null && _isHideInfoView)
                    {
                        _showInfoViewAni?.Begin();
                    }
                }
            }

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
                _vm.PlayMusicCommand.Execute(m);
        }

        private void GotoPlayingBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.Playing is Music { } m)
            {
                listBox.ScrollIntoView(m);
            }
        }

        private void ListSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vm.SearchItem(ListSearchBox.Text);
        }

        private void SelectModeTB_Click(object sender, RoutedEventArgs e)
        {
            listBox.SelectionMode = SelectModeTB.IsChecked == true ? SelectionMode.Multiple : SelectionMode.Single;
        }

        private void AddToNextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectionMode == SelectionMode.Multiple)
            {
                if (listBox.SelectedItems is { Count: > 0 })
                {
                    _vm.AddToPlayNextCommand.Execute(listBox.SelectedItems);
                    listBox.SelectedItems.Clear();
                }
            }
            else
            {
                if (listBox.SelectedItem != null)
                    _vm.AddToPlayNextSingleCommand.Execute(listBox.SelectedItem);
            }
            AddtoMenu.IsOpen = false;
        }

        private List<Music> SelectedMusic=> listBox.SelectionMode == SelectionMode.Multiple ?
                                            [.. listBox.SelectedItems.Cast<Music>()] : [(Music)listBox.SelectedItem];

        private void AddToDissBtn_Click(object sender, RoutedEventArgs e)
        {
            List<Music> selectedItems = SelectedMusic;
            Components.PublicPopupMenuHolder.AddToMyDissCommand?.Execute(selectedItems);
            AddtoMenu.IsOpen = false;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMusic.Count > 0)
                _vm.DeleteMusicFromDirid(SelectedMusic);
        }

        private async void AddToBtn_Click(object sender, RoutedEventArgs e)
        {
            await Task.Yield();
            AddtoMenu.IsOpen = true;
        }

        private void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMusic.Count > 0)
                _vm.DownloadMusic(SelectedMusic);
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectAllBtn.IsChecked is true)
            {
                listBox.SelectionMode = SelectionMode.Multiple;
                listBox.SelectAll();
            }
            else
            {
                listBox.SelectionMode = SelectionMode.Single;
            }
        }

        public PlaylistPageViewModel? ViewModel
        {
            get => DataContext as PlaylistPageViewModel;
            set => DataContext = value;
        }
    }
}
