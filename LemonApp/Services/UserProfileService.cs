using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.User;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Media.Protection.PlayReady;

namespace LemonApp.Services;
public class UserProfileService(
    AppSettingService appSettingsService,
    IHttpClientFactory httpClientFactory)
{
    public event Action<TencUserAuth>? OnAuth;
    public event Action? OnAuthExpired;
    public TencUserProfileGetter UserProfileGetter { get; } = new();
    private readonly SettingsMgr<UserProfile> _profileMgr = appSettingsService.GetConfigMgr<UserProfile>();

    public void UpdateNeteaseAuth(NeteaseUserAuth auth)
    {
        if (_profileMgr == null) throw new Exception("Failed to load components");

        _profileMgr.Data.NeteaseUserAuth= auth;
    }

    public NeteaseUserAuth? GetNeteaseAuth() => _profileMgr?.Data.NeteaseUserAuth;

    public async Task UpdateAuthAndNotify(TencUserAuth auth)
    {
        Debug.WriteLine("Login qq:" + auth.Id);
        var client = httpClientFactory.CreateClient(App.PublicClientFlag);
        if (_profileMgr == null||client==null) throw new Exception("Failed to load components");

        //获取nick pic
        bool success = await UserProfileGetter.Fetch(client, auth);
        if (success)
        {
            var data = _profileMgr.Data;
            data.TencUserAuth = auth;
            data.UserName = UserProfileGetter.UserName;
            data.AvatarUrl = UserProfileGetter.AvatarUrl;
            _profileMgr.Data = data;

            await _profileMgr.SaveAsync();
            OnAuth?.Invoke(auth);
        }
        else
        {
            OnAuthExpired?.Invoke();
        }
    }

    public TencUserAuth GetAuth()
    {
        if(_profileMgr is { })
        {
            return _profileMgr.Data.TencUserAuth!;
        }
        return new();
    }

    public string? GetSharedLaToken()
    {
        if(_profileMgr is { })
        {
            return _profileMgr.Data.SharedLaToken;
        }
        return null;
    }
    public void SetSharedLaToken(string token)
    {
        if (_profileMgr is { })
        {
            _profileMgr.Data.SharedLaToken = token;
        }
    }


    public async  Task<BitmapImage?> GetAvatorImg()
    {
        if(_profileMgr is { } mgr && mgr.Data.AvatarUrl is { } url)
            return await ImageCacheService.FetchData(url);

        return null;
    }

    public string? GetNickname()
    {
        if (_profileMgr is { } mgr && mgr.Data.UserName is { } name)
            return name;

        return null;
    }
}
