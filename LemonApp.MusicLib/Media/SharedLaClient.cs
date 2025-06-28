using SharedLaCreator.Core;
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
    public static string GetSharedLa(string sharedId,string mid,string quality="SQ") {
        return $"https://api.twlmgatito.cn/la/shared/{mid}/?sharedId={sharedId}&quality={quality}";
    }
}
