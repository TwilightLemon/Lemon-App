using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.User;
using LemonApp.Services;
using LemonApp.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class UserMenuViewModel:ObservableObject
{
    public record ActionMenu(string Name,Geometry? Icon,Action? Action);
    private readonly SettingsMgr<UserProfile>? profileMgr;
    public Action? RequestClose;
    public UserMenuViewModel(
        AppSettingsService appSettingsService,
        UserProfileService userProfileService)
    {
        profileMgr = appSettingsService.GetConfigMgr<UserProfile>();
        userProfile = profileMgr?.Data;
        if(!string.IsNullOrEmpty(userProfile?.TencUserAuth?.Id))
        {
            IsLoginQQ = Visibility.Visible;
            //只有登录到QQ音乐之后才能绑定网易云
            Menus.Insert(1, new ActionMenu("登录到网易云音乐",(Geometry)App.Current.FindResource("NeteaseIcon"), Menu_LoginNetease));
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
            action.Invoke();
            RequestClose?.Invoke();
        }
    }

    public ObservableCollection<ActionMenu> Menus { get; set; } = [
        new ActionMenu("登录到QQ音乐",null,Menu_LoginQQ),
        new ActionMenu("设置",(Geometry)App.Current.FindResource("Icon_Settings"),Menu_GotoSettingsPage),
        new ActionMenu("退出",null,Menu_Exit)
    ];
    static void Menu_LoginQQ()
    {
        var sp = App.Host!.Services;
        var loginWindow = sp.GetRequiredService<LoginWindow>();
        var user=sp.GetRequiredService<UserProfileService>();
        loginWindow.OnLogin = async (auth) =>
        {
            await user.UpdateAuthAndNotify(auth);
        };
        loginWindow.Show();
    }
    static async void Menu_LoginNetease()
    {
        var settings = App.Host!.Services.GetRequiredService<AppSettingsService>();
        var mgr = settings.GetConfigMgr<UserProfile>()!;
        mgr.Data!.NeteaseUserAuth = new NeteaseUserAuth() { Id = "100101010" };
        await mgr.Save();
    }
    static void Menu_GotoSettingsPage()
    {
        var navigator = App.Host!.Services.GetRequiredService<MainNavigationService>();
        navigator.RequstNavigation(PageType.SettingsPage);
    }
    static void Menu_Exit()
    {
        Environment.Exit(0);
    }
}
