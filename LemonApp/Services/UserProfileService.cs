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
        _profileMgr.Data.NeteaseUserAuth= auth;
    }

    public NeteaseUserAuth? GetNeteaseAuth() => _profileMgr.Data.NeteaseUserAuth;

    public async Task UpdateAuthAndNotify(TencUserAuth auth)
    {
        Debug.WriteLine("Login qq:" + auth.Id);
        var client = httpClientFactory.CreateClient(App.PublicClientFlag) ?? throw new Exception("Failed to load components");

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
        if (_profileMgr.Data.TencUserAuth is { } auth)
            return auth;
        return new() { Id = "0", Cookie = string.Empty, G_tk = "5381" };
    }

    public string? GetSharedLaToken() => _profileMgr.Data.SharedLaToken;
    public void SetSharedLaToken(string token) => _profileMgr.Data.SharedLaToken = token;


    public Task<BitmapImage?> GetAvatorImg() => ImageCacheService.FetchData(_profileMgr.Data.AvatarUrl);

    public string? GetNickname() => _profileMgr.Data.UserName;
}
