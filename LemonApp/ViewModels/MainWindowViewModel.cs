using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.Services;
using LemonApp.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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

    public class MainMenu(string name, Geometry icon, Type pageType)
    {
        public string Name { get; } = name;
        public Geometry Icon { get; } = icon;
        public Type PageType { get; } = pageType;
        public bool RequireCreateNewPage = true;
    }
    public ObservableCollection<MainMenu> MainMenus { get; set; } = [
        new MainMenu("Home", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(HomePage)),
        new MainMenu("Rank", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), typeof(RankPage))
        ];


    public object? CurrentPage;

    [ObservableProperty]
    private MainMenu? selectedMenu;

    partial void OnSelectedMenuChanged(MainMenu? value)
    {
        if (value != null)
        {
            if (value.PageType is { } type && value.RequireCreateNewPage)
            {
                var page = App.Host!.Services.GetRequiredService(type);
                CurrentPage = page;
            }
            else if(!value.RequireCreateNewPage)
            {
                value.RequireCreateNewPage = true;//reset
            }
        }else{
            CurrentPage = null;
        }
    }


    public bool IsLyricPageOpen { get; set; } = false;

    [ObservableProperty]
    private Brush avator = Brushes.LightPink;
}
