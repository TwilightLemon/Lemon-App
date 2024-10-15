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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class UserMenuViewModel:ObservableObject
{
    public record ActionMenu(string Name,Geometry? Icon,Action? Action);
    private readonly SettingsMgr<UserProfile>? profileMgr;
    public UserMenuViewModel(AppSettingsService appSettingsService)
    {
        profileMgr = appSettingsService.GetConfigMgr<UserProfile>();
        userProfile = profileMgr?.Data;
        if(!string.IsNullOrEmpty(userProfile?.TencUserAuth?.Id))
        {
            IsLoginQQ = Visibility.Visible;
            //只有登录到QQ音乐之后才能绑定网易云
            Menus.Insert(1, new ActionMenu("登录到网易云音乐",null, Menu_LoginNetease));
        }
        if (!string.IsNullOrEmpty(userProfile?.NeteaseUserAuth?.Id))
        {
            IsLoginNetease = Visibility.Visible;
        }

    }
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
        if (value != null && value.Action is { } action)
            action.Invoke();
    }

    public ObservableCollection<ActionMenu> Menus { get; set; } = [
        new ActionMenu("登录到QQ音乐",null,Menu_LoginQQ),
        new ActionMenu("设置",(Geometry)App.Current.FindResource("Icon_Settings"),null),
        new ActionMenu("退出",null,Menu_Exit)
    ];

    //TODO: 统一的账号管理服务，用于调用登录和完成通知

    static void Menu_LoginQQ()
    {
        var loginWindow = App.Host!.Services.GetRequiredService<LoginWindow>();
        var settings = App.Host!.Services.GetRequiredService<AppSettingsService>();
        var mgr=settings.GetConfigMgr<UserProfile>()!;
        loginWindow.OnLogin = async (auth) =>
        {
            Debug.WriteLine("Login qq:"+auth.Id);
            var up = new TencUserProfileGetter();
            await up.Fetch(auth);
            mgr.Data = new UserProfile()
            {
                TencUserAuth = auth,
                NeteaseUserAuth = mgr.Data?.NeteaseUserAuth,
                UserName = up.UserName,
                AvatarUrl = up.AvatarUrl
            };
            await mgr.Save();
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
    static void Menu_Exit()
    {
        Environment.Exit(0);
    }
}
