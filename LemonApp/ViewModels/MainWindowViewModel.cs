using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.Services;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LemonApp.ViewModels;
public partial class MainWindowViewModel:ObservableObject
{
    public MainWindowViewModel(
        UserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
        userProfileService.OnAuth += UserProfileService_OnAuth;
        userProfileService.OnAuthExpired += UserProfileService_OnAuthExpired;
    }

    private void UserProfileService_OnAuthExpired()
    {
        //TODO: notify main window to show msg
    }

    private void UserProfileService_OnAuth(TencUserAuth auth)
    {
        //update user profile viewmodel
        if(_userProfileService.GetAvatorImg()is { } img)
        {
            Avator = new ImageBrush(img);
        }
    }

    private readonly UserProfileService _userProfileService;

    public record MainMenu(string Name, Geometry Icon, Type PageType);
    public ObservableCollection<MainMenu> MainMenus { get; set; } = [
        new MainMenu("Home", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(HomePage)),
        new MainMenu("Rank", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), null)
        ];

    [ObservableProperty]
    private object? currentPage;

    [ObservableProperty]
    private MainMenu? selectedMenu;

    partial void OnSelectedMenuChanged(MainMenu? value)
    {
        if(value?.PageType is { } type)
        {
            var page = App.Host!.Services.GetRequiredService(type);
            CurrentPage = page;
        }
        else
        {
            CurrentPage = null;
        }
    }


    public bool IsLyricPageOpen { get; set; } = false;

    [ObservableProperty]
    private Brush avator = Brushes.LightPink;
}
