﻿using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.Media;

public class AudioGetter(HttpClient hc,
    Func<TencUserAuth> tencAuth,
    Func<NeteaseUserAuth>? neteAuth=null,
    SharedLaClient? sharedLaClient=null,
    Func<string?>? sharedLaToken=null)
{
    private TencUserAuth _tencAuth => tencAuth();
    private NeteaseUserAuth? _neteaseAuth => neteAuth?.Invoke();
    private string ? _sharedLaToken => sharedLaToken?.Invoke();
    private readonly SharedLaClient? _laClient = sharedLaClient;
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
        if(m.Source == Platform.qq)
        {
            var final=GetFinalQuality(m.Quality, preferQuality);

            if (_laClient != null && _sharedLaToken is {Length:>0} token)
            {
                string quality = QualityMatcher(final)[1];
                if (await _laClient.GetSharedLa(token, m.MusicID,quality) is { } url){
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
                    //TODO: token expired handler
                }
            }

            return new MusicUrlData() {
                Quality=final,
                SourceText= "YQQ",
                Url = await GetUrlFcgLine(m.MusicID, final)
            };
        }
        else if(m.Source == Platform.wyy)
        {
            return new MusicUrlData()
            {
                Quality = MusicQuality.Std,
                SourceText = "WYY",
                Url = await GetUrlFromWYY(m.MusicID)
            };
        }
        return null;
    }

    private  async Task<string?> GetUrlFromWYY(string id)
    {
        string url = $"http://music.163.com/api/song/enhance/player/url?ids=[{id}]&br=320000";
        string data= await _hc.SetForNetease(_neteaseAuth?.Cookie??"")
                               .GetStringAsync(url);
        if(JsonNode.Parse(data)is { } obj)
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
            if(json["req_0"]?["data"]?["midurlinfo"]?[0]?["purl"]?.ToString() is { } purl)
            {
                return "https://ws.stream.qqmusic.qq.com/" + purl;
            }
        }
        return null;
    }
}
