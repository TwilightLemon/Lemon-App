using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Funcs;
using LemonApp.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LemonApp.ViewModels
{
    public partial class AccountInfoPageViewModel
        (UserProfileService userProfileService):ObservableObject
    {
        public async Task Load()
        {
            Nickname = userProfileService.GetNickname();
            Avator = new ImageBrush(await userProfileService.GetAvatorImg());
            SharedLaToken = userProfileService.GetSharedLaToken();
            var auth= userProfileService.GetAuth();
            cookie = auth.Cookie;
            G_tk = auth.G_tk;
        }
        [ObservableProperty]
        private string? nickname;
        [ObservableProperty]
        private Brush? avator;
        public string? cookie;
        [ObservableProperty]
        private string? g_tk;
        [ObservableProperty]
        private string? sharedLaToken;

        partial void OnSharedLaTokenChanged(string? value)
        {
            if (value != null)
                userProfileService.SetSharedLaToken(value);
        }

        [RelayCommand]
        public void CopyCookie()
        {
            Clipboard.SetText(cookie);
        }

        [RelayCommand]
        public void GotoProfileFiles()
        {
            string file=System.IO.Path.Combine(Settings.SettingsPath, "UserProfile.json");
            Process.Start("explorer", "/select," + file);
        }
    }
}
