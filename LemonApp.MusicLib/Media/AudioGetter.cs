using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.Media;

public class AudioGetter(HttpClient hc,
    Func<TencUserAuth> tencAuth,
    Func<NeteaseUserAuth?>? neteAuth=null,
    //SharedLaClient? sharedLaClient=null,
    Func<string?>? sharedLaToken=null)
{
    private TencUserAuth _tencAuth => tencAuth();
    private NeteaseUserAuth? _neteaseAuth => neteAuth?.Invoke();
    private string ? _sharedLaToken => sharedLaToken?.Invoke();
    //private readonly SharedLaClient? _laClient = sharedLaClient;
    private readonly HttpClient _hc = hc!;

    /// <summary>
    /// 获取音质对应文件拓展名
    /// 0:filetype 1:title
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    public static string[] QualityMatcher(MusicQuality m) => m switch
    {
        MusicQuality.SQ => [".flac", "SQ"],
        MusicQuality.HQ => [".mp3", "HQ"],
        MusicQuality.Std => [".m4a", "120k"],
        _ => throw new NotImplementedException("not supported quality.")
    };

    public static MusicQuality GetFinalQuality(MusicQuality supported,MusicQuality preferred)
        => supported >= preferred ? preferred : supported;
    

    public async Task<MusicUrlData?> GetUrlAsync(Music m, MusicQuality preferQuality)
    {
        //歌曲品质   先选择preference | supported
        var final = GetFinalQuality(m.Quality, preferQuality);
        if (m.Source == Platform.qq)
        {
            //尝试通过SharedLaClient获取  TODO:完善异常情况处理(token过期...)
            //一般来说，共享的token都不会受音质限制，所以不遍历所有音质
            if ( _sharedLaToken is {Length:>0} token)
            {
                string quality = QualityMatcher(final)[1];
                if (SharedLaClient.GetSharedLa(token, m.MusicID,quality) is { } url){
                    string redirect = await HttpHelper.GetRedirectUrl(url);
                    if (!string.IsNullOrEmpty(redirect)&&await HttpHelper.GetHTTPFileSize(_hc, redirect) > 0)
                    {
                        return new MusicUrlData()
                        {
                            Quality = final,
                            SourceText = "Shared",
                            Url = redirect
                        };
                    }
                }
            }

            //遍历所有音质
            string? purl = null;
            while (true)
            {
                purl = await GetUrlFcgLine(m.MusicID, final);
                if (!string.IsNullOrEmpty(purl)||final == MusicQuality.Std)
                    break;
                final--;
            }

            return new MusicUrlData() {
                Quality=final,
                SourceText= "YQQ",
                Url = purl
            };
        }
        else if(m.Source == Platform.wyy)
        {
            string? url = await GetUrlFromGdStudio(m.MusicID);
            if (url is null)
            {
                url = await GetUrlFromWYY(m.MusicID);
                final = MusicQuality.Std;
            }
            return new MusicUrlData()
            {
                Quality = final,
                SourceText = "WYY",
                Url = url
            };
        }
        return null;
    }
    private async Task<string?> GetUrlFromGdStudio(string id)
    {
        string url = $"https://music-api.gdstudio.xyz/api.php?types=url&source=netease&id={id}&br=999";
        string data = await _hc.SetCookie("").GetStringAsync(url);
        if (JsonNode.Parse(data) is { } json)
        {
            if (json["url"]?.ToString() is { Length: > 0 } link)
            {
                return link;
            }
        }
        return null;
    }
    private async Task<string?> GetUrlFromWYY(string id)
    {
        string url = $"http://music.163.com/api/song/enhance/player/url?ids=[{id}]&br=320000";
        string data = await _hc.SetForNetease(_neteaseAuth?.Cookie ?? "")
                               .GetStringAsync(url);
        if (JsonNode.Parse(data) is { } obj)
        {
            if (obj?["data"]?[0]?["url"]?.ToString() is { } result)
            {
                return result;
            }
        }
        return null;
    }

    private  async Task<string?> GetUrlFcgLine(string Musicid, MusicQuality quality)
    {
        string prefix = quality switch
        {
            MusicQuality.Std => "C400",
            MusicQuality.HQ => "M800",
            MusicQuality.SQ => "F000",
            _=>throw new NotImplementedException()
        };
        string surffix = quality switch
        {
            MusicQuality.Std => "m4a",
            MusicQuality.HQ => "mp3",
            MusicQuality.SQ => "flac",
            _ => throw new NotImplementedException()
        };
        string url = "https://u.y.qq.com/cgi-bin/musicu.fcg?format=json&data={%22req_0%22:{%22module%22:%22vkey.GetVkeyServer%22,%22method%22:%22CgiGetVkey%22,%22param%22:{%22filename%22:[%22PREFIXSONGMIDSONGMID.SUFFIX%22],%22guid%22:%2210000%22,%22songmid%22:[%22SONGMID%22],%22songtype%22:[0],%22uin%22:%220%22,%22loginflag%22:1,%22platform%22:%2220%22}},%22loginUin%22:%220%22,%22comm%22:{%22uin%22:%220%22,%22format%22:%22json%22,%22ct%22:24,%22cv%22:0}}";
        url = url.Replace("SONGMID", Musicid).Replace("PREFIX", prefix).Replace("SUFFIX", surffix);
        string data = await _hc.SetForMusicuFcg(_tencAuth.Cookie!)
                               .GetStringAsync(url);
        if(JsonNode.Parse(data) is { } json)
        {
            if(json["req_0"]?["data"]?["midurlinfo"]?[0]?["purl"]?.ToString() is {Length:>0} purl)
            {
                return "https://ws.stream.qqmusic.qq.com/" + purl;
            }
        }
        return null;
    }
}
