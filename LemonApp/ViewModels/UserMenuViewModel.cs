﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.Services;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class UserMenuViewModel:ObservableObject
{
    public record ActionMenu(string Name,Geometry? Icon,Action? Action);
    private readonly SettingsMgr<UserProfile> profileMgr;
    public Action? RequestCloseMenu;
    public UserMenuViewModel(
        AppSettingService appSettingsService,
        UserProfileService userProfileService)
    {
        profileMgr = appSettingsService.GetConfigMgr<UserProfile>();
        userProfile = profileMgr.Data;
        if(!string.IsNullOrEmpty(userProfile.TencUserAuth?.Id))
        {
            IsLoginQQ = Visibility.Visible;
            //只有登录到QQ音乐之后才能绑定网易云
            Menus.Insert(1, new ActionMenu("Log in by Netease Music",(Geometry)App.Current.FindResource("NeteaseIcon"), Menu_LoginNetease));
            //载入profile info
            var a = async () =>
            {
                if (await userProfileService.GetAvatorImg() is { } img)
                {
                    Avator = new ImageBrush(img);
                }
            };
            a();
        }
        if (!string.IsNullOrEmpty(userProfile?.NeteaseUserAuth?.Id))
        {
            IsLoginNetease = Visibility.Visible;
        }

    }

    [RelayCommand]
    private void GotoProfilePage() {
        var navigator = App.Services.GetRequiredService<MainNavigationService>();
        navigator.RequstNavigation(PageType.AccountInfoPage);
        RequestCloseMenu?.Invoke();
    }

    [ObservableProperty]
    public Brush avator=Brushes.LightPink;

    [ObservableProperty]
    public UserProfile? userProfile;

    [ObservableProperty]
    public ActionMenu? selectedMenuItem= null;

    [ObservableProperty]
    public Visibility isLoginQQ= Visibility.Collapsed;

    [ObservableProperty]
    public Visibility isLoginNetease = Visibility.Collapsed;

    partial void OnSelectedMenuItemChanged(ActionMenu? value)
    {
        if (value != null && value.Action is { } action){
            RequestCloseMenu?.Invoke();
            action.Invoke();
        }
    }

    public ObservableCollection<ActionMenu> Menus { get; set; } = [
        new ActionMenu("Log in by QQ Music",(Geometry)App.Current.FindResource("QQMusicIcon"),Menu_LoginQQ),
        new ActionMenu("Settings",(Geometry)App.Current.FindResource("Icon_Settings"),Menu_GotoSettingsPage),
        new ActionMenu("View Theme Config",(Geometry)App.Current.FindResource("Menu_Theme"),Menu_Theme),
        new ActionMenu("Show Embedded Lyric",(Geometry)App.Current.FindResource("Icon_Desktop"),Menu_OpenDesktopWindow),
        new ActionMenu("Exit",null,Menu_Exit)
    ];
    public static void Menu_LoginQQ()
    {
        var sp = App.Services;
        var loginWindow = sp.GetRequiredService<LoginWindow>();
        var user=sp.GetRequiredService<UserProfileService>();
        loginWindow.OnLoginTenc = async (auth) =>
        {
            await user.UpdateAuthAndNotify(auth);
        };
        loginWindow.ShowDialog();
    }
    static void Menu_LoginNetease()
    {
        var sp = App.Services;
        var loginWindow = sp.GetRequiredService<LoginWindow>();
        var user = sp.GetRequiredService<UserProfileService>();
        loginWindow.OnLoginNetease = user.UpdateNeteaseAuth;
        loginWindow.LoginTenc = false;
        loginWindow.ShowDialog();
    }
    static void Menu_Theme()
    {
        //TODO: add theme config page
        string path = System.IO.Path.Combine(Settings.SettingsPath, "Appearance.json");
        Process.Start("explorer", path);
    }
    static void Menu_GotoSettingsPage()
    {
        var navigator = App.Services.GetRequiredService<MainNavigationService>();
        navigator.RequstNavigation(PageType.SettingsPage);
    }

    static EmbeddedWindow? _desktopWindow = null;
    public static void Menu_OpenDesktopWindow()
    {
        var config = App.Services.GetRequiredService<AppSettingService>().GetConfigMgr<PlayingPreference>();
        if (_desktopWindow == null||!_desktopWindow.IsLoaded)
        {
            _desktopWindow = App.Services.GetRequiredService<EmbeddedWindow>();
            _desktopWindow.Closing += (s, e) => 
            { 
                _desktopWindow = null;
                config.Data.EnableEmbededLyric = false;
            };
            _desktopWindow.Show();
            config.Data.EnableEmbededLyric = true;
        }
        else _desktopWindow.Close();
    }
    static void Menu_Exit()
    {
        App.Current.Shutdown();
    }
}
