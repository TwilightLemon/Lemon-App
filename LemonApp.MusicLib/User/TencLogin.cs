﻿using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.User;

public class TencLogin(HttpClient httpClient)
{
    private readonly HttpClient _hc = httpClient;
    private string? _referUrl = null, _referCookie = null;
    private bool _startLogin = false;
    private Dictionary<string, string> _cookies = [];

    public event Action<TencUserAuth>? OnAuthCompleted;

    public void CollectInfo(string url, string cookie)
    {
        if (_startLogin)
            return;
        //收集过程中所有cookie
        foreach (string item in cookie.Split(';'))
        {
            string[] kv = item.Split('=');
            if (kv.Length == 2)
            {
                var key = kv[0].Trim();
                _cookies[key] = kv[1].Trim();
            }
        }
        if (cookie.Contains("p_skey"))
            _referCookie = cookie;
        if (url.Contains("https://y.qq.com/portal/wx_redirect.html") && url.Contains("&code="))
            _referUrl = url;
        if (_referUrl != null && _referCookie != null)
            Login();
    }

    private async void Login()
    {
        _startLogin = true;
        string l_code = TextHelper.FindTextByAB(_referUrl!, "&code=", "&", 0);
        //计算g_tk
        string p_skey = _cookies["p_skey"];
        long hash = 5381;
        foreach (char c in p_skey)
        {
            hash += (hash << 5) + c;
        }
        string g_tk = (hash & 0x7fffffff).ToString();
        //POST music.fcg to log in
        string postData = "{\"comm\":{\"g_tk\":" + g_tk + ",\"platform\":\"yqq\",\"ct\":24,\"cv\":0},\"req\":{\"module\":\"QQConnectLogin.LoginServer\",\"method\":\"QQLogin\",\"param\":{\"code\":\"" + l_code + "\"}}}";
        var result = await _hc.SetForMusicuFcg(_referCookie!)
                                        .PostAsync("https://u.y.qq.com/cgi-bin/musicu.fcg",
                                        new StringContent(postData, Encoding.UTF8));
        bool hasValue = result.Headers.TryGetValues("Set-Cookie", out var nc);
        if (hasValue&&nc!=null)
        {
            foreach (var item in nc)
            {
                var temp = item.Split(';')[0].Split('=');
                _cookies[temp[0]] = temp[1];
            }
        }
        else
        {
            throw new Exception("Failed to Login, no header is sent.");
        }
        string json = await result.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);
        string qq = _cookies["uin"];
        //将_cookies转为cookie字符串
        List<string> list = _cookies.Select(item => $"{item.Key}={item.Value}").ToList();
        string cookie = string.Join(";", list);
        var data = new TencUserAuth()
        {
            Id = qq,
            Cookie = cookie,
            G_tk = g_tk
        };
        OnAuthCompleted?.Invoke(data);
    }

}
