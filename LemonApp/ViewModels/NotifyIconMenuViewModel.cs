﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.ViewModels;

public partial class NotifyIconMenuViewModel : ObservableObject
{
    public record SimpleActionMenu(string Name, Action? Action);
    public ObservableCollection<SimpleActionMenu> Menus { get; set; } = [
        new SimpleActionMenu("显示",Menu_Show),
        new SimpleActionMenu("退出",Menu_Exit)
        ];
    public Action? RequestCloseMenu;
    [ObservableProperty]
    private SimpleActionMenu? _selectedMenu;
    partial void OnSelectedMenuChanged(SimpleActionMenu? value)
    {
        if (value != null)
        {
            value.Action?.Invoke();
            RequestCloseMenu?.Invoke();
        }
    }
    private static void Menu_Show()
    {
        App.Current.MainWindow.ShowWindow();
    }
    private static void Menu_Exit()
    {
        App.Current.Shutdown();
    }
}