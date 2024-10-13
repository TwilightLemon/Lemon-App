using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class UserMenuViewModel:ObservableObject
{
    public record ActionMenu(string Name,Geometry? Icon,Action? Action);
    private readonly SettingsMgr<UserProfile>? profileMgr;
    public UserMenuViewModel(AppSettingsService appSettingsService)
    {
         profileMgr = appSettingsService.GetConfigMgr<UserProfile>();
        tencUserAuth = profileMgr?.Data?.TencUserAuth;
        neteaseUserAuth= profileMgr?.Data?.NeteaseUserAuth;
    }

    [ObservableProperty]
    public TencUserAuth? tencUserAuth;
    [ObservableProperty]
    public NeteaseUserAuth? neteaseUserAuth;

    [ObservableProperty]
    public ActionMenu? selectedMenuItem= null;
    partial void OnSelectedMenuItemChanged(ActionMenu? value)
    {
        if (value != null && value.Action is { } action)
            action.Invoke();
    }

    public ObservableCollection<ActionMenu> Menus { get; set; } = [
        new ActionMenu("Settings",Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"),null),
        new ActionMenu("Exit",null,null)
        ];
}
