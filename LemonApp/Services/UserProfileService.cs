using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.User;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LemonApp.Services;

public class UserProfileService(
    AppSettingsService appSettingsService,
    IHttpClientFactory httpClientFactory)
{
    public event Action<TencUserAuth>? OnAuth;
    public event Action? OnAuthExpired;

    public async Task UpdateAuthAndNotify(TencUserAuth auth)
    {
        Debug.WriteLine("Login qq:" + auth.Id);

        var mgr = appSettingsService.GetConfigMgr<UserProfile>()
            ?? throw new InvalidOperationException("where is user profile mgr??!!");
        var up = new TencUserProfileGetter();
        var client = (httpClientFactory.CreateClient(App.PublicClientFlag))
            ?? throw new InvalidOperationException("where is public httpclient??!!");

        //获取nick pic
        bool success = await up.Fetch(client, auth);
        if (success)
        {
            mgr.Data = new UserProfile()
            {
                TencUserAuth = auth,
                NeteaseUserAuth = mgr.Data?.NeteaseUserAuth,
                UserName = up.UserName,
                AvatarUrl = up.AvatarUrl
            };
            await mgr.Save();
            OnAuth?.Invoke(auth);
        }
        else
        {
            OnAuthExpired?.Invoke();
        }
    }

    public TencUserAuth GetAuth()
    {
        if(appSettingsService.GetConfigMgr<UserProfile>() is { } mgr)
        {
            return mgr.Data?.TencUserAuth;
        }
        return new();
    }
    
    public async  Task<BitmapImage?> GetAvatorImg()
    {
        if(appSettingsService.GetConfigMgr<UserProfile>() is { } mgr && mgr.Data?.AvatarUrl is { } url)
            return await ImageCacheHelper.FetchData(url);

        return null;
    }

    public string? GetNickname()
    {
        if (appSettingsService.GetConfigMgr<UserProfile>() is { } mgr && mgr.Data?.UserName is { } name)
            return name;

        return null;
    }
}
