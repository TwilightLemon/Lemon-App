using LemonApp.Common.Configs;
using SharedLaCreator.Core;
using System.Net.Http;
namespace LemonApp.MusicLib.Media;
public class SharedLaClient(HttpClient httpClient)
{
    private readonly LaClientValidator _validator = new(httpClient);
    private LaClient? _clientInstance;
    public async Task<LaClient?> GetClient() {
        if (_clientInstance == null || _clientInstance.IsClientExpired)
            _clientInstance = await _validator.VerifyServerAndLogin();
        return _clientInstance;
    }
    public async Task<string?> GetSharedLa(string shaid,string mid,string quality="SQ") {
        _clientInstance ??= await _validator.VerifyServerAndLogin();
        return _clientInstance?.ShareLa(shaid, mid,quality);
    }
}
